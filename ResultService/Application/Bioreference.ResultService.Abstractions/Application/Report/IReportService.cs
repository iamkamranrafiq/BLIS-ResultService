using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using System.Collections;
using Bioreference.ResultService.Common.Common.Report;
using Bioreference.ResultService.Application.Model.Report.ClinicalTrials;
using Bioreference.ResultService.Application.Model.Report.PriorResult;

namespace Bioreference.ResultService.Abstractions.Application.Report
{
    public interface IReportService
    {
        public Task<ReportModel> ReportFetch(string accessionNbr, DateTime dateServiced);
        public Task<ReportModel> ReportFetchByReportId(int reportId, bool useCache = false, string cacheKey = null);
        public Task<ReportModel> ResultUpdate(EventResults events, bool useCache = false, string cacheKey = null);
        public Task<List<ReportInfoSearchCriteria>> FetchReports(string analyteCode, transmitStatusType status, bool searchPanels);
        public Task<CriteriaDefinitionModel> FetchCriteria(string className);
        public Task<bool> ResetRefAnlayte(string testCode, List<long> reportIds);
        public Task<List<string>> GetAuditUsers(int reportId);
        public Task<CorrectedReasonMappingsModel> CorrectedReasonMapping();
        public Task<List<DictionaryEntry>> DepartmentResponsible();
        public Task<List<DictionaryEntry>> ReportingDepartment();
        public Task<List<DictionaryEntry>> GetResponsibleLabsDropdown();
        public Task<List<DictionaryEntry>> GetSignificancesDropdown();
        public Task<bool> SaveCorrectedReasons(List<CorrectedReasonsModel> correctedReasonList);
        public Task<bool> SendToCHM(SendToCHMModel sendToCHM);
        public Task<List<ClinicalTrialsResponseModel>> GetClinicalTrailsData(ClinicalTrialsRequestModel clinicalTrialsRequestModel);
        public Task<List<PriorResultModel>> GetPriorResults(PriorResultRequestModel priorResultRequest);
    }
}
