using Bioreference.LIS;
using Bioreference.ResultService.Application.Model.COC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.COC
{
    public interface ICOCService
    {
        public Task<List<COCBatchReportModel>> FetchCOCReport(bool isShowClosed, int pageNo, int pageSize);
        public Task<List<COCBatchReportItemModel>> FetchCOCReportItem(int batchId);
        public Task<COCAddEditResponseModel> CreateCOCSequence(COCSearchCriteria cocSearchCriteria);
        public Task<CocBatchAccessionEditStatusModel> AddCOCAccession(string accessionNumber, int batchId);
        public Task<CocBatchAccessionEditStatusModel> RemoveCOC(string accessionNumber, int batchId);
        public Task<COCAddEditResponseModel> ModifyCOCSequence(COCSearchCriteria cocSearchCriteria);
        public Task<bool> ReleaseBatch(List<int> reports);
        public Task<CocBatchAccessionEditStatusModel> ClosedReOpenBatch(bool isClosed, int batchId);
        public Task<bool> IsCocApprover(string userName);
    }
}
