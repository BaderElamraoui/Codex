using Carta.Api.External.Logic.Objects;
using Carta.Api.External.Logic.Processor;
using Carta.Security.Cryptography.Software.Jwe;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
namespace Carta.Api.External
{

    public class Service : IService
    {
        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly int RETRY_MANAGEMENT_COUNT = int.Parse(ConfigurationManager.AppSettings["RETRY_MANAGEMENT_COUNT"]);

        public Stream PostExternalData(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);
                var sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                var externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;
                if (!externalApiProcessor.TryProcessPostRequest(guid, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return new MemoryStream(0);

            }
        }


        public Stream PostData(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);
                var sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.Info(string.Format("GTW API REQUEST: {0}", sbRequest.ToString()));

                var gtwApiProcessor = new GtwApiProcessor(sbRequest.ToString());

                string response;
                var count = RETRY_MANAGEMENT_COUNT;
                var httpResponse = HttpStatusCode.BadRequest;
                var processingResult = gtwApiProcessor.TryProcessPostRequest(out response, out httpResponse);
                while (count > 0 && !processingResult)
                {
                    processingResult = gtwApiProcessor.TryProcessPostRequest(out response, out httpResponse);
                    count--;
                }

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                var ctx = WebOperationContext.Current;
                if (ctx != null) ctx.OutgoingResponse.StatusCode = httpResponse;
                return GetResponse(response);

            }

        }

        private static Stream GetResponse(string response)
        {
            var resultByte = Encoding.UTF8.GetBytes(response);
            return new MemoryStream(resultByte);
        }

        public Stream GetClientId(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);
                var sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.Info($"GTW API REQUEST: {sbRequest}");

                var gtwApiProcessor = new GtwApiProcessor(sbRequest.ToString(), false);

                string response;
                var httpResponse = HttpStatusCode.BadRequest;
                gtwApiProcessor.TryProcessGetClientRequest(out response, out httpResponse);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                var ctx = WebOperationContext.Current;
                if (ctx != null) ctx.OutgoingResponse.StatusCode = httpResponse;
                return GetResponse(response);

            }
        }

        public Stream AntelopCheckCard(Stream streamRequest)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                if (streamRequest == null)
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                var guid = Guid.NewGuid().ToString("N");
                using (ThreadContext.Stacks["NDC"].Push(guid))
                {
                    WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json;charset=UTF-8";
                    var headers = WebOperationContext.Current.IncomingRequest.Headers;

                    foreach (var header in headers.AllKeys)
                    {
                        var headerContent = headers[header];
                        log.InfoFormat("Header Name : {0}, Header Content : {1} ", header, headerContent);
                    }
                    var sReader = new StreamReader(streamRequest);
                    var sbRequest = new StringBuilder(sReader.ReadToEnd());
                    var request = sbRequest.ToString();

                    log.InfoFormat("EXTERNAL CRYPTED API REQUEST: {0}", request);

                    var externalApiProcessor = new ExternalApiProcessor(request);

                    string response;
                    HttpStatusCode externalStatusCode;
                    externalApiProcessor.TryProcessCheckCard(guid, headers, out response, out externalStatusCode);

                    stopwatch.Stop();
                    log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                    var enc = new JweRsaEncryption();
                    return GetCheckCardResponse(response, externalStatusCode);

                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw new WebFaultException(HttpStatusCode.InternalServerError);
            }
        }

        public Stream AntelopGetCard(string issuerCardId, bool includePreviousCard)
        {

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (string.IsNullOrWhiteSpace(issuerCardId))
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json;charset=UTF-8";
            var headers = WebOperationContext.Current.IncomingRequest.Headers;
            using (ThreadContext.Stacks["NDC"].Push(guid))
            {

                foreach (var header in headers.AllKeys)
                {
                    var headerContent = headers[header];
                    log.InfoFormat("Header Name : {0}, Header Content : {1} ", header, headerContent);

                }
                log.InfoFormat("EXTERNAL API REQUEST issuerCardId: {0}", issuerCardId);
                var externalApiProcessor = new ExternalApiProcessor(issuerCardId);

                string response;
                if (!ExternalApiProcessor.TryProcessGetCard(guid, issuerCardId, headers, out response))
                    throw new WebFaultException((HttpStatusCode)422);

                var enc = new JweObject("");
                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetCardResponse(response, enc, includePreviousCard);
            }

        }

        private static Stream GetCheckCardResponse(string response, HttpStatusCode externalStatusCode)
        {
            var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);

            var outputResponse = new JObject();
            if (serviceResponse != null && serviceResponse.serviceResponseCode == Constants.SUCCESS)
            {
                JToken issuerCardId = serviceResponse.serviceResponseData.SelectToken("issuerCardId");
                outputResponse.Add("issuerCardId", issuerCardId.ToString());
                outputResponse.Add("decision", decision.SUCCESS);
            }
            else if (serviceResponse != null)
            {
                outputResponse.Add("decision", decision.DECLINE);
                outputResponse.Add("declineReason", serviceResponse.serviceResponseCode == statusDecline.CARD_EXPIRED ? DeclineReason.CARD_EXPIRED :
                                                 serviceResponse.serviceResponseCode == statusDecline.INVALID_PAN ? DeclineReason.INVALID_PAN :
                                                 serviceResponse.serviceResponseCode == statusDecline.PAN_INELIGIBLE ? DeclineReason.PAN_INELIGIBLE :
                                                 DeclineReason.OTHER);
            }
            else if (externalStatusCode == HttpStatusCode.Unauthorized)
            {
                outputResponse.Add("decision", decision.DECLINE);
                outputResponse.Add("declineReason", DeclineReason.INVALID_PAN);
            }
            else
            {
                outputResponse.Add("decision", decision.DECLINE);
                outputResponse.Add("declineReason", DeclineReason.OTHER);
            }

            var resultByte = Encoding.UTF8.GetBytes(outputResponse.ToString());
            return new MemoryStream(resultByte);
        }

        private static Stream GetCardResponse(string response, JweObject jweObject, bool includePreviousCard)
        {
            var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);

            var outputResponse = new JObject();
            if (serviceResponse.serviceResponseCode == Constants.SUCCESS)
            {
                JToken pan = serviceResponse.serviceResponseData.SelectToken("pan");
                JToken expiryDate = serviceResponse.serviceResponseData.SelectToken("expiryDate");

                var pathKey = @ConfigurationManager.AppSettings[Constants.JWE_ANTELOP_PUBLIC_KEY];
                var keyId = ConfigurationManager.AppSettings[Constants.ANTELOP_KEY];
                var cardNumber = pan.ToString();
                string encryptedPan;
                jweObject.keyPath = pathKey;
                jweObject.TryAsymmetricJweEncrypt(cardNumber, "RSA-OAEP-256", "A128CBC-HS256", pathKey, keyId, out encryptedPan);

                outputResponse.Add("pan", encryptedPan);
                outputResponse.Add("expiryDate", expiryDate);

                JToken previousPan = serviceResponse.serviceResponseData.SelectToken("previousPan");
                JToken previousExpiryDate = serviceResponse.serviceResponseData.SelectToken("previousExpiryDate");

                if (includePreviousCard)
                {
                    var previousCard = new PreviousCard();
                    if (!string.IsNullOrWhiteSpace(previousPan.ToString()))
                    {
                        jweObject.TryAsymmetricJweEncrypt(previousPan.ToString(), "RSA-OAEP-256", "A128CBC-HS256", pathKey, keyId, out encryptedPan);
                        previousCard.pan = encryptedPan;
                    }
                    if (!string.IsNullOrWhiteSpace(previousExpiryDate.ToString()))
                        previousCard.expiryDate = previousExpiryDate.ToString();

                    if (!string.IsNullOrWhiteSpace(previousCard.pan) || !string.IsNullOrWhiteSpace(previousCard.expiryDate))
                        outputResponse.Add("previousCard", JObject.FromObject(previousCard, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore }));
                    else
                        throw new WebFaultException((HttpStatusCode)404);
                }
            }
            else
            {
                outputResponse.Add("decision", decision.DECLINE);
                outputResponse.Add("declineReason", serviceResponse.serviceResponseCode == statusDecline.CARD_EXPIRED ? DeclineReason.CARD_EXPIRED :
                                                 serviceResponse.serviceResponseCode == statusDecline.INVALID_PAN ? DeclineReason.INVALID_PAN :
                                                 serviceResponse.serviceResponseCode == statusDecline.PAN_INELIGIBLE ? DeclineReason.PAN_INELIGIBLE :
                                                 DeclineReason.OTHER);
            }
            log.Info("Final Response :" + outputResponse.ToString());
            var resultByte = Encoding.UTF8.GetBytes(outputResponse.ToString());
            return new MemoryStream(resultByte);
        }
        public Stream GetCardCryptogram(string issuerCardId)
        {

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (string.IsNullOrWhiteSpace(issuerCardId))
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json;charset=UTF-8";
            var headers = WebOperationContext.Current.IncomingRequest.Headers;
            using (ThreadContext.Stacks["NDC"].Push(guid))
            {

                foreach (var header in headers.AllKeys)
                {
                    var headerContent = headers[header];
                    log.InfoFormat("Header Name : {0}, Header Content : {1} ", header, headerContent);

                }
                log.InfoFormat("EXTERNAL API REQUEST issuerCardId: {0}", issuerCardId);
                var externalApiProcessor = new ExternalApiProcessor(issuerCardId);

                string response;
                if (!ExternalApiProcessor.TryProcessGetCryptogram(guid, issuerCardId, headers, out response))
                    throw new WebFaultException((HttpStatusCode)422);

                var enc = new JweObject("");
                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetCryptogramResponse(response, enc);
            }

        }

        public Stream GetPinCode(string issuerCardId)
        {

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (string.IsNullOrWhiteSpace(issuerCardId))
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json;charset=UTF-8";
            var headers = WebOperationContext.Current.IncomingRequest.Headers;
            using (ThreadContext.Stacks["NDC"].Push(guid))
            {

                foreach (var header in headers.AllKeys)
                {
                    var headerContent = headers[header];
                    log.InfoFormat("Header Name : {0}, Header Content : {1} ", header, headerContent);

                }
                log.InfoFormat("EXTERNAL API REQUEST issuerCardId: {0}", issuerCardId);
                var externalApiProcessor = new ExternalApiProcessor(issuerCardId);

                string response;
                if (!ExternalApiProcessor.TryProcessGetPinCode(guid, issuerCardId, headers, out response))
                    throw new WebFaultException((HttpStatusCode)422);

                var enc = new JweObject("");
                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetPinCodeResponse(response, enc);
            }

        }

        private static Stream GetCryptogramResponse(string response, JweObject jweObject)
        {
            var pathKey = @ConfigurationManager.AppSettings[Constants.JWE_ANTELOP_PUBLIC_KEY];
            var keyId = ConfigurationManager.AppSettings[Constants.ANTELOP_KEY];
            var encryptedcvx2 = "";
            var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
            var outputResponse = new JObject();

            if (serviceResponse.serviceResponseCode == Constants.SUCCESS)
            {
                JToken cvx2 = serviceResponse.serviceResponseData.SelectToken("cvx2");
                if (cvx2 == null)
                    throw new WebFaultException((HttpStatusCode)404);

                jweObject.keyPath = pathKey;
                jweObject.TryAsymmetricJweEncrypt(cvx2.ToString(), "RSA-OAEP-256", "A128CBC-HS256", pathKey, keyId, out encryptedcvx2);

                outputResponse.Add("cvx2", encryptedcvx2);
            }
            else
            {
                outputResponse.Add("decision", decision.DECLINE);
                outputResponse.Add("declineReason", serviceResponse.serviceResponseCode == statusDecline.CARD_EXPIRED ? DeclineReason.CARD_EXPIRED :
                                                 serviceResponse.serviceResponseCode == statusDecline.INVALID_PAN ? DeclineReason.INVALID_PAN :
                                                 serviceResponse.serviceResponseCode == statusDecline.PAN_INELIGIBLE ? DeclineReason.PAN_INELIGIBLE :
                                                 DeclineReason.OTHER);
            }
            log.Info("Final Response :" + outputResponse.ToString());
            var resultByte = Encoding.UTF8.GetBytes(outputResponse.ToString());
            return new MemoryStream(resultByte);
        }

        private static Stream GetPinCodeResponse(string response, JweObject jweObject)
        {
            var pathKey = @ConfigurationManager.AppSettings[Constants.JWE_ANTELOP_PUBLIC_KEY];
            var keyId = ConfigurationManager.AppSettings[Constants.ANTELOP_KEY];
            var encryptedpinCode = "";
            var serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);
            var outpuResponse = new JObject();

            if (serviceResponse.serviceResponseCode == Constants.SUCCESS)
            {
                JToken pinCode = serviceResponse.serviceResponseData.SelectToken("pinCode");
                if (pinCode == null)
                    throw new WebFaultException((HttpStatusCode)404);

                jweObject.keyPath = pathKey;
                jweObject.TryAsymmetricJweEncrypt(pinCode.ToString(), "RSA-OAEP-256", "A128CBC-HS256", pathKey, keyId, out encryptedpinCode);

                outpuResponse.Add("pinCode", encryptedpinCode);
            }
            else
            {
                outpuResponse.Add("decision", decision.DECLINE);
                outpuResponse.Add("declineReason", serviceResponse.serviceResponseCode == statusDecline.CARD_EXPIRED ? DeclineReason.CARD_EXPIRED :
                                                 serviceResponse.serviceResponseCode == statusDecline.INVALID_PAN ? DeclineReason.INVALID_PAN :
                                                 serviceResponse.serviceResponseCode == statusDecline.PAN_INELIGIBLE ? DeclineReason.PAN_INELIGIBLE :
                                                 DeclineReason.OTHER);
            }
            log.Info("Final Response :" + outpuResponse.ToString());
            var resultByte = Encoding.UTF8.GetBytes(outpuResponse.ToString());
            return new MemoryStream(resultByte);
        }
        public Stream ChallengeRequest(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json";

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);



                var sbRequest = new StringBuilder(sReader.ReadToEnd());
                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                var jwsDecrypted = JwsTools.Jws.GetDecryptedPayload(sbRequest.ToString());
                log.InfoFormat("EXTERNAL API JWS DECRYPTED REQUEST: {0}", sbRequest.ToString());

                if (string.IsNullOrWhiteSpace(jwsDecrypted))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                var externalApiProcessor = new ExternalApiProcessor(jwsDecrypted);

                string response;

                externalApiProcessor.TryProcess3dsChallengeRequest(guid, out response);

                if (!externalApiProcessor.TryProcess3dsChallengeRequest(guid, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);
            }
        }

        public Stream ChallengeRequestCancel(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json";

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);
                var sbRequest = new StringBuilder(sReader.ReadToEnd());
                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                if (string.IsNullOrWhiteSpace(sbRequest.ToString()))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                var externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;
                if (!externalApiProcessor.TryProcess3dsChallengeRequestCancel(guid, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);
            }
        }

        public Stream Challenge(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json";

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);

                var sbRequest = new StringBuilder(sReader.ReadToEnd());
                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                if (string.IsNullOrWhiteSpace(sbRequest.ToString()))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                var externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;

                if (!externalApiProcessor.TryProcess3dsChallengeRequest(guid, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);
            }
        }

        public Stream ChallengeResult(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json";

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);

                var sbRequest = new StringBuilder(sReader.ReadToEnd());
                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                if (string.IsNullOrWhiteSpace(sbRequest.ToString()))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                var externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;
                if (!externalApiProcessor.TryProcess3dsChallengeResult(guid, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);
            }
        }

        public Stream ApataChallengeResult(Stream streamRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            var guid = Guid.NewGuid().ToString("N");
            WebOperationContext.Current.OutgoingResponse.ContentType = "Application/json";

            using (ThreadContext.Stacks["NDC"].Push(guid))
            {
                var sReader = new StreamReader(streamRequest);

                var sbRequest = new StringBuilder(sReader.ReadToEnd());
                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                if (string.IsNullOrWhiteSpace(sbRequest.ToString()))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                var externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;

                externalApiProcessor.TryProcessApataChallengeResult(guid, out response);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);
            }
        }

    }
    public class PreviousCard
    {
        public PreviousCard() { }
        public string pan { get; set; }
        public string expiryDate { get; set; }
    }
    public class RawWebContentTypeMapper : WebContentTypeMapper
    {
        /// <summary>
        /// Get message format for content type
        /// </summary>
        /// <param name="contentType">The content type</param>
        /// <returns>WebContentFormat</returns>
        public override WebContentFormat GetMessageFormatForContentType(string contentType)
        {
            return WebContentFormat.Raw;
        }
    }
}