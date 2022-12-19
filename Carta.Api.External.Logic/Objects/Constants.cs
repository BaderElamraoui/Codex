namespace Carta.Api.External.Logic.Objects
{
    public class Constants
    {
        protected Constants() { }

        public const string SUCCESS = "000";
        public const string EXECUTED = "Executed";
        public const string DEBIT = "DEBIT";
        public const string CREDIT = "CREDIT";
        public const string CONFIRM_TRANSFER_SERVICE = "CONFIRM_SERVICE";
        public const string ROLLBACK_TRANSFER_SERVICE = "ROLLBACK_SERVICE";
        public const string EXTERNAL_TRANSFER_SERVICE = "EXTERNAL_SERVICE";
        public const string GTW_ENDPOINT = "GTW_ENDPOINT";
        public const string CHANNEL_ID = "CHANNEL_ID";
        public const string CHANNEL_TYPE = "CHANNEL_TYPE";
        public const string REQUESTOR_ID = "REQUESTOR_ID";
        public const string REQUESTOR_CREDENTIALS = "REQUESTOR_CREDENTIALS";
        public const string AUTHORISATION_CHANLLENCE = "AUTHORISATION_CHANLLENCE";
        public const string AUTHORISATION_CHALLENGE_CANCEL = "AUTHORISATION_CHALLENGE_CANCEL";

        public const string WEB_CHANNEL_ID = "WEB_CHANNEL_ID";
        public const string WEB_CHANNEL_TYPE = "WEB_CHANNEL_TYPE";
        public const string WEB_REQUESTOR_ID = "WEB_REQUESTOR_ID";
        public const string WEB_REQUESTOR_CREDENTIALS = "WEB_REQUESTOR_CREDENTIALS";
        public const string JWS_PUBLIC_KEY = "JWS_PUBLIC_KEY";
        public const string JWS_PRIVATE_KEY = "JWS_PRIVATE_KEY";
        public const string ANTELOP_CHECK_CARD = "ANTELOP_CHECK_CARD";
        public const string ANTELOP_GET_CARD = "ANTELOP_GET_CARD";

        public const string ANTELOP_GTW_ENDPOINT = "ANTELOP_GTW_ENDPOINT";
        public const string ANTELOP_CHANNEL_ID = "ANTELOP_CHANNEL_ID";
        public const string ANTELOP_CHANNEL_TYPE = "ANTELOP_CHANNEL_TYPE";
        public const string ANTELOP_REQUESTOR_ID = "ANTELOP_REQUESTOR_ID";
        public const string ANTELOP_REQUESTOR_CREDENTIALS = "ANTELOP_REQUESTOR_CREDENTIALS";

        public const string JWE_CARTA_PUBLIC_KEY = "JWE_CARTA_PUBLIC_KEY";
        public const string JWE_CARTA_PRIVATE_KEY = "JWE_CARTA_PRIVATE_KEY";
        public const string JWE_ANTELOP_PUBLIC_KEY = "JWE_ANTELOP_PUBLIC_KEY";
        public const string ANTELOP_KEY = "ANTELOP_KEY";
        public const string CARTA_KEY = "CARTA_KEY";
        public const string TOUCHTECH_API_KEY = "TOUCHTECH_API_KEY";
         public const string FWD_CHALLENGE_ENDPOINT = "FWD_CHALLENGE_ENDPOINT";
        public const string ANTELOP_GET_CRYPTOGRAM = "ANTELOP_GET_CRYPTOGRAM";
        public const string ANTELOP_GET_PINCODE = "ANTELOP_GET_PINCODE";
        public const string FWD_APATA_CHALLENGE_ENDPOINT = "FWD_APATA_CHALLENGE_ENDPOINT";

        public const string FINANCIAL_INSTITUTION_CB = "FINANCIAL_INSTITUTION_CB";
        public const string FINANCIAL_INSTITUTION_AB = "FINANCIAL_INSTITUTION_AB";
        public const string AB_GTW_ENDPOINT = "AB_GTW_ENDPOINT";
    }
    public class statusDecline
    {
        protected statusDecline() { }
        public const string CVX2_FAILURE = "L0601";
        public const string CARD_EXPIRED = "H0008";
        public const string PAN_INELIGIBLE = "DPI01";
        public const string INVALID_PAN = "DIP01";
    }

    public class DeclineReason
    {
        protected DeclineReason() { }
        public const string INVALID_PAN = "INVALID_PAN";
        public const string PAN_INELIGIBLE = "PAN_INELIGIBLE";
        public const string CARD_EXPIRED = "CARD_EXPIRED";
        public const string CVX2_FAILURE = "CVX2_FAILURE";
        public const string CVX2_VERIFICATION_RESULT = "INVALID";
        public const string OTHER = "OTHER";

    }

    public class decision
    {
        protected decision() { }
        public const string SUCCESS = "SUCCESS";
        public const string DECLINE = "DECLINE";
    }
}