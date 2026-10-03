using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Common.Helpers;


namespace Bioreference.ResultService.Application.Model
{
    public class ReportCommentModel //: AuditDataClassBase
    {
        private string _text;
        private string _internalNote;
        private string _compareObjectDiplayName;

        //  public DataClassBase Parent { get; set; }
        public CommentTypeModel CommentType { get; set; }
        public bool IsAutoAdded { get; set; }
        
        /// <summary>
        /// Comment text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Text 
        { 
            get => _text;
            set => _text = value.NormalizeToWindows();
        }
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
        
        /// <summary>
        /// Internal note with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string InternalNote 
        { 
            get => _internalNote;
            set => _internalNote = LineEndingHelper.NormalizeToWindows(value);
        }
        
        /// <summary>
        /// Compare object display name with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string CompareObjectDiplayName 
        { 
            get => _compareObjectDiplayName;
            set => _compareObjectDiplayName = LineEndingHelper.NormalizeToWindows(value);
        }

    }
}
