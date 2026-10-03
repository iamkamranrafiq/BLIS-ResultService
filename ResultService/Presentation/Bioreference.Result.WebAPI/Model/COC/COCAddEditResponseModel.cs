using Bioreference.ResultService.Application.Model.Enum;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class COCAddEditResponseModel
    {
        public string Message { get; set; }
        public CocBatchCreateStatusModel Status { get; set; }
        public COCModel COCModel { get; set; }
    }
}
