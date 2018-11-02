using Carta.External.Dal.Db;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Processor
{
    public class RequestProcessor
    {
        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string PrepareRequest(JToken jsonRequest, List<V3_API_EXTERNAL_SERVICE_PARAMS> externalServiceParams, IDictionary<string, object> inputParams)
        {
            List<V3_API_EXTERNAL_SERVICE_PARAMS> inputExternalServiceParams = externalServiceParams.Where(x => x.IS_INPUT.HasValue && x.IS_INPUT.Value).ToList();

            foreach (V3_API_EXTERNAL_SERVICE_PARAMS item in inputExternalServiceParams)
            {
                object value;
                if (inputParams.TryGetValue(item.PARAMS_NAME, out value) && value != null)
                {
                    foreach (JProperty prop in jsonRequest.Children<JProperty>())
                    {
                        if (prop.Name == item.PARAMS_NAME)
                            prop.Value = value.ToString();
                    }
                }

            }

            return jsonRequest.ToString(Formatting.None);
        }

        public bool TryParseAndPrepareResponse(string externalResponse, string criteria, List<V3_API_EXTERNAL_SERVICE_PARAMS> externalServiceParams, out Dictionary<string, object> outputParams)
        {
            try
            {
                if (string.IsNullOrEmpty(externalResponse))
                {
                    outputParams = null;
                    return false;
                }

                JToken jsonExternalResponse = JToken.Parse(externalResponse);
                outputParams = new Dictionary<string, object>();

                if (IsResponseValid(jsonExternalResponse, criteria))
                {
                    log.Info("Response is valid");

                    List<V3_API_EXTERNAL_SERVICE_PARAMS> outputExternalServiceParams = externalServiceParams.Where(x => x.IS_INPUT.HasValue && !x.IS_INPUT.Value).ToList();
                    

                    foreach (V3_API_EXTERNAL_SERVICE_PARAMS item in outputExternalServiceParams)
                    {
                        foreach (JProperty prop in jsonExternalResponse.Children<JProperty>())
                        {
                            if (prop.Name == item.PARAMS_NAME)
                                outputParams[item.PARAMS_NAME] = prop.Value;
                        }
                    }
                   
                }
                return true;

            }
            catch (Exception ex)
            {
                log.Error("Error during parsing external response:", ex);
                outputParams = null;
                return false;
            }
            

        }

        public bool IsResponseValid(JToken jsonResponse, string criteria)
        {
            if (string.IsNullOrEmpty(criteria))
                return true;

            string paramsName = criteria.Split('|')[0];
            string operation = criteria.Split('|')[1];
            string paramsValue = criteria.Split('|')[2] == "null" ? null : criteria.Split('|')[2];

            string output = jsonResponse.Value<string>(paramsName) ?? null;
            log.Debug(string.Format("Checking if {0}{1}{2}", paramsName, operation, paramsValue));
            if ((operation == "=" && paramsValue == output) || (operation == "!=" && paramsValue != output))
            {
                return true;
            }
            return false;
        }



    }
}
