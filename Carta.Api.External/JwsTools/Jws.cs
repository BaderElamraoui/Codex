using Carta.Api.External.Logic.Objects;
using Carta.Security.Cryptography.Software.Jws;
using System.Configuration;

namespace Carta.Api.External.JwsTools
{
    public static class Jws
    {
        public static string GetDecryptedPayload(string cryptedRequest)
        {
            string decryptedRequest;
            var jwsObj = new JwsObject(JwsType.Flattened);
            var publicKey = ConfigurationManager.AppSettings[Constants.JWS_PUBLIC_KEY];
            jwsObj.TryJwsVerify(cryptedRequest, publicKey, true, out decryptedRequest);
            return decryptedRequest;
        }
    }
}