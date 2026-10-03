using Bioreference.ResultService.Common.Enumerations;
namespace Bioreference.ResultService.Application.Model
{
    public class SendToCHMModel
    {
        public int ReportId { get; set; } = 0;
        public List<CHMDetail> Details { get; set; } = new List<CHMDetail>();
    }
    public class CHMDetail
    {
        public long ReportAnalyteId { get; set; } = 0;
        public long ReportPanelId { get; set; } = 0;
        public ReportSourceType? ReportSourceType { get; set; } = default;
    }
}
