using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model.COC
{
    public class COCAddEditResponseModel
    {
        private string _message;

        /// <summary>
        /// Message with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Message 
        { 
            get => _message;
            set => _message = value.NormalizeToWindows();
        }
        public CocBatchCreateStatusModel Status { get; set; }
        public COCModel COCModel { get; set; }
    }
}
