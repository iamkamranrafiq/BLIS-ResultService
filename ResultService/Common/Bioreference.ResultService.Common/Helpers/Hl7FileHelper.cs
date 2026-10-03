namespace Bioreference.ResultService.Common.Helpers
{
    public static class Hl7FileHelper
    {
        public static string GetMappingFilePath(string messageType)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string localDependencyPath = Path.Combine(baseDirectory, "HL7Configuration");

            string fileName = $"{messageType}_Mapping.json";
            string fullPath = Path.Combine(localDependencyPath, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Mapping file not found: {fileName}");

            return fullPath;
        }

        public static string ReadMappingFile(string path)
        {
            return File.ReadAllText(path);
        }

        public static string DateTimeStamp(this string input)
        {
            DateTime d;
            if (!DateTime.TryParse(input, out d))
                return string.Empty;
            return d.ToString("yyyyMMddHHmmss");
        }
    }
}
