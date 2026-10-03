
namespace Bioreference.ResultService.WebAPI.Model
{
    public class ResultModel
    {
        public string FlagValue { get; set; }
        public int ID { get; }
        public bool IsFlagValue { get; }
        public bool RequiresManualReview { get; }
        public string[] Comments { get; }
        public string Value { get; set; }
        public int OrderIndex { get; set; }
    }
}
