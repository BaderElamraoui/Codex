using Carta.External.Dal.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Dal.Cache
{
    public class Container
    {
        public List<V3_EXTERNAL_ENDPOINTS> ApiLoginEndpoint { get; set; }
        public List<V3_API_EXTERNAL_SERVICE> ApiExternalServices { get; set; }
        public List<V3_API_EXTERNAL_SERVICE_PARAMS> ApiExternalServiceParams { get; set; }
        public List<V3_EXTERNAL_BRANCH_API_LOGINS> ApiExternalBranchApiLogins { get; set; }


    }
}
