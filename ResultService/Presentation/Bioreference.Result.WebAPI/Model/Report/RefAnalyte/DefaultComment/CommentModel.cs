using Bioreference.ResultService.WebAPI.Model;

namespace Bioreference.ResultService.WebAPI.Model.Report.RefAnalyte.DefaultComment
{
    public class CommentModel
    {
        public int Id { get; set; }
        public string AssignedID { get; set; }
        public string Text { get; set; }
        public commentTypeModel Type { get; set; }
        public bool IsRepeatable { get; set; }
        public string ExternalCommentCode { get; set; }
        public int ExternalCommentType { get; set; }
    }
}
