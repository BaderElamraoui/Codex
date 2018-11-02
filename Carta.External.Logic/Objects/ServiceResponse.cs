using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Objects
{


    public class ServiceResponse
    {

        public string serviceRequestId { get; set; }       // Request Id
        public string serviceResponseCode { get; set; }       // Response Code
        public string serviceResponseLabel { get; set; }       // Response Label
        public dynamic serviceResponseData { get; set; }       // Output Data
       
        public ServiceResponse(ServiceRequest serviceRequest)
        {
            serviceRequestId = serviceRequest.serviceRequestId;
            serviceResponseCode = "900";
            serviceResponseLabel = "System Error";
            serviceResponseData = new ExpandoObject();
        }

    }


}
