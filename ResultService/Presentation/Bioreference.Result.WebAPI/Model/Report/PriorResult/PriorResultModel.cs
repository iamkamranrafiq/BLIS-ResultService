
namespace Bioreference.ResultService.WebAPI.Model
{
    public class PriorResultModel
    {
        public int ReportId { get; set; }
        public long ReportAnalyteId { get; set; }
        public string AnalyteCode { get; set; }
        public long EUID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string ResultValue { get; set; }
        public int ResultStatus { get; set; }
        public string ResultStatusStr { get; set; }
        public int OrderId { get; set; }
        public string AccessionNbr { get; set; }
        public DateTime? DateServiced { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string ResultReleasedUser { get; set; }
        public string PriorEuidResultValue { get; set; }
        public string DOB { get; set; }
        public string DeltaHoldRule { get; set; }
        public string DeltaRuleChange { get; set; }

    }

}
