using System;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public static class Extensions
    {

        public static bool CompareText(this string value, string CompareTo)
        {
            return value.Equals(CompareTo, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNothingOrEmpty(this string value)
        {
            return value is null || value.Trim().Length == 0;
        }

        public static string OnlyNumeric(this string value)
        {
            var s = new StringBuilder();
            int i;
            var loopTo = value.Length - 1;
            for (i = 0; i <= loopTo; i++)
            {
                if ("0123456789".Contains(value.Substring(i, 1)))
                {
                    s.Append(value.Substring(i, 1));
                }
            }
            return s.ToString();
        }

        public static bool IsInteger(this string value)
        {
            for (int i = 0, loopTo = value.Length - 1; i <= loopTo; i++)
            {
                if (!"0123456789".Contains(value.Substring(i, 1)))
                {
                    return false;
                }
            }
            return true;
        }

        public static string StandardASCII(this string value)
        {
            var s = new StringBuilder();
            if (value is not null)
            {
                int i;
                char c;
                for (int p = 0, loopTo = value.Length - 1; p <= loopTo; p++)
                {
                    c = Conversions.ToChar(value.Substring(p, 1));
                    i = Strings.Asc(c);
                    if (i >= 32 && i < 127)
                        s.Append(c);
                }
            }
            return s.ToString();
        }

    }
}