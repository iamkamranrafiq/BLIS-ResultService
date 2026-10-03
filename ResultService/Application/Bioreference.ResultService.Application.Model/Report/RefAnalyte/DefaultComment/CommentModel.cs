using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model.Report.RefAnalyte.DefaultComment
{
    public class CommentModel
    {
        private string _text;

        public int Id { get; set; }
        public string AssignedID { get; set; }
        
        /// <summary>
        /// Comment text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Text 
        { 
            get => _text;
            set => _text = value.NormalizeToWindows();
        }
        public commentTypeModel Type { get; set; }
        public bool IsRepeatable { get; set; }
        public string ExternalCommentCode { get; set; }
        public int ExternalCommentType { get; set; }
    }
}
