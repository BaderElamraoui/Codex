using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.Api.External.Logic.Objects
{
    public class Constants
    {
        protected Constants() { }

        public const string SUCCESS = "000";
        public const string EXECUTED = "Executed";
        public const string DEBIT = "Debit";
        public const string CREDIT = "Credit";
        public const string CONFIRM_TRANSFER_SERVICE = "CONFIRM_SERVICE";
        public const string ROLLBACK_TRANSFER_SERVICE = "ROLLBACK_SERVICE";
        public const string EXTERNAL_TRANSFER_SERVICE = "EXTERNAL_SERVICE";
        public const string GTW_ENDPOINT = "GTW_ENDPOINT";
        public const string CHANNEL_ID = "CHANNEL_ID";
        public const string CHANNEL_TYPE = "CHANNEL_TYPE";
        public const string REQUESTOR_ID = "REQUESTOR_ID";
        public const string REQUESTOR_CREDENTIALS = "REQUESTOR_CREDENTIALS";
        public const string ANTELOP_CHECK_CARD = "ANTELOP_CHECK_CARD";
        public const string ANTELOP_GET_CARD = "ANTELOP_GET_CARD";

        public const string ANTELOP_GTW_ENDPOINT = "ANTELOP_GTW_ENDPOINT";
        public const string ANTELOP_CHANNEL_ID = "ANTELOP_CHANNEL_ID";
        public const string ANTELOP_CHANNEL_TYPE = "ANTELOP_CHANNEL_TYPE";
        public const string ANTELOP_REQUESTOR_ID = "ANTELOP_REQUESTOR_ID";
        public const string ANTELOP_REQUESTOR_CREDENTIALS = "ANTELOP_REQUESTOR_CREDENTIALS";

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
