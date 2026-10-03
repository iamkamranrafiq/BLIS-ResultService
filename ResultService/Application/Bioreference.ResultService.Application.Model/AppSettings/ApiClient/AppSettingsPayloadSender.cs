namespace Bioreference.ResultService.Application.Model.AppSettings.ApiClient
{
    public class AppSettingsPayloadSender
    {
        public const string SectionName = "Bioreference.Payload.Sender.Settings";
        public int DestinationEndPointTimeOutSec { get; set; }
        public bool IgnoreHttps { get; set; }
        public List<SenderUrlMapping> SenderUrlMappings { get; set; }
    }
    public class SenderUrlMapping
    {
        public string Destination { get; set; }
        public string Url { get; set; }
    }
}
