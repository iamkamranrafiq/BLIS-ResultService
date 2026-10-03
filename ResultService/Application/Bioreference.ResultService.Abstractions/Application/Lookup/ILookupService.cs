
using Bioreference.Common.TestMaster;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Application.Model.Lookup;

namespace Bioreference.ResultService.Abstractions.Application.Lookup
{
    public interface ILookupService
    {
        public Task<TestCodeGroupsModel> GetTestCodeGroups(bool forSecurity, bool forGrouping);
        public Task<List<CommentModel>> GetComments(CommentTypeModel commentType);        
        public Task<Dictionary<int, string>> GetDivisions();
        public Task<List<ResultService.Application.Model.Lookup.PendingList>> GetPendingList();
        public Task<List<PendingListItemLite>> GetPendingListItems(int id);

    }
}
