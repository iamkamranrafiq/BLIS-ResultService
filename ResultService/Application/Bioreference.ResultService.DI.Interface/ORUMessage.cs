using Newtonsoft.Json;

namespace Bioreference.ResultService.DI.Interface
{
    public class ORUMessage
    {
        public MSH MSH { get; set; } = new();
        public PatientInfo PatientInfo { get; set; } = new();
        public List<PatientComment> PatientInfoComments { get; set; } = new();
        public PatientVisit PatientVisit { get; set; } = new();
        public List<LabAccession> LabAccession { get; set; } = new();
        public List<LabComment> LabAccessionComment { get; set; } = new();

        [JsonProperty(PropertyName = "LabReports")]
        public List<LabReport> LabReport { get; set; } = new();
        public Order Order { get; set; } = new Order();
        public List<OrderComment> OrderComment { get; set; } = new();
    }
}
