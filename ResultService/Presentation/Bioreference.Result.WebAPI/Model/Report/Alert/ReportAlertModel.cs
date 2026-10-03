namespace Bioreference.ResultService.WebAPI.Model
{
    public class ReportAlertModel
    {
        public long Id { get; set; }
        public AlertModel Alert { get; set; }
        public string AlertCode { get; set; }
        public object IdentifierId { get; set; }
        public List<string> GetFormattedAuditItems { get; set; }

    }
}
