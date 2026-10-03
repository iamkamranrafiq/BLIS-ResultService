namespace Bioreference.ResultService.WebAPI.Model
{
    public class CommentModel
    {
        public string Text { get; set; }
        public string Id { get; set; }
        public List<InternalNoteModel> InternalNotes { get; set; }
    }
}
