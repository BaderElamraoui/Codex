using Carta.External.Dal.Cache;
using Carta.External.Dal.Db;
using Carta.External.Logic.Http;
using Carta.External.Logic.Objects;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Processor
{
    public class GtwApiProcessor
    {

        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ServiceRequest serviceRequest;
        private readonly ServiceResponse serviceResponse;
        private IDictionary<string, object> serviceParams;
        private readonly RequestProcessor requestProcessor;
        private readonly HttpManager httpManager;
        private const string SUCCESS = "000";

        public GtwApiProcessor(string request)
        {
            serviceRequest = JsonConvert.DeserializeObject<ServiceRequest>(request);
            serviceResponse = new ServiceResponse(serviceRequest);
            MapRequestParams();
            requestProcessor = new RequestProcessor();
            httpManager = new HttpManager();
        }

        private void MapRequestParams()
        {
            serviceParams = (IDictionary<string, object>)serviceRequest.serviceData;
        }

        public string ProcessPostRequest()
        {
            string response = string.Empty;

            string serviceName = serviceRequest.serviceName;

            string uid = GetParamValue("uid") != null ? GetParamValue("uid").ToString() : string.Empty;
            string serviceId = GetParamValue("serviceId") != null ? GetParamValue("serviceId").ToString() : string.Empty;

            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(serviceId))
            {
                log.Error("UID is mandatory in the request for unicity selection");
                return response;
            }

            V3_EXTERNAL_BRANCH_API_LOGINS externalBranchApiLogin = CacheContainer.Instance.Container.ApiExternalBranchApiLogins.FirstOrDefault(x => x.LOGIN == serviceRequest.requestorId && x.PASS == serviceRequest.requestorCredential && x.UID == int.Parse(uid) && x.SERVICE_ID == serviceId);

            log.Info(string.Format("Getting V3_EXTERNAL_BRANCH_API_LOGINS by  LOGIN={0}, UID={1}, SERVICE_ID={2}: Result={3}", serviceRequest.requestorId, uid, serviceId, externalBranchApiLogin != null));

            if (externalBranchApiLogin == null)
            {
                log.Warn("V3_EXTERNAL_BRANCH_API_LOGINS is null");
            }

            log.Info("Getting Service Name to FWD the operation to");

            V3_API_EXTERNAL_SERVICE externalService = CacheContainer.Instance.Container.ApiExternalServices.SingleOrDefault(x => x.SERVICE_GTW_NAME == serviceName && x.UID == int.Parse(uid));

            log.Info(string.Format("Getting V3_API_EXTERNAL_SERVICE by SERVICE_GTW_NAME={0}, UID={1}: Result={2}", serviceName, uid, externalService != null));

            if (externalService == null)
            {
                log.Error("No Configuration has been found for called service");
                return response;
            }

            log.Info("Getting service endpoint");

            V3_EXTERNAL_ENDPOINTS externalEndpoint = CacheContainer.Instance.Container.ApiLoginEndpoint.SingleOrDefault(x => x.LOGIN == serviceRequest.requestorId && x.PASS == serviceRequest.requestorCredential && x.CHANNEL_ID == serviceRequest.channelId && x.CHANNEL_TYPE == serviceRequest.channelType);

            log.Info(string.Format("Getting V3_API_LOGIN_ENDPOINTS by LOGIN={0}, PASS=***,CHANNEL_ID={1}, CHANNEL_TYPE={2} : Result={3}", serviceRequest.requestorId, serviceRequest.channelId, serviceRequest.channelType, externalEndpoint != null));

            if (externalEndpoint == null)
            {
                log.Error("No endpoint has been found");
                return response;
            }

            log.Info("Getting service param to prepare the external Request");

            List<V3_API_EXTERNAL_SERVICE_PARAMS> externalParams = CacheContainer.Instance.Container.ApiExternalServiceParams.Where(x => x.EXTERNAL_SERVICE_NAME == externalService.EXTERNAL_SERVICE_NAME && x.UID == int.Parse(uid)).ToList();



            string request = requestProcessor.PrepareRequest(externalService.PARSED_REQUEST_MAP, externalParams, serviceParams);

            string externalResponse = httpManager.Post(request, externalService.PARSED_HEADERS, externalEndpoint.ENDPOINT);

            Dictionary<string, object> outputParams;

            if (requestProcessor.TryParseAndPrepareResponse(externalResponse, externalService.CRITERIA, externalParams, out outputParams))
            {
                serviceResponse.serviceResponseCode = SUCCESS;
                foreach (KeyValuePair<string, object> entry in outputParams)
                {
                    ((IDictionary<string, object>)serviceResponse.serviceResponseData)[entry.Key] = entry.Value;
                }
                response = JsonConvert.SerializeObject(serviceResponse);
            }

            return response;
        }

        public object GetParamValue(string key)
        {
            object value;
            if (serviceParams.TryGetValue(key, out value))
                return value;

            return null;
        }


    }
}
