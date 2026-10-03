namespace Bioreference.ResultService.Application.Model
{
    public class RevisedReportEngineSetting
    {
        public int RevisedReportDuration { get; set; }
        public string RevisedReportFromEmail { get; set; }
        public string RevisedReportMailBody { get; set; }
        public string RevisedReportMailSubject { get; set; }
        public string RevisedReportToEmail { get; set; }
        public string SMTPHost { get; set; }

    }
}
