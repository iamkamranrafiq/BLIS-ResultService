namespace Bioreference.ResultService.WebAPI.Model
{
    public class AttachmentModel
    {
        public long Id { get; set; }
        public int AttachmentTypeId { get; set; }
        public string AttachmentTypeDescription { get; set; }
        public string AttachmentDescription { get; set; }
        public object IdentifierId { get; set; }
        public byte[] FileContents { get; set; }
        public string FileContentsBase64 { get; set; }
        public List<string> GetFormattedAuditItems { get; }

    }
}
