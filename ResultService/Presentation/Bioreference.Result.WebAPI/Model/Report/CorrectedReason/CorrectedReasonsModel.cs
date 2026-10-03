namespace Bioreference.ResultService.WebAPI.Model
{
    public class CorrectedReasonsModel
    {
        public string TestCode { get; set; }
        public int ReasonTypeId { get; set; }
        public int PrimaryReasonId { get; set; }
        public int SecondaryReasonId { get; set; }
        public string DeptResponsible { get; set; }
        public int ControllableId { get; set; }
        public int SignificanceId { get; set; }
        public string Description { get; set; }
        public int ReportId { get; set; }
        public int ReportAnalyteId { get; set; }
        public string AccessionNbr { get; set; }
        public bool IsAgencyReportable { get; set; }
        public string CausedBy { get; set; }
        public bool CriticalResult { get; set; }
        public string ReportingDepartment { get; set; }
        public string ResponsibleLab { get; set; }
        public string OrgPerformingFacility { get; set; }

        // Uncomment if you need these:
        // public string NCEField { get; set; }
        // public string Comments { get; set; }
        // public bool NCECritical { get; set; }
    }
}
