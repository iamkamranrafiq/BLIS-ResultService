
namespace Bioreference.ResultService.Application.Model
{
    public class AppSettingsInboundCompleted
    {
        public const string SectionName = "Bioreference.InboundCompletedProcessor";
        public string? AuditChannelPath { get; set; }
        public string? AuditFilePrefix { get; set; }
    }
}
