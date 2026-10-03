namespace Bioreference.LIS
{
    public static class StringExtensions
    {
        public static string Get7Digit(this string accessionNbr)
        {
            if (string.IsNullOrWhiteSpace(accessionNbr))
                return accessionNbr;

            accessionNbr = accessionNbr.Trim();

            return accessionNbr.Length == 9 && accessionNbr.StartsWith("10")
                ? accessionNbr.Substring(2)
                : accessionNbr;
        }

        public static string Get9Digit(this string accessionNbr)
        {
            if (string.IsNullOrWhiteSpace(accessionNbr))
                return accessionNbr;

            accessionNbr = accessionNbr.Trim();

            return accessionNbr.Length == 7
                ? "10" + accessionNbr
                : accessionNbr;
        }
    }
}
