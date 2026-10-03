using Bioreference.ResultService.Application.Model.Lookup.InternalNote;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class CommentModel
    {
        private string _text;

        /// <summary>
        /// Comment text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Text 
        { 
            get => _text;
            set => _text = value.NormalizeToWindows();
        }
        public string Id { get; set; }
        public List<InternalNoteModel> InternalNotes { get; set; }
    }
}
