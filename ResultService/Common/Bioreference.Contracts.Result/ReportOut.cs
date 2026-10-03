namespace Bioreference.Contracts.Result
{
    public class ReportOut
    {
        public string AccessionNumber { get; set; }
        public long ReportId { get; set; }
        public bool IsReportable { get; set; }
        public int DataType { get; set; }
    }
}
