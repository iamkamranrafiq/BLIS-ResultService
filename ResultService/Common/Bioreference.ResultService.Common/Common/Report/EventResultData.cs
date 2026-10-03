using Bioreference.ResultService.Common.Enumerations;
namespace Bioreference.ResultService.Common.Common.Report
{
    public class EventResultData
    {
        public string CommentId { get; set; } = string.Empty;
        public string PanelCode { get; set; } = string.Empty;
        public string AnalyteCode { get; set; } = string.Empty;
        public long ReportAnalyteId { get; set; } = 0;
        public long ReportPanelId { get; set; } = 0;
        public int InternalNoteId { get; set; } = 0;
        public string InternalNoteTxt { get; set; } = string.Empty;
        public string CommentTxt { get; set; } = string.Empty;
        public string UpdatedValue { get; set; } = string.Empty;
        public string UserDivisionCode { get; set; } = string.Empty;
        public bool IsTMComment { get; set; } = false;
        public bool IsLabComment { get; set; } = false;
        public bool IsTNPComment { get; set; } = false;
        public ReportSourceType? ReportSourceType { get; set; } = default;
        public ReportCommentType? ReportCommentType { get; set; } = default;
    }


}
