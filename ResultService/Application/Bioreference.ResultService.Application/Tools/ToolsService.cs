using AutoMapper;
using Bioreference.Common.TestMaster;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Tools;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Tools;
using Bioreference.RuleEngine;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ResultService.Application.Tools
{
    public class ToolsService : IToolsService
    {
        private readonly IMapper _mapper;
        public ToolsService(IMapper mapper)
        {
            _mapper = mapper;
        }
        public async Task<ViewRulesModel> GetRules(int ruleSetId, int pageNo, int pageSize)
        {
            ViewRulesModel viewRules = new ViewRulesModel();
            var rules = await Task.Run(() => RuleManager.Fetch(false, ruleSetId, pageNo, pageSize));
            viewRules.LastUpdated = await Task.Run(() => Bioreference.UI.UpdateStatus.Fetch($"B2.Bioreference.RuleEngine.Rules." + ruleSetId.ToString()).LastUpdated);

            var mappedResponse = _mapper.Map<List<RuleDTO>>(rules.Rules);
            viewRules.Rules = mappedResponse;

            return viewRules;
        }
        public async Task<List<RuleSetDto>> GetRuleSet()
        {
            var rules = RuleSets.Fetch();
            var mappedResponse = _mapper.Map<List<RuleSetDto>>(rules.List);
            return await Task.FromResult(mappedResponse);
        }
        public async Task<List<TNPSearchModel>> TNPSearch(TNPSearchCriteria request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.EndDate < request.StartDate)
                throw new ArgumentException("Invalid date range: End date must be after start date");

            List<TNPSearchModel> tNPSearchResponses = new List<TNPSearchModel>();
            var pendingListsLite = await Task.Run(() => PendingListsLite.Fetch());
            var selectedPendingListLite = pendingListsLite?.List
                ?.FirstOrDefault(pendingList => pendingList.PendingListId == request.PendlingListId);

            var response = await Task.Run(() => TNPPendings.Fetch(request.StartDate, request.EndDate, selectedPendingListLite, request.Account,request.TestCodes, request.PageNo,request.PageSize));
            if (response != null)
            {
                tNPSearchResponses = _mapper.Map<List<TNPSearchModel>>(response.List);
            }
            return tNPSearchResponses;
        }

        public async Task<bool> ReleaseToReporting(List<ReleaseToReportingModel> releaseToReportingModel)
        {
            try
            {
                // Validate input
                if (releaseToReportingModel == null || !releaseToReportingModel.Any())
                {
                    return false;
                }

                bool isSuccess = false;
                foreach(var report in releaseToReportingModel)
                {
                    if (report.OrderID <= 0)
                    {
                        continue; // Skip invalid order IDs
                    }

                    try
                    {
                        //TODO: CLAPP-2520 - update user name
                        await Task.Run(() => LIS.Order.ReleaseToReporting(report.OrderID, report.IsReportHold, ""));
                        isSuccess = true;
                    }
                    catch
                    {
                        // Log error but continue processing other orders
                        continue;
                    }
                }

                return isSuccess;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ReleaseBulkTNP(List<BulkTNPReleaseModel> bulkTNPReleaseModel)
        {
            try
            {
                // Validate input
                if (bulkTNPReleaseModel == null || !bulkTNPReleaseModel.Any())
                {
                    return false;
                }

                bool isSuccess = false;
                
                foreach (var tnp in bulkTNPReleaseModel)
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(tnp.AccessionNumber) || 
                        string.IsNullOrWhiteSpace(tnp.TestCode))  
                        //string.IsNullOrWhiteSpace(tnp.PanelCode))
                    {
                        continue; // Skip invalid records
                    }

                    try
                    {
                        ReportAnalyte analyte = null;
                        var report = await Task.Run(() => LIS.Report.Fetch(tnp.AccessionNumber));

                        if (report == null)
                        {
                            continue;
                        }

                        // Process analytes
                        if (report.Analytes?.List != null)
                        {
                            foreach (ReportAnalyte a in report.Analytes.List)
                            {
                                if (a?.Code == tnp.TestCode)
                                {
                                    analyte = a;
                                    break;
                                }
                            }
                        }

                        if (analyte != null)
                        {
                            analyte.SetResultValue(tnp.CommentType, true);

                            if (!string.IsNullOrEmpty(tnp.Comment) && tnp.Comment != "\r\n")
                            {
                                analyte.Comments.AddComment(tnp.Comment);
                            }

                            analyte.MarkAsReleased();
                        }

                        // Process panels
                        if (report.AnalytePanels?.List != null)
                        {
                            ReportAnalyte lastAnalyte = null;
                            foreach (ReportAnalytePanel panel in report.AnalytePanels.List)
                            {
                                if (panel?.PanelCode == tnp.PanelCode && !panel.HasBeenReleased())
                                {
                                    if (panel.Analytes?.List != null)
                                    {
                                        foreach (ReportAnalyte a in panel.Analytes.List)
                                        {
                                            if (a?.Analyte != null && ((RefAnalyte)a.Analyte).IsRequired)
                                            {
                                                a.SetResultValue(tnp.CommentType, true);
                                                lastAnalyte = a;
                                            }
                                        }

                                        if (lastAnalyte != null && !string.IsNullOrEmpty(tnp.Comment) && tnp.Comment != "\r\n")
                                        {
                                            lastAnalyte.Comments.AddComment(tnp.Comment);
                                        }

                                        panel.MarkAsReleased();
                                    }
                                }
                            }
                        }

                        report.Save();
                        isSuccess = true;
                    }
                    catch
                    {
                        // Log error but continue processing other records
                        continue;
                    }
                }

                return isSuccess;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
