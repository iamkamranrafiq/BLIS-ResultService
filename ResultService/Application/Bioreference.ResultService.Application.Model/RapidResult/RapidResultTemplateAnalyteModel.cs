
namespace Bioreference.ResultService.Application.Model
{
    public class RapidResultTemplateAnalyteModel
    {      
        public int Id { get; set; }
        public bool IsReference { get; set; }
        public string AnalyteCode { get; set; }
        public string AnalyteName { get; set; }
        public int SortOrder { get; set; }
        public string DefaultValue { get; set; }
    }
}
