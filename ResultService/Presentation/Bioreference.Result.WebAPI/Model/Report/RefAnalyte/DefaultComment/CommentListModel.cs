using DefaultCommentNamespace = Bioreference.ResultService.WebAPI.Model.Report.RefAnalyte.DefaultComment;

namespace Bioreference.ResultService.WebAPI.Model
{    public class CommentListModel
    {
        public List<DefaultCommentNamespace.CommentModel> DefaultComment { get; set; }
    }
}
