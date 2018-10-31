using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Objects
{
    public class ServiceRequest
    {

        public string serviceRequestId { get; set; }   
        public string requestorId { get; set; }        
        public string requestorCredential { get; set; }
        public string serviceName { get; set; }        
        public string channelType { get; set; }        
        public string channelId { get; set; }                                                                                                                                                                                                                                                                                                                                                                                                                                                               
        public dynamic serviceData { get; set; }

        public ServiceRequest()
        {
            serviceData = new ExpandoObject();
        }

    }
}
