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
        public ExternalApiProcessor(string request)
        {
            _request = request;
        }

        public bool TryProcessPostRequest(string guid, out string response)
        {
            response = string.Empty;

            return true;
        }

        


    }
}
