using Microsoft.VisualBasic;

namespace Bioreference.ResultService.DI.Interface
{
    static class Common
    {

        // Public Function FormatDateTime(ByVal dDate As Date, Optional ByVal format As String = "yyyyMMddHHmmss") As String
        // Return String.Format("{0:" & format & "}", dDate)
        // End Function

        public static string SexConverter(string Gender)
        {
            if (Gender == "1")
            {
                return "M";
            }
            else if (Gender == "2")
            {
                return "F";
            }
            else
            {
                return "U";
            }
        }

        public static List<NTE> GetNteFromComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return new List<NTE>();

            // Normalize all line endings to '\n'
            comment = comment.Replace("\r\n", "\n");

            // Now split cleanly
            string[] parts = comment.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var nteList = new List<NTE>(parts.Length);
            foreach (var text in parts)
                nteList.Add(new NTE(text));

            return nteList;
        }


        public static string PrependZeros(string strTestCode)
        {
            if (Strings.Len(strTestCode) < 4)
            {
                while (Strings.Len(strTestCode) != 4)
                    strTestCode = "0" + strTestCode;
                return strTestCode;
            }
            else
            {
                return strTestCode;
            }
        }

    }
}