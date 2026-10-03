using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class OrderCommentModel
    {
        private string _text;

        public string ExtApplicationType { get; set; }

        public bool IsAutoAdded { get; set; }
       
        /// <summary>
        /// Comment text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Text 
        { 
            get => _text;
            set => _text = value.NormalizeToWindows();
        }

        public DateTime DateCreated { get; set; }

        public long ID { get; set; }

        public string ExternalCommentCode { get; set; }

        
    }

}
