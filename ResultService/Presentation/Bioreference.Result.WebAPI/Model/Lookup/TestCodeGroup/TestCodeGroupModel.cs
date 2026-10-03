
namespace Bioreference.ResultService.WebAPI.Model
{
    public class TestCodeGroupModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TestCodeGroupItemModel> TestCodeList { get; set; }
    }
}
