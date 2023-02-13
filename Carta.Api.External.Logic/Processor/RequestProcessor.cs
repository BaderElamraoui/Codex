using Carta.Api.External.Dal.Db;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Carta.Api.External.Logic.Processor
{
    public class RequestProcessor
    {
        private readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string PrepareExternalRequest(JToken jsonRequest, List<V3_API_EXTERNAL_SERVICE_PARAMS> externalServiceParams, IDictionary<string, object> inputParams)
        {
            var inputExternalServiceParams = externalServiceParams.Where(x => x.IS_INPUT && x.IS_INPUT).ToList();

            foreach (var item in inputExternalServiceParams)
            {
                object value;
                if (inputParams.TryGetValue(item.PARAMS_NAME, out value) && value != null)
                {

                    var token = jsonRequest.SelectToken(item.PARAMS_NAME);
                    token?.Replace(value.ToString());

                }
                else
                {
                    //Make sure that we don't send # to external parties
                    var token = jsonRequest.SelectToken(item.PARAMS_NAME);
                    token?.Replace(null);
                }

            }

            return jsonRequest.ToString(Formatting.None);
        }

        public List<Header> PrepareExternalRequestHeaders(IDictionary<string, object> inputParams, List<Header> headers)
        {
            var headersOutput = headers;
            foreach (var header in headersOutput)
            {
                if (string.IsNullOrEmpty(header.value))
                {
                    Log.Info("Header value is null");
                    header.value = inputParams[header.id].ToString();
                }

                else if (header.value.Contains("#"))
                {
                    try
                    {
                        Log.Info("Header value contain #");
                        header.value = header.value.Replace("#", inputParams[header.id].ToString());
                    }
                    catch
                    {
                        header.value = header.value.Replace("#", Guid.NewGuid().ToString("N"));
                    }
                }
                else if (header.id == "callbackUrl")
                    header.value = "https://my.callback.url";
            }
            return headersOutput;
        }

        public bool TryParseAndPrepareExternalResponse(string externalResponse, string criteria, List<V3_API_EXTERNAL_SERVICE_PARAMS> externalServiceParams, out Dictionary<string, object> outputParams)
        {
            try
            {
                if (string.IsNullOrEmpty(externalResponse))
                {
                    outputParams = null;
                    return false;
                }

                var jsonExternalResponse = JToken.Parse(externalResponse);
                outputParams = new Dictionary<string, object>();

                if (!IsResponseValid(jsonExternalResponse, criteria)) return true;
                Log.Info("Response is valid");

                var outputExternalServiceParams = externalServiceParams.Where(x => !x.IS_INPUT).ToList();


                foreach (var item in outputExternalServiceParams)
                {
                    foreach (var prop in jsonExternalResponse.Children<JProperty>())
                    {
                        if (prop.Name == item.PARAMS_NAME)
                            outputParams[item.PARAMS_NAME] = prop.Value;
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                Log.Error("Error during parsing external response:", ex);
                outputParams = null;
                return false;
            }


        }

        private bool IsResponseValid(JToken jsonResponse, string criteria)
        {
            if (string.IsNullOrEmpty(criteria))
                return true;

            var paramsName = criteria.Split('|')[0];
            var operation = criteria.Split('|')[1];
            var paramsValue = criteria.Split('|')[2] == "null" ? null : criteria.Split('|')[2];

            var output = jsonResponse.Value<string>(paramsName) ?? null;
            Log.Debug($"Checking if {paramsName}{operation}{paramsValue}");
            return (operation == "=" && paramsValue == output) || (operation == "!=" && paramsValue != output);
        }



    }
}
