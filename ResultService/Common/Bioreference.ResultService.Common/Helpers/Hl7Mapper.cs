using Newtonsoft.Json;
using HL7Parser.Builders;
namespace Bioreference.ResultService.Common.Helpers
{
    public static class Hl7Mapper
    {
        private static readonly ObjectBuilder _objectBuilder = new();

        public static string? CreateMappedJson(string hl7String, string mappingJson, bool bypassValidation = false)
        {
            return _objectBuilder.CreateMappedHL7Object(hl7String, mappingJson , bypassValidation);
        }

        public static T? Deserialize<T>(string? json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonConvert.DeserializeObject<T>(json);
        }
    }
}
