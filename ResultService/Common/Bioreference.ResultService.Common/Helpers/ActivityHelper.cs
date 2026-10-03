using Bioreference.ResultService.Common.Enumerations;
using System.Diagnostics;

namespace Bioreference.ResultService.Common.Helpers
{
    public static class ActivityHelper
    {
        public static void SetLogKey(string logKey)
        {            
            if (string.IsNullOrWhiteSpace(logKey))
                return;  
            
            Activity.Current?.SetTag("log_key", logKey.Trim());           
        }

        public static void SetAccessionLogKey(string logKey)
        {
            if (string.IsNullOrWhiteSpace(logKey))
                return;
            
            logKey = NormalizeAccession(logKey);           
            if (IsSevenDigitNumber(logKey))
            {
                logKey = "10" + logKey;
            }

            Activity.Current?.SetTag("log_key", logKey);
        }

        private static bool IsSevenDigitNumber(string value)
        {
            return !string.IsNullOrEmpty(value)
                   && value.Length == 7
                   && value.All(char.IsDigit);
        }
        private static string NormalizeAccession(string input)
        {
            return new string(input
                .Trim()
                .Where(char.IsDigit)
                .ToArray());
        }
    }
}
