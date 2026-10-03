using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Brad
{
    public interface IBradService
    {
        public Task<List<SpecimenTypeInfo>> GetSpecimenTypeList();
        public Task<List<string>> GetWalkInFridges();
        public Task<List<FridgeInfoModel>> GetFridge();
        public Task<List<SpecimenMappedInfo>> GetMappedSpecimens(List<PendingBradSearchCriteria> SpecList);
        public Task<List<RequestSpecimenResponseStatus>> SendToBRAD(CallToBradSearchCriteria sendToBradDTO);
        public Task<RequestSpecimenInfo> CheckRequestable(List<PendingBradSearchCriteria> SpecList);
        public Task<Dictionary<int, string>> GetRequestDepartments(List<PendingBradSearchCriteria> SpecList);
        public Task<List<string>> GetMappedAccessions(List<List<string>> splits, string fridgeId);
    }
}
