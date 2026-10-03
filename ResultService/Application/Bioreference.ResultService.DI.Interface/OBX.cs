namespace Bioreference.ResultService.DI.Interface
{
    public class OBX 
    {
        public string ValueType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Units { get; set; } = string.Empty;
        public string ReferenceRange { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
        public string ReferenceLabId { get; set; } = string.Empty;
        public string Set_ID { get; set; } = string.Empty;
        public string MachineSequence { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string RackPosition { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public string Comments_Reference { get; set; } = string.Empty;

        //OBX Fields
        public string ReportingType { get; set; } = string.Empty;
        public string TechUser { get; set; } = string.Empty;
        public string ReleaseUser { get; set; } = string.Empty;
        public string PerformLocation { get; set; } = string.Empty;
        public string TechUserFName { get; set; } = string.Empty;
        public string TechUserMName { get; set; } = string.Empty;
        public string COCReleasedDate { get; set; } = string.Empty;
        public string AttachmentMultipart { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
        public string AttachmentBase { get; set; } = string.Empty;
        public string ProducerName { get; set; } = string.Empty;
        public string PresumptiveHold { get; set; } = string.Empty;
        public string SPMTNP { get; set; } = string.Empty;
    }
}
