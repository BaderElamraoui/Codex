using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Carta.Api.External.Logger
{
    public static class DataMasker
    {
        private static readonly string[] SensitiveFields = new[]
        {
            "pan", "pin", "cardNumber", "cvv2", "expiryDate", "previousExpiryDate"
        };

        public static string MaskSensitiveData(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            foreach (var field in SensitiveFields)
            {
                var pattern = $@"(?i)(""{field}""\s*:\s*"")(.*?)("")";
                input = Regex.Replace(input, pattern, m =>
                {
                    var value = m.Groups[2].Value;
                    var maskedValue = MaskValue(value);
                    return $"{m.Groups[1].Value}{maskedValue}{m.Groups[3].Value}";
                });
            }

            return input;
        }

        private static string MaskValue(string value)
        {
            if (value.Length <= 4)
                return new string('*', value.Length);

            return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
        }
    }
}