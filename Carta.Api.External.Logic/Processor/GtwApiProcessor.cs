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
using System.Net;
using System.Net.WebSockets;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace Carta.Api.External.Logic.Processor
{
    public class GtwApiProcessor
    {

        private readonly static ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ServiceRequest _serviceRequest;
        private readonly ServiceResponse _serviceResponse;
        private IDictionary<string, object> _serviceParams;
        private readonly RequestProcessor _requestProcessor;
        private readonly HttpManager _httpManager;
        private const string SUCCESS = "000";
        private const string SUCCESS_LABEL = "Successful Operation";
        private const string CONCERNED_ENTITY = "CEA";
        private readonly string _request;

        private string _privateKey;

        public GtwApiProcessor(string request, bool convert = true)
        {
            if (convert)
            {
                _serviceRequest = JsonConvert.DeserializeObject<ServiceRequest>(request);
                _serviceResponse = new ServiceResponse(_serviceRequest);
                MapRequestParams();
            }
            else
            {
                _request = request;
            }
            _requestProcessor = new RequestProcessor();
            _httpManager = new HttpManager();
            _privateKey = ConfigurationManager.AppSettings[Constants.JWS_PRIVATE_KEY];
        }

        private void MapRequestParams()
        {
            _serviceParams = (IDictionary<string, object>)_serviceRequest.serviceData;
        }

        public bool TryProcessPostRequest(out string response, out HttpStatusCode statusCode)
        {
            response = string.Empty;
            statusCode = HttpStatusCode.BadRequest;
            var serviceName = _serviceRequest.serviceName;

            var uid = GetParamValue("uid") != null ? GetParamValue("uid").ToString() : string.Empty;
            var serviceId = GetParamValue("serviceId") != null ? GetParamValue("serviceId").ToString() : string.Empty;

            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(serviceId))
            {
                Log.Error("UID is mandatory in the request for unicity selection");
                return false;
            }

            var externalBranchApiLogin = CacheContainer.Instance.Container.ApiExternalBranchApiLogins.FirstOrDefault(x => x.LOGIN == _serviceRequest.requestorId && x.PASS == _serviceRequest.requestorCredential && x.UID == int.Parse(uid) && x.SERVICE_ID == serviceId);

            Log.Info(
                $"Getting V3_EXTERNAL_BRANCH_API_LOGINS by  LOGIN={_serviceRequest.requestorId}, UID={uid}, SERVICE_ID={serviceId}: Result={externalBranchApiLogin != null}");

            if (externalBranchApiLogin == null)
            {
                Log.Warn("V3_EXTERNAL_BRANCH_API_LOGINS is null");
            }

            Log.Info("Getting Service Name to FWD the operation to");

            var externalService = CacheContainer.Instance.Container.ApiExternalServices.SingleOrDefault(x => x.SERVICE_GTW_NAME == serviceName && x.UID == int.Parse(uid));

            Log.Info(
                $"Getting V3_API_EXTERNAL_SERVICE by SERVICE_GTW_NAME={serviceName}, UID={uid}: Result={externalService != null}");

            if (externalService == null)
            {
                Log.Error("No Configuration has been found for called service");
                return false;
            }

            Log.Info("Getting service endpoint");

            var externalEndpoint = CacheContainer.Instance.Container.ApiExternalEndpoint.SingleOrDefault(x => x.CONCERNED_ENTITY == CONCERNED_ENTITY && x.SERVICE_ID == serviceId && x.BRANCH_API_LOGIN_UID == int.Parse(uid));

            Log.InfoFormat("Getting V3_API_LOGIN_ENDPOINTS by LOGIN={0}, PASS=***,CHANNEL_ID={1}, CHANNEL_TYPE={2} : Result={3}", _serviceRequest.requestorId, _serviceRequest.channelId, _serviceRequest.channelType, externalEndpoint != null);

            if (externalEndpoint == null)
            {
                Log.Error("No endpoint has been found");
                return false;
            }

            Log.Info("Getting service param to prepare the external Request");

            var externalParams = CacheContainer.Instance.Container.ApiExternalServiceParams.Where(x => x.EXTERNAL_SERVICE_NAME == externalService.EXTERNAL_SERVICE_NAME && x.UID == int.Parse(uid)).ToList();

            var requestHeaders = RequestProcessor.PrepareExternalRequestHeaders(_serviceParams, externalService.PARSED_HEADERS);


            var request = RequestProcessor.PrepareExternalRequest(externalService.PARSED_REQUEST_MAP, externalParams, _serviceParams);

            string externalResponse;
            var externalStatusCode = HttpStatusCode.BadRequest;

            if (externalBranchApiLogin.JWS_ENABLED != null && externalBranchApiLogin.JWS_ENABLED == true)
            {
                var payload = request;
                var header = JsonConvert.SerializeObject(requestHeaders);

                var jwsObj = new JwsObject(JwsType.Flattened);


                var configuredAlgorithm = externalBranchApiLogin.JWS_ALGORITHM.Split('|');
                _privateKey = configuredAlgorithm[2].ToLower();
                string jwsEncryptionData;
                request = jwsObj.TryJwsSign(header, payload, _privateKey, out jwsEncryptionData) ? jwsEncryptionData : payload;

            }
            _httpManager.TryCall(request, requestHeaders, externalEndpoint.ENDPOINT, externalService.METHOD, out externalResponse, out externalStatusCode);

            if (externalStatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, object> outputParams;
                if (!_requestProcessor.TryParseAndPrepareExternalResponse(externalResponse, externalService.CRITERIA,
                    externalParams, out outputParams)) return true;
                _serviceResponse.serviceResponseCode = SUCCESS;
                _serviceResponse.serviceResponseLabel = SUCCESS_LABEL;
                foreach (var entry in outputParams)
                {
                    ((IDictionary<string, object>)_serviceResponse.serviceResponseData)[entry.Key] = entry.Value;
                }
                response = JsonConvert.SerializeObject(_serviceResponse);
                statusCode = externalStatusCode;
            }
            else
            {
                response = externalResponse;
                statusCode = externalStatusCode;
            }
            return true;
        }

        public void TryProcessGetClientRequest(out string response, out HttpStatusCode statusCode)
        {
            response = string.Empty;
            statusCode = HttpStatusCode.BadRequest;

            if (WebOperationContext.Current == null) return;
            var headers = WebOperationContext.Current.IncomingRequest.Headers;
            if (!_httpManager.TryCallGetClientId(_request, headers, ConfigurationManager.AppSettings[Constants.GTW_ENDPOINT], "POST", out response, out statusCode)) return;
        }

        private object GetParamValue(string key)
        {
            object value;
            return _serviceParams.TryGetValue(key, out value) ? value : null;
        }


    }
}
