using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Objects
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

    }
}
