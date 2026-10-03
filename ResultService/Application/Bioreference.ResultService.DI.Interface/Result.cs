

using Newtonsoft.Json;

namespace Bioreference.ResultService.DI.Interface
{
    public class Result
    {
        [JsonProperty(PropertyName = "Result")]
        public List<OBX> Obx { get; set; } = new();
        public List<FlagGroup> FlagGroup { get; set; } = new();
        public List<LabComment> ResultComment { get; set; } = new();
    }
}
