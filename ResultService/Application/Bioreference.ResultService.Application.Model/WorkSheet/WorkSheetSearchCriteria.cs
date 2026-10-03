
namespace Bioreference.ResultService.Application.Model
{
    public class WorkSheetSearchCriteria
    {
        public int TemplateId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
