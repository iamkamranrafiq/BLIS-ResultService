
namespace Bioreference.ResultService.WebAPI.Model
{
    public class AnalytePanelModel
    {
        public string PanelCode { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public int ReferenceLabId { get; set; }
        public string ReferenceLabCode { get; set; }
        public bool IsReportable { get; set; }
        public int OutboundChannelId { get; set; }
        public bool IsAgencyReportable { get; set; }
    }
}
