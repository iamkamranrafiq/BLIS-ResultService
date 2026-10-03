
namespace Bioreference.ResultService.Application.Model
{
    public class TestAnalytePanelModel : AnalytePanelModel
    {
        public long Id { get; set; }
        public string PanelCode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string ReferenceLabCode { get; set; }
        public int ReferenceLabId { get; set; }
        public bool IsReportable { get; set; }
        public int OutboundChannelId { get; set; }
        public bool IsAgencyReportable { get; set; }
        public string RefLabOrderingCode { get; set; }
        public bool AllowPreliminaryRelease { get; set; }
        public string AltInboundTestCode { get; set; }
        public string AltOutboundTestCode { get; set; }
        public string AltOutboundDescription { get; set; }
        public string DepartmentShortName { get; set; }
    }
}
