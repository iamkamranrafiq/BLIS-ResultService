
using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class ReportModel //: AuditDataClassBase
    {
        private string _specimenComment;
        private string _specimenSummary;

        public long Id { get; set; }
        public long OrderId { get; set; }
        public string AccessionNumber { get; set; }
        public string AccountNumber { get; set; }
        public DateTime ResultDate { get; set; }
        public GenderModel Gender { get; set; }
        public string DOB { get; set; }
        public int AgeNumber { get; set; }
        public string AgeType { get; set; }
        
        /// <summary>
        /// Specimen comment with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string SpecimenComment 
        { 
            get => _specimenComment;
            set => _specimenComment = value.NormalizeToWindows();
        }
        
        /// <summary>
        /// Specimen summary with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string SpecimenSummary 
        { 
            get => _specimenSummary;
            set => _specimenSummary = LineEndingHelper.NormalizeToWindows(value);
        }
        public bool IsCOC { get; set; }

        public bool Has4kTest { get; set; }
        public int ParentReportId { get; set; }
        public int ChildReportId { get; set; }
        public DateTime DateServiced { get; set; }
        public DateTime CalcDobFromDate { get; set; }
        public bool BypassRules { get; set; }
        public bool HasCalcAnalytes { get; set; }
        public List<string> AuditItems { get; set; }
        public string AccessionIdentifier { get; set; }
        public string ParentIdentifierId { get; set; }
        public parentIdentifierTypeModel AccessionIdentifierType { get; set; }
        public Dictionary<string, string> CorrectedTests { get; set; }
        public Dictionary<string, string> CorrectedPanelTests { get; set; }
        public bool IsRuleExecution { get; set; }
       // public List<ReportCommentGroup> CommentGroups { get; set; }
       
        public string EnterersLocationType { get; set; }
        public string EnterersLocation { get; set; }
        public string EnterersFirstName { get; set; }
        public string EnterersLastName { get; set; }
        public bool AgeExists { get; set; }
        public bool AgeNotExists { get; set; }
        public List<string> GetFormattedAuditItems { get; set; }
        public ReportAnalytesModel Analytes { get; set; }
        public ReportAnalytePanelsModel AnalytePanels { get; set; }
        public ReportCommentsModel Comments { get; set; }

        public List<PriorResultModel> PriorResults { get; set; }
  
    }
}
