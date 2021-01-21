using Carta.Api.External.Logic.Http;
using Carta.Api.External.Logic.Objects;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using Carta.Api.External.Dal.Db;

namespace Carta.Api.External.Logic.Processor
{

    public class ExternalApiProcessor
    {
        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string _request;


        public ExternalApiProcessor(string request)
        {
            _request = request;
        }

        public bool TryProcessPostRequest(string guid, out string response)
        {
            log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                JObject externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                string serviceName = string.Empty;
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
                log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                log.Info("Preparing Gtw Request");
                string gtwRequest = PrepareGtwRequest(guid, serviceName, externalServiceRequest);

                log.DebugFormat("Request={0}", gtwRequest);

                HttpManager httpManager = new HttpManager();
                response = httpManager.Post(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT]);

                ServiceResponse serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Debug(ex);
                return false;
            }

            return true;
        }


        private string PrepareGtwRequest(string guid, string serviceName, JObject externalServiceRequest)
        {


            ServiceRequest serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.REQUESTOR_CREDENTIALS],

                serviceData = GetServiceData(externalServiceRequest)
            };

            string request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }

        private string Prepare3dsGtwRequest(string guid, string serviceName, JObject externalServiceRequest)
        {


            ServiceRequest serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.WEB_CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.WEB_CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.WEB_REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.WEB_REQUESTOR_CREDENTIALS],

                serviceData = GetServiceData(externalServiceRequest)
            };

            string request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }

        private dynamic GetServiceData(JObject externalServiceRequest)
        {
            ExpandoObject serviceData = new ExpandoObject();

            foreach (var item in externalServiceRequest)
            {
                ((IDictionary<string, object>)serviceData)[item.Key] = item.Value;
            }
            ((IDictionary<string, object>)serviceData)["actionDatetimestamp"] = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["ACTION_DATE_TIMESTAMP_FORMAT"]);
            return serviceData;
        }

        public bool TryProcess3dsChallengeRequest(string guid, out string response)
        {
            log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                JObject externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                string serviceName = ConfigurationManager.AppSettings[Constants.AUTHORISATION_CHANLLENCE];
                log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                log.Info("Preparing Gtw Request");
                string gtwRequest = Prepare3dsGtwRequest(guid, serviceName, externalServiceRequest);

                log.DebugFormat("Request={0}", gtwRequest);

                HttpManager httpManager = new HttpManager();
                response = httpManager.Post(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT]);

                ServiceResponse serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Debug(ex);
                return false;
            }

            return true;
        }

        public bool TryProcess3dsChallengeResult(string guid, out string response)
        {
            log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {

                JObject externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                dynamic service = GetServiceData(externalServiceRequest);

                List<Header> headers = new List<Header>(){
                    new Header {id = "3ds-challenge-result", value = service.serviceData.authenticationStatus},
                     new Header {id = "3ds-transaction-token", value = service.serviceData.transactionToken},
                     new Header {id = "X-Api-Key", value = ConfigurationManager.AppSettings[Constants.TOUCHTECH_API_KEY]}
                };

                HttpManager httpManager = new HttpManager();
                response = httpManager.PostWithoutBody(headers, ConfigurationManager.AppSettings[Constants.FWD_CHALLENGE_ENDPOINT]);

                ServiceResponse serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Debug(ex);
                return false;
            }

            return true;
        }


        public bool TryProcess3dsChallengeRequestCancel(string guid, out string response)
        {
            log.Info("Trying To process GTW Request");
            response = string.Empty;
            try
            {
                //dynamic externalServiceRequest = JsonConvert.DeserializeObject<dynamic>(_request);

                JObject externalServiceRequest = JObject.Parse(_request);

                if (externalServiceRequest == null)
                    return false;

                string serviceName = serviceName = ConfigurationManager.AppSettings[Constants.AUTHORISATION_CHALLENGE_CANCEL];
                log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                log.Info("Preparing Gtw Request");
                string gtwRequest = Prepare3dsGtwRequest(guid, serviceName, externalServiceRequest);

                log.DebugFormat("Request={0}", gtwRequest);

                HttpManager httpManager = new HttpManager();
                response = httpManager.Post(gtwRequest, null, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT]);

                ServiceResponse serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
                if (!serviceResponse.IsSuccess)
                    return false;

            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Debug(ex);
                return false;
            }

            return true;
        }


    }
}
