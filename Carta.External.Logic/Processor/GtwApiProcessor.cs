using Carta.External.Logic.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Processor
{
    public class GtwApiProcessor
    {

        private readonly ServiceRequest serviceRequest;

        public GtwApiProcessor(string request)
        {
            serviceRequest = JsonConvert.DeserializeObject<ServiceRequest>(request);
        }

        public IDictionary<string, object> GetRequestParams()
        {
            return (IDictionary<string, object>)serviceRequest.serviceData;
        }

        public string Process()
        {
            string response = string.Empty;

            return response;

        }

        

    }
}
