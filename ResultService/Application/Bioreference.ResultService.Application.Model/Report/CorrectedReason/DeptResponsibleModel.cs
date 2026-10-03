namespace Bioreference.ResultService.Application.Model
{
    public class DeptResponsibleModel
    {
        public int DeptResponsibleId { get; set; }
        public string DeptResponsibleName { get; set; }
        public string TestCode { get; set; }
        public int ReportId { get; set; }
        public long ReportAnalyteId { get; set; }
    }
}
