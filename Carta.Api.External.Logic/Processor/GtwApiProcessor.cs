using Carta.Api.External.Dal.Cache;
using Carta.Api.External.Dal.Db;
using Carta.Api.External.Logic.Http;
using Carta.Api.External.Logic.Objects;
using Carta.Security.Cryptography.Software.Jws;
using log4net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
namespace Carta.Api.External.Logic.Processor
{
    public class GtwApiProcessor
    {

        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ServiceRequest _serviceRequest;
        private readonly ServiceResponse _serviceResponse;
        private IDictionary<string, object> _serviceParams;
        private readonly RequestProcessor _requestProcessor;
        private readonly HttpManager _httpManager;
        private const string SUCCESS = "000";
        private const string SUCCESS_LABEL = "Successful Operation";
        private const string CONCERNED_ENTITY = "CEA";
        private string _privateKey;

        public GtwApiProcessor(string request)
        {
            _serviceRequest = JsonConvert.DeserializeObject<ServiceRequest>(request);
            _serviceResponse = new ServiceResponse(_serviceRequest);
            MapRequestParams();
            _requestProcessor = new RequestProcessor();
            _httpManager = new HttpManager();
            _privateKey = ConfigurationManager.AppSettings[Constants.JWS_PRIVATE_KEY];
        }

        private void MapRequestParams()
        {
            _serviceParams = (IDictionary<string, object>)_serviceRequest.serviceData;
        }

        public bool TryProcessPostRequest(out string response)
        {
            response = string.Empty;

            string serviceName = _serviceRequest.serviceName;

            string uid = GetParamValue("uid") != null ? GetParamValue("uid").ToString() : string.Empty;
            string serviceId = GetParamValue("serviceId") != null ? GetParamValue("serviceId").ToString() : string.Empty;

            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(serviceId))
            {
                log.Error("UID is mandatory in the request for unicity selection");
                return false;
            }

            V3_EXTERNAL_BRANCH_API_LOGINS externalBranchApiLogin = CacheContainer.Instance.Container.ApiExternalBranchApiLogins.FirstOrDefault(x => x.LOGIN == _serviceRequest.requestorId && x.PASS == _serviceRequest.requestorCredential && x.UID == int.Parse(uid) && x.SERVICE_ID == serviceId);

            log.Info(string.Format("Getting V3_EXTERNAL_BRANCH_API_LOGINS by  LOGIN={0}, UID={1}, SERVICE_ID={2}: Result={3}", _serviceRequest.requestorId, uid, serviceId, externalBranchApiLogin != null));

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
                return false;
            }

            log.Info("Getting service endpoint");

            V3_API_LOGIN_ENDPOINTS externalEndpoint = CacheContainer.Instance.Container.ApiExternalEndpoint.SingleOrDefault(x => x.CONCERNED_ENTITY == CONCERNED_ENTITY && x.SERVICE_ID == serviceId && x.BRANCH_API_LOGIN_UID == int.Parse(uid));

            log.InfoFormat("Getting V3_API_LOGIN_ENDPOINTS by LOGIN={0}, PASS=***,CHANNEL_ID={1}, CHANNEL_TYPE={2} : Result={3}", _serviceRequest.requestorId, _serviceRequest.channelId, _serviceRequest.channelType, externalEndpoint != null);

            if (externalEndpoint == null)
            {
                log.Error("No endpoint has been found");
                return false;
            }

            log.Info("Getting service param to prepare the external Request");

            List<V3_API_EXTERNAL_SERVICE_PARAMS> externalParams = CacheContainer.Instance.Container.ApiExternalServiceParams.Where(x => x.EXTERNAL_SERVICE_NAME == externalService.EXTERNAL_SERVICE_NAME && x.UID == int.Parse(uid)).ToList();

            List<Header> requestHeaders = _requestProcessor.PrepareExternalRequestHeaders(_serviceParams, externalService.PARSED_HEADERS);


            string request = _requestProcessor.PrepareExternalRequest(externalService.PARSED_REQUEST_MAP, externalParams, _serviceParams);

            if (externalBranchApiLogin.JWS_ENABLED != null && externalBranchApiLogin.JWS_ENABLED == true)
            {
                string algorithm = externalBranchApiLogin.JWS_ALGORITHM;
                string payload = request;
                string header = JsonConvert.SerializeObject(requestHeaders);

                JwsObject jwsObj = new JwsObject(JwsType.Flattened);


                string[] configuredAlgorithm = externalBranchApiLogin.JWS_ALGORITHM.Split('|');
                _privateKey = configuredAlgorithm[2].ToLower();
                string jwsEncryptionData;
                if (jwsObj.TryJwsSign(header, payload, _privateKey, out jwsEncryptionData))
                {
                    request = jwsEncryptionData;
                }
                else
                    request = payload;

            }
            string externalResponse = _httpManager.Post(request, requestHeaders, externalEndpoint.ENDPOINT);

            Dictionary<string, object> outputParams;

            if (_requestProcessor.TryParseAndPrepareExternalResponse(externalResponse, externalService.CRITERIA, externalParams, out outputParams))
            {
                _serviceResponse.serviceResponseCode = SUCCESS;
                _serviceResponse.serviceResponseLabel = SUCCESS_LABEL;
                foreach (KeyValuePair<string, object> entry in outputParams)
                {
                    ((IDictionary<string, object>)_serviceResponse.serviceResponseData)[entry.Key] = entry.Value;
                }
                response = JsonConvert.SerializeObject(_serviceResponse);
            }

            return true;
        }

        public object GetParamValue(string key)
        {
            object value;
            if (_serviceParams.TryGetValue(key, out value))
                return value;

            return null;
        }


    }
}
