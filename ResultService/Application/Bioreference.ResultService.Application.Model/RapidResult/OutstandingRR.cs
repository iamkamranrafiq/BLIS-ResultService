

namespace Bioreference.ResultService.Application.Model
{
    public class OutstandingRR
    {
        public string AccessionNbr { get; set; }
        public DateTime DateServiced { get; set; }
        public string PatientName { get; set; }
        public string AnalyteCode { get; set; }
        public string PerformingFacility { get; set; }
        public string ResultStatus { get; set; }
    }
}
