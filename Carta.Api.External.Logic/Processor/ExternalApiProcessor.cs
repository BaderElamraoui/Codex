using Carta.Api.External.Logic.Http;
using Carta.Api.External.Logic.Objects;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Net;
using Carta.Security.Cryptography.Software.Encryption;
using Carta.Security.Cryptography.Software.Jwe;
using System.IO;
using Carta.Api.External.Dal.Db;

namespace Carta.Api.External.Logic.Processor
{

    public class ExternalApiProcessor
    {
        private readonly static ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string _request;


        public ExternalApiProcessor(string request)
        {
            _request = request;
        }

        public bool TryProcessPostRequest(string guid, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                var externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                var serviceName = string.Empty;
                if ((string)externalServiceRequest["state"] == Constants.EXECUTED)
                {
                    if ((string)externalServiceRequest["transferType"] == Constants.DEBIT)
                        serviceName = ConfigurationManager.AppSettings[Constants.CONFIRM_TRANSFER_SERVICE];
                    if ((string)externalServiceRequest["transferType"] == Constants.CREDIT)
                        serviceName = ConfigurationManager.AppSettings[Constants.EXTERNAL_TRANSFER_SERVICE];
                }
                else
                {
                    if ((string)externalServiceRequest["transferType"] == Constants.CREDIT)
                        serviceName = ConfigurationManager.AppSettings[Constants.ROLLBACK_TRANSFER_SERVICE];
                }
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var gtwRequest = PrepareGtwRequest(guid, serviceName, externalServiceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }


        private string PrepareGtwRequest(string guid, string serviceName, JObject externalServiceRequest)
        {


            var serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.REQUESTOR_CREDENTIALS],

                serviceData = GetServiceData(externalServiceRequest)
            };

            var request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }

        private string Prepare3dsGtwRequest(string guid, string serviceName, JObject externalServiceRequest)
        {


            var serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.WEB_CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.WEB_CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.WEB_REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.WEB_REQUESTOR_CREDENTIALS],

                serviceData = GetServiceData(externalServiceRequest)
            };

            var request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }

        private static dynamic GetServiceData(JObject externalServiceRequest)
        {
            var serviceData = new ExpandoObject();

            foreach (var item in externalServiceRequest)
            {
                ((IDictionary<string, object>)serviceData)[item.Key] = item.Value;
            }
            ((IDictionary<string, object>)serviceData)["actionDatetimestamp"] = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["ACTION_DATE_TIMESTAMP_FORMAT"]);
            return serviceData;
        }


        public bool TryProcessCheckCard(string guid, WebHeaderCollection Headers, out string response, out HttpStatusCode externalStatusCode)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            externalStatusCode = HttpStatusCode.BadRequest;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                var externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                var serviceName = string.Empty;

                serviceName = ConfigurationManager.AppSettings[Constants.ANTELOP_CHECK_CARD];
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;


                #region check if encrypted pan is correct
                foreach (var item in externalServiceRequest)
                {
                    switch (item.Key)
                    {
                        case "pan":
                        {
                            var jweObject = new JweObject("");
                            var privateKey = @ConfigurationManager.AppSettings[Constants.JWE_CARTA_PRIVATE_KEY];
                            var keyId = ConfigurationManager.AppSettings[Constants.CARTA_KEY];
                            jweObject.keyPath = privateKey;
                            string clearValue;
                            jweObject.TryAsymmetricJweDecrypt(item.Value.ToString(), "RSA-OAEP-256", "A256CBC-HS512", privateKey, keyId, out clearValue);

                            if (!string.IsNullOrWhiteSpace(clearValue)) continue;
                            externalStatusCode = HttpStatusCode.Unauthorized;
                            Log.InfoFormat("The pan is not decrypted correctly, in this case we return the http status code {0}", externalStatusCode);
                            return false;
                        }
                        case "cvx2":
                        {
                            var jweObject = new JweObject("");
                            var privateKey = @ConfigurationManager.AppSettings[Constants.JWE_CARTA_PRIVATE_KEY];
                            var keyId = ConfigurationManager.AppSettings[Constants.CARTA_KEY];
                            jweObject.keyPath = privateKey;
                            string clearValue;
                            jweObject.TryAsymmetricJweDecrypt(item.Value.ToString(), "RSA-OAEP-256", "A256CBC-HS512", privateKey, keyId, out clearValue);

                            if (!string.IsNullOrWhiteSpace(clearValue)) continue;
                            externalStatusCode = HttpStatusCode.Unauthorized;
                            Log.InfoFormat("The pan is not decrypted correctly, in this case we return the http status code {0}", externalStatusCode);
                            return false;
                        }
                    }
                }
                #endregion
                Log.Info("Preparing Gtw Request");
                var gtwRequest = PrepareAntelopRequest(guid, serviceName, externalServiceRequest, Headers);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();

                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.ANTELOP_GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;
            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }

        public static bool TryProcessGetCard(string guid, string issuerCardId, WebHeaderCollection Headers, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                var serviceName = ConfigurationManager.AppSettings[Constants.ANTELOP_GET_CARD];
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var serviceData = new ExpandoObject();
                ((IDictionary<string, object>)serviceData)["issuerCardId"] = issuerCardId;
                ((IDictionary<string, object>)serviceData)["actionDatetimestamp"] = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]);
                var serviceRequest = new ServiceRequest()
                {
                    serviceRequestId = guid,
                    serviceName = serviceName,
                    channelId = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_ID],
                    channelType = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_TYPE],
                    requestorId = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_ID],
                    requestorCredential = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_CREDENTIALS],
                    actionDatetimestamp = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]),
                    serviceData = serviceData
                };

                var gtwRequest = JsonConvert.SerializeObject(serviceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.ANTELOP_GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }

        private string PrepareAntelopRequest(string guid, string serviceName, JObject externalServiceRequest, WebHeaderCollection Headers)
        {


            var serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_CREDENTIALS],
                actionDatetimestamp = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]),
                serviceData = GetAntelopServiceData(externalServiceRequest)
            };

            var request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }

        private dynamic GetAntelopServiceData(JObject externalServiceRequest)
        {
            var serviceData = new ExpandoObject();
            ((IDictionary<string, object>)serviceData)["actionDatetimestamp"] = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]);
            foreach (var item in externalServiceRequest)
            {
                switch (item.Key)
                {
                    case "pan":
                    {
                        var jweObject = new JweObject("");
                        var privateKey = @ConfigurationManager.AppSettings[Constants.JWE_CARTA_PRIVATE_KEY];
                        var keyId = ConfigurationManager.AppSettings[Constants.CARTA_KEY];
                        jweObject.keyPath = privateKey;
                        string clearValue;
                        jweObject.TryAsymmetricJweDecrypt(item.Value.ToString(), "RSA-OAEP-256", "A256CBC-HS512", privateKey, keyId, out clearValue);
                        ((IDictionary<string, object>)serviceData)[item.Key] = clearValue;
                        break;
                    }
                    case "cvx2":
                    {
                        var jweObject = new JweObject("");
                        var privateKey = @ConfigurationManager.AppSettings[Constants.JWE_CARTA_PRIVATE_KEY];
                        var keyId = ConfigurationManager.AppSettings[Constants.CARTA_KEY];
                        jweObject.keyPath = privateKey;
                        string clearValue;
                        jweObject.TryAsymmetricJweDecrypt(item.Value.ToString(), "RSA-OAEP-256", "A256CBC-HS512", privateKey, keyId, out clearValue);
                        ((IDictionary<string, object>)serviceData)[item.Key] = clearValue;
                        break;
                    }
                    default:
                        ((IDictionary<string, object>)serviceData)[item.Key] = item.Value;
                        break;
                }
            }
            return serviceData;
        }
        public bool TryProcess3dsChallengeRequest(string guid, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                var externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                var serviceName = ConfigurationManager.AppSettings[Constants.AUTHORISATION_CHANLLENCE];
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var gtwRequest = Prepare3dsGtwRequest(guid, serviceName, externalServiceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }

        public bool TryProcess3dsChallengeResult(string guid, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {

                var externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                var service = GetServiceData(externalServiceRequest);

                var headers = new List<Header>(){
                    new Header {id = "3ds-challenge-result", value = service.serviceData.authenticationStatus},
                     new Header {id = "3ds-transaction-token", value = service.serviceData.transactionToken},
                     new Header {id = "X-Api-Key", value = ConfigurationManager.AppSettings[Constants.TOUCHTECH_API_KEY]}
                };

                var httpManager = new HttpManager();
                if (HttpManager.PostWithoutBody(headers, ConfigurationManager.AppSettings[Constants.FWD_CHALLENGE_ENDPOINT], out response))
                {
                    Log.Info("Resposne of chalenge result : " + response);
                    return true;
                }

                Log.Info("Resposne of chalenge result : " + response);
                return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

        }


        public bool TryProcess3dsChallengeRequestCancel(string guid, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                var externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                var serviceName =  ConfigurationManager.AppSettings[Constants.AUTHORISATION_CHALLENGE_CANCEL];
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var gtwRequest = Prepare3dsGtwRequest(guid, serviceName, externalServiceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }


        public static bool TryProcessGetCryptogram(string guid, string issuerCardId, WebHeaderCollection headers, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                var serviceName = ConfigurationManager.AppSettings[Constants.ANTELOP_GET_CRYPTOGRAM];
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var serviceData = new ExpandoObject();
                ((IDictionary<string, object>)serviceData)["issuerCardId"] = issuerCardId;
                ((IDictionary<string, object>)serviceData)["actionDatetimestamp"] = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]);
                var serviceRequest = new ServiceRequest()
                {
                    serviceRequestId = guid,
                    serviceName = serviceName,
                    channelId = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_ID],
                    channelType = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_TYPE],
                    requestorId = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_ID],
                    requestorCredential = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_CREDENTIALS],
                    actionDatetimestamp = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]),
                    serviceData = serviceData
                };

                var gtwRequest = JsonConvert.SerializeObject(serviceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.ANTELOP_GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }
        public static bool TryProcessGetPinCode(string guid, string issuerCardId, WebHeaderCollection Headers, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                var serviceName = ConfigurationManager.AppSettings[Constants.ANTELOP_GET_PINCODE];
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var serviceData = new ExpandoObject();
                ((IDictionary<string, object>)serviceData)["issuerCardId"] = issuerCardId;
                ((IDictionary<string, object>)serviceData)["actionDatetimestamp"] = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]);
                var serviceRequest = new ServiceRequest()
                {
                    serviceRequestId = guid,
                    serviceName = serviceName,
                    channelId = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_ID],
                    channelType = ConfigurationManager.AppSettings[Constants.ANTELOP_CHANNEL_TYPE],
                    requestorId = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_ID],
                    requestorCredential = ConfigurationManager.AppSettings[Constants.ANTELOP_REQUESTOR_CREDENTIALS],
                    actionDatetimestamp = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["Iso8601Withfff"]),
                    serviceData = serviceData
                };

                var gtwRequest = JsonConvert.SerializeObject(serviceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.ANTELOP_GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }

        public bool TryProcessPostGenesysRequest(string guid, out string response)
        {
            Log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                var externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                var serviceName = string.Empty;
                Log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                Log.Info("Preparing Gtw Request");
                var gtwRequest = PrepareGenesysRequest(guid, serviceName, externalServiceRequest);

                Log.DebugFormat("Request={0}", gtwRequest);

                var httpManager = new HttpManager();
                var externalStatusCode = HttpStatusCode.BadRequest;
                if (!httpManager.TryCall(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT], "POST", out response, out externalStatusCode))
                    return false;

                var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
                Log.Debug(ex);
                return false;
            }

            return true;
        }
        private string PrepareGenesysRequest(string guid, string serviceName, JObject externalServiceRequest)
        {


            var serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.REQUESTOR_CREDENTIALS],

                serviceData = GetServiceData(externalServiceRequest)
            };

            var request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }
    }
}
