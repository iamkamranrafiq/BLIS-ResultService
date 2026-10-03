namespace Bioreference.ResultService.Application.Model
{ 
    public class RequisitionStatusSetting
    {
        public int ExcludedHours { get; set; }
        public int LookBackHours { get; set; }
        public string RequisitionClientId { get; set; }
        public string RequisitionClientSecret { get; set; }
        public string RequisitionGrantType { get; set; }
        public string RequisitionStatusURL { get; set; }
        public string RequisitionTokenURL { get; set; }
    }
}
