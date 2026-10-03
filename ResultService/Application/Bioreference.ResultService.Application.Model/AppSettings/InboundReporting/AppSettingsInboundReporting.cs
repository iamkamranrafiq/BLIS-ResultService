namespace Bioreference.ResultService.Application.Model.AppSettings.InboundReporting
{
    public class AppSettingsInboundReporting
    {
        public const string SectionName = "Bioreference.InboundReportingProcessor";
        public string? AuditChannelPath { get; set; }
        public string? AuditFilePrefix { get; set; }
    }
}
