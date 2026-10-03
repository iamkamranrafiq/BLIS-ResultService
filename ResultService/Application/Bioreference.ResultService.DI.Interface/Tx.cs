namespace Bioreference.ResultService.DI.Interface
{
    public class Tx
    {
        public string TestCode { get; set; } = string.Empty;
        public string TestDescription { get; set; } = string.Empty;
        public string SetId { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime OrderDateTime { get; set; } = DateTime.MinValue;
        public DateTime CollectDatetime { get; set; } = DateTime.MinValue;
        public string PlacerOrderNumber { get; set; } = string.Empty;
        public string SpecimenType { get; set; } = string.Empty;
        public string ClientAccountNumber { get; set; } = string.Empty;
        public string OrderingDoctorLast { get; set; } = string.Empty;
        public string OrderingDoctorFirst { get; set; } = string.Empty;
        public string OrderingDoctorMiddle { get; set; } = string.Empty;
    }

}
