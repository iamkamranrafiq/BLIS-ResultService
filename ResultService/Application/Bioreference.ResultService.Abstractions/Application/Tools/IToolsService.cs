using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Tools;
using Bioreference.RuleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Tools
{
    public interface IToolsService
    {
        public Task<ViewRulesModel> GetRules(int ruleSetId, int pageNo, int pageSize);
        public Task<List<TNPSearchModel>> TNPSearch(TNPSearchCriteria request);
        public Task<bool> ReleaseToReporting(List<ReleaseToReportingModel> releaseToReportingModel);
        public Task<bool> ReleaseBulkTNP(List<BulkTNPReleaseModel> bulkTNPReleaseModel);
        public Task<List<RuleSetDto>> GetRuleSet();
    }
}
