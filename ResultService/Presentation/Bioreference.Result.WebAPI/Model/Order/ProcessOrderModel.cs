using Bioreference.ResultService.Common;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class ProcessOrderModel
    {
        public ProcessOrderMessageModel message {  get; set; }
        public ProcessOrderConfiguration processOrderConfig { get; set; }
    }
}
