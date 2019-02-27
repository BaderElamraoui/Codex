using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.Api.External.Dal.Db
{
    public partial class V3_API_EXTERNAL_SERVICE
    {

        public JToken PARSED_REQUEST_MAP { get; set; }
        public List<Header> PARSED_HEADERS { get; set; }

    }

    public class Header
    {
        public string id { get; set; }
        public string value { get; set; }
    }
}
