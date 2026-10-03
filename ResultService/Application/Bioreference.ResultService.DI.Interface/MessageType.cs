using System.Text.Json.Serialization;

namespace Bioreference.ResultService.DI.Interface
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageType
    {
        ORU,
        SSU,
        FUNC,
        ORM
    }
}
