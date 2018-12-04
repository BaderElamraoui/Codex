using Carta.External.Logic.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Processor
{

    public class ExternalApiProcessor
    {
        private readonly string _request;
        private readonly string _executed = "Executed";
        public ExternalApiProcessor(string request)
        {
            _request = request;
        }

        public bool TryProcessPostRequest(string guid, out string response)
        {
            response = string.Empty;

            ExternalServiceRequest externalServiceRequest = JsonConvert.DeserializeObject<ExternalServiceRequest>(_request);

            if (externalServiceRequest == null)
                return false;

            if(externalServiceRequest.state == _executed)
            {
                // Process confirmation
            }
            else
            {
                // Process Rollback
            }
            

            return true;
        }

        


    }
}
