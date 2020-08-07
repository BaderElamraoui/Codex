using Carta.Api.External.Logic.Objects;
using Carta.Api.External.Logic.Processor;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
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
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string GUID = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(GUID))
            {
                StreamReader sReader = new StreamReader(streamRequest);
                StringBuilder sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                ExternalApiProcessor externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;
                if (!externalApiProcessor.TryProcessPostRequest(GUID, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return new MemoryStream(0);

            }
        }


        public Stream PostData(Stream streamRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string GUID = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(GUID))
            {
                StreamReader sReader = new StreamReader(streamRequest);
                StringBuilder sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.Info(string.Format("GTW API REQUEST: {0}", sbRequest.ToString()));

                GtwApiProcessor gtwApiProcessor = new GtwApiProcessor(sbRequest.ToString());

                string response;
                int count = RETRY_MANAGEMENT_COUNT;
                HttpStatusCode httpResponse = HttpStatusCode.BadRequest;
                bool processingResult = gtwApiProcessor.TryProcessPostRequest(out response, out httpResponse);
                while (count > 0 && !processingResult)
                {
                    processingResult = gtwApiProcessor.TryProcessPostRequest(out response, out httpResponse);
                    count--;
                }

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = httpResponse;
                return GetResponse(response);

            }

        }

        public Stream GetResponse(string response)
        {
            byte[] resultByte = Encoding.UTF8.GetBytes(response);
            return new MemoryStream(resultByte);
        }

        public Stream GetClientId(Stream streamRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string GUID = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(GUID))
            {
                StreamReader sReader = new StreamReader(streamRequest);
                StringBuilder sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.Info(string.Format("GTW API REQUEST: {0}", sbRequest.ToString()));

                GtwApiProcessor gtwApiProcessor = new GtwApiProcessor(sbRequest.ToString(), false);

                string response;
                HttpStatusCode httpResponse = HttpStatusCode.BadRequest;
                bool processingResult = gtwApiProcessor.TryProcessGetClientRequest(out response, out httpResponse);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = httpResponse;
                return GetResponse(response);

            }
        }

        public Stream AntelopCheckCard(Stream streamRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string GUID = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(GUID))
            {
                StreamReader sReader = new StreamReader(streamRequest);
                StringBuilder sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.InfoFormat("EXTERNAL API REQUEST: {0}", sbRequest.ToString());

                ExternalApiProcessor externalApiProcessor = new ExternalApiProcessor(sbRequest.ToString());

                string response;
                externalApiProcessor.TryProcessCheckCard(GUID, out response);

                ServiceResponse serviceResponse = JsonConvert.DeserializeObject<ServiceResponse>(response);

                JObject outpuResponse = new JObject(
                    new JProperty("decision", serviceResponse.serviceResponseCode == Constants.SUCCESS ? decision.CONTINUE : decision.DECLINE),
                    new JProperty("declineReason", serviceResponse.serviceResponseCode == statusDecline.CARD_EXPIRED ? DeclineReason.CARD_EXPIRED :
                                                     serviceResponse.serviceResponseCode == statusDecline.INVALID_PAN ? DeclineReason.INVALID_PAN :
                                                     serviceResponse.serviceResponseCode == statusDecline.PAN_INELIGIBLE ? DeclineReason.PAN_INELIGIBLE :
                                                     DeclineReason.OTHER)
                                                    );
                if (serviceResponse.serviceResponseCode == Constants.SUCCESS)
                {
                    JToken issuerCardId = serviceResponse.serviceResponseData.SelectToken("issuerCardId");

                    outpuResponse.Add("issuerCardId", issuerCardId.ToString());
                }
                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(outpuResponse.ToString());

            }
        }

        public Stream AntelopGetCard(string issuerCardId)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (string.IsNullOrWhiteSpace(issuerCardId))
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string GUID = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(GUID))
            {


                log.InfoFormat("EXTERNAL API REQUEST issuerCardId: {0}", issuerCardId);

                ExternalApiProcessor externalApiProcessor = new ExternalApiProcessor(issuerCardId);

                string response;
                if (!externalApiProcessor.TryProcessGetCard(GUID, issuerCardId, out response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);
            }
        }
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
