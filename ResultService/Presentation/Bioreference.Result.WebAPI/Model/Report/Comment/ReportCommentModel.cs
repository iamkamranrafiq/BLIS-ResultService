using Bioreference.ResultService.WebAPI.Model;


namespace Bioreference.ResultService.WebAPI.Model
{
    public class ReportCommentModel //: AuditDataClassBase
    {
        //  public DataClassBase Parent { get; set; }
        public CommentTypeModel CommentType { get; set; }
        public bool IsAutoAdded { get; set; }
        public string Text { get; set; }
        public DateTime DateCreated { get; set; }
        public long ID { get; set; }
        //   public override  object IdentifierId { get;  }
        protected object ParentIdentifierId { get; set; }
        protected long ObjectLookupIdentifier { get; set; }
        protected int ObjectLookupType { get; set; }
        public string ExternalId { get; set; }
        public ExternalCommentTypeModel ExternalCommentType { get; set; }
        public string ExternalCommentCode { get; set; }
        public int Priority { get; set; }
        public List<string> GetFormattedAuditItems { get; set; }
        public int InternalNoteId { get; set; }
        public string InternalNote { get; set; }
        public string CompareObjectDiplayName { get; set; }

    }
}
