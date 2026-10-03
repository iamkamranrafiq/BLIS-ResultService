
namespace Bioreference.ResultService.WebAPI.Model
{
    public class COCModel
    {
        public string BatchName { get; set; }
        public string BatchYear { get; set; }
        public string BatchMonth { get; set; }
        public string BatchDay { get; set; }
        public string BatchStartAccession { get; set; }
        public string BatchEndAccession { get; set; }
        public int Id { get;set; }
        public List<COCBatchAccessionModel> COCBatchAccessions { get; set; }

    }
}
