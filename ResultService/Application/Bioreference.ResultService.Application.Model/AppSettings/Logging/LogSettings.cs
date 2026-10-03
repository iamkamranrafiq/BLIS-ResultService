namespace Bioreference.ResultService.Application.Model.AppSettings.Logging
{
    public class LogSettings
    {
        public const string SectionJobLogging = "Bioreference.JobLogging";

        public const string SectionConsumerLogging = "Bioreference.ConsumerLogging";
        public string LoggingName { get; set; } = string.Empty;
    }
}
