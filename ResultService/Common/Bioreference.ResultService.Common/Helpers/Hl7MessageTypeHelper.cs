using Bioreference.ResultService.DI.Interface;
using Newtonsoft.Json.Linq;

namespace Bioreference.ResultService.Common.Helpers
{
    public static class Hl7MessageTypeHelper
    {
        public static MessageType? GetMessageType(string mappedJson)
        {
            var jsonObject = JObject.Parse(mappedJson);
            var messageTypeToken = jsonObject.SelectToken("MSH.MessageType")
                                 ?? jsonObject.SelectToken("MSH[0].MessageType");

            if (Enum.TryParse(messageTypeToken?.ToString(), true, out MessageType type))
                return type;

            return null;
        }
    }
}
