using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common.Common.RapidResult;

namespace Bioreference.ResultService.Abstractions.Application.RapidResult
{
    public interface IRapidResultService
    {
        public Task<List<RapidResultsModel>> RapidResults(int templateId, bool isCompleted = false, int pageNo = 0, int pageSize = 0);
        public Task<RapidResultTemplates> RapidResultTemplates();
        public Task<List<OutstandingRR>> OutstandingRapidResults(int templateId, int pastDays);
        public Task<bool> DeleteRapidResult(int rapidResultId);
        public Task<RapidResultModel> EventProcessor(EventRapids events);
        public Task<RapidResultModel> RapidResultAccessions(int rapidResultId);
        public Task<CheckAccessionResponseModel> CheckAddedAccession(AccessionOrderModel request);
        public Task<List<RapidControlModel>> RapidResultControl(int templateId);
        public Task<RapidResultTemplateModel> RapidResultTemplateDetail(int templateId);
    }
}
