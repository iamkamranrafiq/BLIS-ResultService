

namespace Bioreference.ResultService.Application.Model
{
    public class RapidResultsModel
    {
        public int Id { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public List<AnalyteRRModel> Analytes { get; set; }
 
    }
}
