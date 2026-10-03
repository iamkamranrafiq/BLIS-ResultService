
using System.Text.Json.Serialization;

namespace Bioreference.ResultService.Application.Model
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderCommentStatus
    {       
        Add,
        Delete
    }
}
