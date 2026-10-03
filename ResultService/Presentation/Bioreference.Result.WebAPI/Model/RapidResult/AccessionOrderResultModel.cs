using Bioreference.ResultService.WebAPI.Model;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class AccessionOrderResultModel
    {
        public string? AccessionNbr { get; set; }
        public bool IsControl { get; set; }
        public AddAccessionStatusTypeModel Status { get; set; }
    }
}
