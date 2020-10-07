using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Carta.Api.External
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService
    {

        [OperationContract]
        [WebInvoke(UriTemplate = "CartaExternalAPI",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            Method = "POST"
            )]
        Stream PostData(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "CartaAPI",
           ResponseFormat = WebMessageFormat.Json,
           RequestFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Bare,
           Method = "POST"
           )]
        Stream PostExternalData(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "GetClientId",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            Method = "POST"
            )]
        Stream GetClientId(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "api/v1/cards/checkCard",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            Method = "POST"
            )]
        Stream AntelopCheckCard(Stream streamRequest);

        [WebGet(UriTemplate = "api/v1/cards/{issuerCardId}")]
        [OperationContract]
        Stream AntelopGetCard(string issuerCardId);
    }
}
