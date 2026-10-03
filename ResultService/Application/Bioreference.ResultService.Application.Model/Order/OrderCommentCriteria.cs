
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class OrderCommentCriteria
    {
        private string _text = string.Empty;

        public int Id { get; set; }
        
        /// <summary>
        /// Comment text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Text 
        { 
            get => _text;
            set => _text = value.NormalizeToWindows() ?? string.Empty;
        }
        public OrderCommentStatus Status { get; set; }
    }
}
