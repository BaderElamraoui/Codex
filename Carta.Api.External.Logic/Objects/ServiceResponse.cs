using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.Api.External.Logic.Objects
{


    public class ServiceResponse
    {

        public string serviceRequestId { get; set; }       // Request Id
        public string serviceResponseCode
        {
            get
            {
                return _serviceResponseCode;
            }
            set {
                _serviceResponseCode = value;
                IsSuccess = (_serviceResponseCode == Constants.SUCCESS);
            }
        }       
        public string serviceResponseLabel { get; set; }       // Response Label
        public dynamic serviceResponseData { get; set; }       // Output Data

        public bool IsSuccess { get; set; }
        private string _serviceResponseCode;

        public ServiceResponse(ServiceRequest serviceRequest)
        {
            serviceResponseData = new ExpandoObject();
        }

    }


}
