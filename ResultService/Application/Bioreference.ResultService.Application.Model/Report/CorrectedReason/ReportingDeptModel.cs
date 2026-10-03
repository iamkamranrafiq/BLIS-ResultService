namespace Bioreference.ResultService.Application.Model
{ 
    public class ReportingDeptModel
    {
        public int ReportingDeptId { get; set; }
        public string ReportingDeptName { get; set; }
        public string TestCode { get; set; }
        public int ReportId { get; set; }
        public long ReportAnalyteId { get; set; }
    }
}
