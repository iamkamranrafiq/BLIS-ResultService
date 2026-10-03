
using Newtonsoft.Json;

namespace Bioreference.ResultService.DI.Interface
{
    public class LabReport
    {
        [JsonProperty(PropertyName = "LabReport")]
        public OBR Obr { get; set; } = new();
        public List<LabComment> LabReportComment { get; set; } = new();

        [JsonProperty(PropertyName = "Results")]
        public List<Result> Result { get; set; } = new();
    }
}
