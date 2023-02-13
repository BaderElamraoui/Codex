using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

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

        [OperationContract]
        [WebInvoke(UriTemplate = "api/v1/cards/{issuerCardId}?includePreviousCard={includePreviousCard}",
                 Method = "GET"
            )]
        Stream AntelopGetCard(string issuerCardId, bool includePreviousCard);
        [OperationContract]
        [WebInvoke(UriTemplate = "ChallengeRequest",
           ResponseFormat = WebMessageFormat.Json,
           RequestFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Bare,
           Method = "POST"
           )]
        Stream ChallengeRequest(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "cancel",
           ResponseFormat = WebMessageFormat.Json,
           RequestFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Bare,
           Method = "POST"
           )]
        Stream ChallengeRequestCancel(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "challenge",
           ResponseFormat = WebMessageFormat.Json,
           RequestFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Bare,
           Method = "POST"
           )]
        Stream Challenge(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "challengeResult",
           ResponseFormat = WebMessageFormat.Json,
           RequestFormat = WebMessageFormat.Json,
           BodyStyle = WebMessageBodyStyle.Bare,
           Method = "POST"
           )]
        Stream ChallengeResult(Stream streamRequest);


        [OperationContract]
        [WebInvoke(UriTemplate = "api/v1/cards/{issuerCardId}/cvx2",
                Method = "GET"
           )]
        Stream GetCardCryptogram(string issuerCardId);

        [OperationContract]
        [WebInvoke(UriTemplate = "api/v1/cards/{issuerCardId}/pinCode",
                Method = "GET"
           )]
        Stream GetPinCode(string issuerCardId);

        [OperationContract]
        [WebInvoke(UriTemplate = "apataChallengeResult",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            Method = "POST"
        )]
        Stream ApataChallengeResult(Stream streamRequest);

        [OperationContract]
        [WebInvoke(UriTemplate = "GenesysApi",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            Method = "POST"
        )]
        Stream GenesysApiRequest(Stream streamRequest);
    }
}
