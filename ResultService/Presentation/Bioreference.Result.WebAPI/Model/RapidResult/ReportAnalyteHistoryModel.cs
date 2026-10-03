namespace Bioreference.ResultService.WebAPI.Model
{
    public class ReportAnalyteHistoryModel
    {
        public long ReportAnalyteId { get; set; }

        public string ResultValue { get; set; }

        public DateTime EnteredDate { get; set; }

        public string EnteredBy { get; set; }

    }
}
