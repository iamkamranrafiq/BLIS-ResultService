using System.Diagnostics;

namespace Bioreference.ScanningService.Application.Common.Helpers
{
    public static class ActivityHelper
    {
        public static void SetLogKey(string logKey)
        {

            if (string.IsNullOrWhiteSpace(logKey))
                return;
            Activity.Current?.SetTag("log_key", logKey);
        }

        public static void SetAccessionLogKey(string logKey)
        {

            if (string.IsNullOrWhiteSpace(logKey))
                return;

            string finalAccessionLogKey = CheckAccessionNumber(logKey);

            Activity.Current?.SetTag("log_key", finalAccessionLogKey);
        }
        private static string CheckAccessionNumber(string accession)
        {

            if (IsSevenDigitNumber(accession))
                return "10" + accession;


            return accession;
        }

        private static bool IsSevenDigitNumber(string value)
        {
            if (value.Length != 7)
                return false;

            foreach (char c in value)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }
    }
}
