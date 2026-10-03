namespace Bioreference.ResultService.DI.Interface
{
    public class MSH
    {
        public string MessageType { get; set; } = string.Empty;

        public string MessageEvent { get; set; } = string.Empty;

        public string SendingApplication { get; set; } = string.Empty;

        public string SendingFacilityCode { get; set; } = string.Empty;

        public string SendingFacility { get; set; } = string.Empty;

        public string ReceivngApplication { get; set; } = string.Empty;

        public string ReceivingFacility { get; set; } = string.Empty;

        public string MessageTime { get; set; } = string.Empty;

        public string MessageID { get; set; } = string.Empty;

        public string VersionId { get; set; } = string.Empty;

        public string AcceptAckType { get; set; } = string.Empty;

        public string ProcessingId { get; set; } = string.Empty;
        public string SequenceNumber { get; set; } = string.Empty; 
    }
}
