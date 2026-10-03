namespace Bioreference.ResultService.Application.Model
{
    public class TestCodeGroupModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TestCodeGroupItemListModel TestCodeList { get; set; }
    }
}
