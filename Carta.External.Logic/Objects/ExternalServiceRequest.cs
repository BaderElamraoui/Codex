using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Objects
{
    class ExternalServiceRequest
    {

        public string accountIBAN { get; set; }
        public string accountNumber { get; set; }
        public string currency { get; set; }
        public string messageId { get; set; }
        public string paymentId { get; set; }
        public string processingTime { get; set; }
        public string rejectionReason { get; set; }
        public string state { get; set; }
        public string transferType { get; set; }

    }
}
