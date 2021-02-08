using Carta.Api.External.Logic.Objects;
using Carta.Security.Cryptography.Software.Jws;
using System.Configuration;

namespace Carta.Api.External.JwsTools
{
    public static class Jws
    {
        public static string GetDecryptedPayload(string cryptedRequest)
        {
            string decryptedRequest = "";
            bool isJwsSigned = false;
            JwsObject jwsObj = new JwsObject(JwsType.Flattened);
            string publicKey = ConfigurationManager.AppSettings[Constants.JWS_PUBLIC_KEY];
            isJwsSigned = jwsObj.TryJwsVerify(cryptedRequest, publicKey, true, out decryptedRequest);
            return decryptedRequest;
        }
    }
}