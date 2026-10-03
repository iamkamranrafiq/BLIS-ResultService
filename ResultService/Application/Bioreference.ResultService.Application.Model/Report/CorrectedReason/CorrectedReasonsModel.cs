using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class CorrectedReasonsModel
    {
        private string _description;

        public string TestCode { get; set; }
        public int ReasonTypeId { get; set; }
        public int PrimaryReasonId { get; set; }
        public int SecondaryReasonId { get; set; }
        public int ControllableId { get; set; }
        public int SignificanceId { get; set; }
        
        public string Description 
        { 
            get => _description;
            set => _description = value.NormalizeToWindows();
        }
        public int ReportId { get; set; }
        public long ReportAnalyteId { get; set; }
        public string AccessionNbr { get; set; }
        public bool IsAgencyReportable { get; set; }
        public string CausedBy { get; set; }
        public bool CriticalResult { get; set; }
        public string ResponsibleLab { get; set; }
        public string OrgPerformingFacility { get; set; }

        public List<DeptResponsibleModel> DeptResponsible { get; set; }
        public List<ReportingDeptModel> ReportingDept { get; set; }



    }
}
