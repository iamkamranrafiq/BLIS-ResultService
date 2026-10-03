namespace Bioreference.ResultService.Common.Helpers
{
    /// <summary>
    /// Utility class to handle line ending normalization across different platforms.
    /// Specifically designed to preserve Windows-style line endings (\r\n) that are 
    /// stored in the database but get normalized to Unix-style (\n) during XML parsing
    /// as well as Iguana friendly line endings.
    /// </summary>
    public static class LineEndingHelper
    {
        /// <summary>
        /// Normalizes line endings to Windows-style (\r\n).
        /// This is used to restore \r\n line endings after XML parsing has normalized them to \n.
        /// </summary>
        /// <param name="text">The text with potentially normalized line endings</param>
        /// <returns>Text with Windows-style line endings (\r\n)</returns>
        public static string NormalizeToWindows(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // First normalize all line endings to \n to avoid double \r\r\n issues
            // Then convert all \n to \r\n
            return text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        /// <summary>
        /// Normalizes line endings to Iguana-friendly style (\r).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string NormalizeToIguana(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Replace("\r\n", "\r").Replace("\n", "\r");
        }

        /// <summary>
        /// Normalizes line endings to Unix-style (\n).
        /// </summary>
        /// <param name="text">The text to normalize</param>
        /// <returns>Text with Unix-style line endings (\n)</returns>
        public static string NormalizeToUnix(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Detects if the text contains Windows-style line endings.
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if text contains \r\n, false otherwise</returns>
        public static bool HasWindowsLineEndings(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Contains("\r\n");
        }

        /// <summary>
        /// Detects if the text contains Unix-style line endings (that are not part of Windows line endings).
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if text contains standalone \n (not preceded by \r), false otherwise</returns>
        public static bool HasUnixLineEndings(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Check if there's a \n that's not preceded by \r
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    if (i == 0 || text[i - 1] != '\r')
                        return true;
                }
            }

            return false;
        }
    }
}

