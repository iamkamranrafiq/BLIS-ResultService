namespace Bioreference.ResultService.Application.Model.AppSettings.OrderOutFibrosure
{
    public class AppSettingsOrderFibrosureEngine
    {
        public const string SectionName = "Bioreference.OrderOutFibrosureJob";
        public string MSH_SendingApp { get; set; }
        public string MSH_SendingFacilityCode { get; set; }
        public string MSH_ReceivingApp { get; set; }
        public string MSH_ReceivingFacility { get; set; }
        public int LabId { get; set; }
        public string CalculationsTestName { get; set; } 
        public string OrderingCode { get; set; }

    }
}
