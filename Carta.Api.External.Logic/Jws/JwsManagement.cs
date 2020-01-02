namespace Carta.Api.External.Logic.Jws
{
    public class JwsManagement
    {

        public static string GetDecryptedPayload(string cryptedRequest)
        {
            string decryptedRequest = "";
            //bool isJwsSigned = false;
            //JwsObject jwsObj = new JwsObject(JwsType.Flattened);
            //string publicKey = ConfigurationManager.AppSettings[Constants.JWS_PUBLIC_KEY];
            //isJwsSigned = jwsObj.TryJwsVerify(sReader.ReadToEnd(), publicKey, true, out decryptedRequest);
            return decryptedRequest;
        }
    }
}
