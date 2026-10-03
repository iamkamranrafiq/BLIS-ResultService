namespace Bioreference.ResultService.DI.Interface
{
    public class LabAccession
    {
        public string ClientOrderNumber { get; set; } = string.Empty;
        public string AccessionNumber { get; set; } = string.Empty;
        public string ReferenceLabNumber { get; set; } = string.Empty;
        public string ClientAccountNumber { get; set; } = string.Empty;
        public string OrderingDoctorLast { get; set; } = string.Empty;
        public string OrderingDoctorFirst { get; set; } = string.Empty;
        public string OrderingDoctorMiddle { get; set; } = string.Empty;
        public string OrderingDoctorName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string CollectDate { get; set; } = string.Empty;
        public string ReceivedDate { get; set; } = string.Empty;
        public string AltField1 { get; set; } = string.Empty;
        public string AltField2 { get; set; } = string.Empty;
        public string AltField3 { get; set; } = string.Empty;
        public string Set_ID { get; set; } = string.Empty;
    }
}
