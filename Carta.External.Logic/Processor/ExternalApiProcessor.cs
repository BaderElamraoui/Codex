using Carta.External.Logic.Http;
using Carta.External.Logic.Objects;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Processor
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
                ExternalServiceRequest externalServiceRequest = JsonConvert.DeserializeObject<ExternalServiceRequest>(_request);

                if (externalServiceRequest == null)
                    return false;

                string serviceName = string.Empty;
                if (externalServiceRequest.state == Constants.EXECUTED)
                {
                    if (externalServiceRequest.transferType == Constants.DEBIT)
                        serviceName = ConfigurationManager.AppSettings[Constants.CONFIRM_TRANSFER_SERVICE];
                    if (externalServiceRequest.transferType == Constants.CREDIT)
                        serviceName = ConfigurationManager.AppSettings[Constants.EXTERNAL_TRANSFER_SERVICE];
                }
                else
                {
                    if (externalServiceRequest.transferType == Constants.CREDIT)
                        serviceName = ConfigurationManager.AppSettings[Constants.ROLLBACK_TRANSFER_SERVICE];
                }
                log.InfoFormat("Service name to execute = {0}", serviceName);
                if (string.IsNullOrEmpty(serviceName))
                    return false;

                log.Info("Preparing Gtw Request");
                string gtwRequest = PrepareGtwRequest(guid, serviceName, externalServiceRequest);

                log.DebugFormat("Request={0}",gtwRequest);

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


        private string PrepareGtwRequest(string guid, string serviceName, ExternalServiceRequest externalServiceRequest)
        {
            

            ServiceRequest serviceRequest = new ServiceRequest()
            {
                serviceRequestId = guid,
                serviceName = serviceName,
                channelId = ConfigurationManager.AppSettings[Constants.CHANNEL_ID],
                channelType = ConfigurationManager.AppSettings[Constants.CHANNEL_TYPE],
                requestorId = ConfigurationManager.AppSettings[Constants.REQUESTOR_ID],
                requestorCredential = ConfigurationManager.AppSettings[Constants.REQUESTOR_CREDENTIALS],
                serviceData = {
                    accountIBAN = externalServiceRequest.accountIBAN,
                    accountNumber = externalServiceRequest.accountNumber,
                    messageId = externalServiceRequest.messageId,
                    paymentId = externalServiceRequest.paymentId,
                    processingTime = externalServiceRequest.processingTime,
                    rejectionReason = externalServiceRequest.rejectionReason,
                    currency = externalServiceRequest.currency,
                    transferType = externalServiceRequest.transferType,
                    state = externalServiceRequest.state,
                    paymentAmount= externalServiceRequest.paymentAmount,
                    actionDatetimestamp = DateTimeOffset.Now.ToString(ConfigurationManager.AppSettings["ACTION_DATE_TIMESTAMP_FORMAT"])
                }
            };

            string request = JsonConvert.SerializeObject(serviceRequest);

            return request;
        }


    }
}
