using DefaultCommentNamespace = Bioreference.ResultService.Application.Model.Report.RefAnalyte.DefaultComment;

namespace Bioreference.ResultService.Application.Model
{    public class CommentListModel
    {
        public List<DefaultCommentNamespace.CommentModel> DefaultComment { get; set; }
    }
}
