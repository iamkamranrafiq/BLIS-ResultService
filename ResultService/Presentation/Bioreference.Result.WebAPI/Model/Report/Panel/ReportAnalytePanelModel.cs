using Bioreference.ResultService.WebAPI.Model;


namespace Bioreference.ResultService.WebAPI.Model
{
    public class ReportAnalytePanelModel
    {
        public bool IsPreliminaryReleased { get; set; }
        public bool MarkedAsPreliminary { get; set; }
        public DateTime ReleaseDate { get; set; }
        public criticalTypeModel CriticalType { get; set; }
        // public Bioreference.LIS.Report Parent { get; set; }
        public ReportAnalytesModel Analytes { get; set; }
        public RefAnalytePanelModel Panel { get; set; }
        public ReportCommentsModel Comments { get; set; }
        public transmitStatusTypeModel TransmitStatus { get; set; }
        public string PanelCode { get; set; }
        public string PanelName { get; set; }
        public string[] OrderingPanelCodes { get; set; }
        public string OrderingCodes { get; set; }
        //  public override bool IsDirty { get; set; }
        public long Id { get; set; }
        public bool IsProcessing { get; set; }
        //   public override object IdentifierId { get; set; }
        public bool ReleaseFromUI { get; set; }
        public bool CanRelease { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool IsCorrected { get; set; }
        //   public List<string> GetFormattedAuditItems { get; set; }
        public bool ToFollowSent { get; set; }
        public long SPMOrderTestId { get; set; }
        public bool IsPresumptiveHold { get; set; }
        public SPMStatusValueModel SPMStatus { get; set; }
        public string ParentTestCode { get; set; }


    }
}
