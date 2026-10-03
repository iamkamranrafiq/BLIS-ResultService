
namespace Bioreference.ResultService.Application.Model
{
    public class OrderCommentInputModel
    {
        public int OrderId { get; set; }
        public List<OrderCommentCriteria> Criteria { get; set; } = new();
    }
}
