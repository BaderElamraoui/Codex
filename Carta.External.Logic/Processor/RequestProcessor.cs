using Carta.External.Dal.Db;
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

        public string PrepareRequest(JToken jsonRequest, List<V3_API_EXTERNAL_SERVICE_PARAMS> externalServiceParams, IDictionary<string, object> inputParams)
        {
            return null;
        }

        public JToken ParseResponse(string response)
        {

            return null;
        }

        public bool IsRequestValid(JToken jsonResponse)
        {
            return false;
        }



    }
}
