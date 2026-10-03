using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.QueueOrder
{
    public class QueueOrderInEngineLISSettings
    {
        public bool AddTestFromTestMaster { get; set; }
        public string AutomatedCorrectedResultComment { get; set; }
        public string AutomatedRevisedAnalyteComment { get; set; }
        public string AutomatedRevisedComment { get; set; }
        public List<string> B24KRecalcBypass { get; set; }
        public string B24KToken { get; set; }
        public int CommentUpdateMaxLength { get; set; }
        public List<string> DefaultDivisionCode { get; set; }
        public string DemographicUpdateAutoComment { get; set; }
        public string DemographicUpdateAutoCommentCode { get; set; }
        public string DemographicUpdateAutoCommentForPanel { get; set; }
        public string DemoRefRangeTblChangeAutoComment { get; set; }
        public string DemoRefRangeTblChangeAutoCommentPanel { get; set; }
        public bool DisableAutoReleaseWithAlerts { get; set; }
        public int DuplicateAccessionTimeSpan { get; set; }
        public string EnvironmentName { get; set; }
        public List<string> FacilitiesToFollow { get; set; }
        public List<string> FourK_AOE_Notify { get; set; }
        public List<string> FourK_Biopsy_Missing { get; set; }
        public List<string> FourK_Biopsy_Negative { get; set; }
        public List<string> FourK_Biopsy_None { get; set; }
        public List<string> FourK_Biopsy_Positive { get; set; }
        public List<string> FourK_DRE_Nodule { get; set; }
        public List<string> FourK_DRE_NoInfo { get; set; }
        public List<string> FourK_DRE_NoNodule { get; set; }
        public int FourK_Minimum_Age { get; set; }
        public List<string> FourK_Panel_OverrideValidation { get; set; }
        public string Inbound4KAPIToken { get; set; }
        public int LimitDayRange { get; set; }
        public List<string> NotPerformedResult { get; set; }
        public bool OverrideReleaseUser { get; set; }
        public int PriorResultLookupRange { get; set; }
        public string ReleaseEngine4KAPIToken { get; set; }
        public bool ReleaseOnDemographicUpdate { get; set; }
        public bool ReportAnalyteBatching { get; set; }
        public int RuleSetChangeResult { get; set; }
        public int RuleSetRelease { get; set; }
        public bool SearchAccessionWithDOS { get; set; }
        public string SMTP { get; set; }
        public List<string> SPMATPTriggers { get; set; }
        public bool SPMTNP_RuleEngine { get; set; }
        public List<string> SPMTNPAbortActions { get; set; }
        public List<string> SPMTNPTriggers { get; set; }
        public List<string> SuppressCommentByResult { get; set; }
        public List<string> TestCodeListForPrelimRelease { get; set; }
        public bool ThrowExeptionForCurrentFinal { get; set; }
        public bool ToFollowEnabled { get; set; }
        public bool ToFollowJ3333Enabled { get; set; }
    }
}
