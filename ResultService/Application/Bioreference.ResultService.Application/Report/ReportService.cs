using AutoMapper;
using Bioreference.Common.Lab;
using Bioreference.Common.TestMaster;
using Bioreference.Contracts.Result;
using Bioreference.Data.Audit;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Common;
using Bioreference.ResultService.Abstractions.Application.Order;
using Bioreference.ResultService.Abstractions.Application.Report;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Report.ClinicalTrials;
using Bioreference.ResultService.Application.Model.Report.PriorResult;
using Bioreference.ResultService.Common.Common.Report;
using Bioreference.ResultService.Common.Enumerations;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.RuleEngine;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Data;

namespace Bioreference.ResultService.Application.Report
{
    public class ReportService : IReportService
    {
        private readonly IMapper _mapper;
        private readonly ResultEventProcessorService _eventProcessorService;
        private readonly IMessageProducer<Incident> _incidentProducer;
        private readonly IMessageProducer<Reflex> _reflexProducer;
        private readonly IOrderService _orderService;
        private readonly ICommonResultService _commonResult;
        private ILogger<ReportService> _logger;
        private DataTable _correctedReasonDeptResponsibleTable = new DataTable();
        private DataTable _correctedReasonReportingDeptTable = new DataTable();
        private readonly ISettingService _settingsProvider;

        public ReportService(IMapper mapper, IOrderService orderService, ILogger<ReportService> logger, ICommonResultService commonResult, ISettingService settingsProvider, ResultEventProcessorService eventProcessorService = null, IProducerProvider provider = null)
        {
            _mapper = mapper;
            _eventProcessorService = eventProcessorService;
            _incidentProducer = provider.GetMessageProducer<Incident>();
            _reflexProducer = provider.GetMessageProducer<Reflex>();
            _orderService = orderService;
            _logger = logger;
            _commonResult = commonResult;
            _settingsProvider = settingsProvider;
        }

        public async Task<ReportModel> ReportFetch(string accessionNbr, DateTime dateServiced)
        {
            Bioreference.LIS.Report report = await Task.Run(() => OrderManager.FetchReport(accessionNbr, dateServiced));
            if (report == null)
                return null;

            AuditManager.LogViewAction(report);
            ActivityHelper.SetAccessionLogKey(report.AccessionNbr);
            _logger.LogInformation(
                "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                report.AccessionNbr,
                DateTime.UtcNow
            );
            _commonResult.UpdateIsBlindInReport(report);
            ReportModel mappedReport = _mapper.Map<ReportModel>(report);
            mappedReport = _commonResult.UpdateResultFlagRangeInReportModel(mappedReport, report);
            return mappedReport;
        }
        public async Task<ReportModel> ReportFetchByReportId(int reportId, bool useCache = false, string cacheKey = null)
        {
            Bioreference.LIS.Report report = await Task.Run(() => Bioreference.LIS.Report.Fetch(reportId, useCache: useCache, cacheKey: cacheKey));
            if (report == null || report.ID == 0)
                return null;
            AuditManager.LogViewAction(report);
            ActivityHelper.SetAccessionLogKey(report.AccessionNbr);
            _logger.LogInformation(
                "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                report.AccessionNbr,
                DateTime.UtcNow
            );
            _commonResult.UpdateIsBlindInReport(report);
            ReportModel mappedReport = _mapper.Map<ReportModel>(report);
            mappedReport= _commonResult.UpdateResultFlagRangeInReportModel(mappedReport, report);
            return mappedReport;
        }
   
       
        public async Task<List<string>> GetAuditUsers(int reportId)
        {
            var auditItems = await Task.Run(() => Data.Audit.AuditItems.Fetch(reportId.ToString(), "Bioreference.LIS.Report"));
            HashSet<string> auditUsersSet = new HashSet<string>();

            foreach (AuditItem user in auditItems.List)
            {
                auditUsersSet.Add(user.UserName);
            }
            return auditUsersSet.ToList();
        }
        public async Task<ReportModel> ResultUpdate(EventResults events, bool useCache = false, string cacheKey = null)
        {
            string connectionString = _settingsProvider.GetConnectionString("Bioreference.LIS");
            ControlSetting controlSetting = await _settingsProvider.FetchSetting<ControlSetting>(connectionString, "");       
              
            Bioreference.LIS.Report report = await Task.Run(() => Bioreference.LIS.Report.Fetch(events.ReportId, useCache: useCache, cacheKey: cacheKey));
            ActivityHelper.SetAccessionLogKey(report.AccessionNbr);
            _logger.LogInformation(
                "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                report.AccessionNbr,
                DateTime.UtcNow
            );
            report.ReflexEvent += Report_ReflexEvent;
            await Task.Run(() => _eventProcessorService.ProcessEvents(events.Events, report));

            if (controlSetting != null && controlSetting.IncidentRevisedResult)
            {
                if (events.Events.Any(p => p.Type == EventType.SaveReport))
                {
                    await ProduceIncidentRevisedResult(report);
                }
            }
            _commonResult.UpdateIsBlindInReport(report);
            ReportModel mappedReport = _mapper.Map<ReportModel>(report);
            mappedReport = _commonResult.UpdateResultFlagRangeInReportModel(mappedReport, report);
            return mappedReport;
        }

        private async void Report_ReflexEvent(object sender, ReflexEventData e)
        {
            Reflex message = new Reflex()
            {
                AccessionNumber = e.AccessionNumber,
                OrderedCode = e.OrderedCode,
                ReflexCode = e.ReflexCode
            };
            var messageWrapper = new Message<Reflex>
            {
                Payload = message,
                Key = message.AccessionNumber,
                MessageId = Guid.NewGuid().ToString(),
            };
            messageWrapper.AddHeader("log_key", message.AccessionNumber);
            MessagePartitionInfo result = await _reflexProducer.ProduceAsync(messageWrapper);
        }

        public async Task ProduceIncidentRevisedResult(Bioreference.LIS.Report report)
        {
            Incident message = CreateIncidentMessage(report);
            if (message != null)
            {
                var messageWrapper = new Message<Incident>
                {
                    Payload = message,
                    Key = Guid.NewGuid().ToString(),
                    MessageId = Guid.NewGuid().ToString(),
                };
                messageWrapper.AddHeader("log_key", report.AccessionNbr);
                MessagePartitionInfo result = await _incidentProducer.ProduceAsync(messageWrapper);
            }
        }

        public Incident CreateIncidentMessage(Bioreference.LIS.Report report)
        {
            List<Contracts.Result.TestInfo> testInfos = new List<Contracts.Result.TestInfo>();

            foreach (ReportAnalyte a in report.Analytes.List)
            {
                if (a.HasBeenPreviouslyReleased() && a.IsPreviousStatusNotReleased() && a.ResultStatus == resultStatusType.Corrected)
                {
                    testInfos.Add(new Contracts.Result.TestInfo
                    {
                        TestCode = a.Code,
                        Description = a.AnalyteName,
                        Result = $"Revised from: {a.PreviousResultValue} to: {a.ResultValue}"
                    });
                }
            }
            foreach (ReportAnalytePanel p in report.AnalytePanels.List)
            {
                if (p.GetStatus() == resultStatusType.Corrected)
                {
                    foreach (ReportAnalyte a in p.Analytes.List)
                    {
                        if (a.HasBeenPreviouslyReleased() && a.IsPreviousStatusNotReleased() && a.ResultStatus == resultStatusType.Corrected)
                        {
                            testInfos.Add(new Contracts.Result.TestInfo
                            {
                                TestCode = a.Code,
                                Description = a.AnalyteName,
                                Result = $"Revised from: {a.PreviousResultValue} to: {a.ResultValue}"
                            });

                        }
                    }

                }

            }

            if (testInfos.Any())
            {
                return new Incident
                {
                    SourceID = Guid.NewGuid().ToString(),
                    Markers = new List<string>(),
                    Identifier = report.AccessionNbr,
                    Category = "RV",
                    IncidentType = "30",
                    Notes = "Revised Results",
                    AccessionNumber = report.AccessionNbr,
                    DateOfService = report.DateServiced.ToString(),
                    ClientID = report.AccountNumber,
                    Tests = testInfos
                };
            }

            return null;
        }

        public async Task<List<ReportInfoSearchCriteria>> FetchReports(string analyteCode, transmitStatusType status, bool searchPanels)
        {
            if (string.IsNullOrEmpty(analyteCode))
                throw new ArgumentNullException(nameof(analyteCode));

            List<ReportInfoSearchCriteria> response = new List<ReportInfoSearchCriteria>();
            Reports reports = await Task.Run(() => Reports.Fetch(analyteCode, status, searchPanels));
            if (reports != null && reports.List.Count > 0)
            {
                response = _mapper.Map<List<ReportInfoSearchCriteria>>(reports.List);
            }
            return response;
        }

        public async Task<CriteriaDefinitionModel> FetchCriteria(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            CriteriaDefinition criteriaDefinition = await Task.Run(() => RuleManager.Fetch().GetCriteria(className));
            var response = _mapper.Map<CriteriaDefinitionModel>(criteriaDefinition);

            return response;
        }

        public async Task<bool> ResetRefAnlayte(string testCode, List<long> reportIds)
        {
            bool isSuccess = false;
            Bioreference.Common.TestMaster.TestInfo testInfo = await Task.Run(() => Bioreference.Common.TestMaster.TestInfo.Fetch(testCode));

            foreach (var reportId in reportIds)
            {
                Bioreference.LIS.Report report = await Task.Run(() => Bioreference.LIS.Report.Fetch(reportId));
                ActivityHelper.SetAccessionLogKey(report.AccessionNbr);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    report.AccessionNbr,
                    DateTime.UtcNow
                );
                // Search individual analytes
                var ra = report.Analytes.Find(testInfo.Code);
                foreach (var a in ra)
                {
                    a.ResetAnalyte(testInfo);
                }

                // Search all panels
                foreach (ReportAnalytePanel p in report.AnalytePanels.List)
                {
                    ra = p.Analytes.Find(testInfo.Code);
                    foreach (var a in ra)
                    {
                        a.ResetAnalyte(testInfo);
                    }
                }

                //Save report
                report.Save();

                isSuccess = true;
            }

            return isSuccess;
        }

        public async Task<CorrectedReasonMappingsModel> CorrectedReasonMapping()
        {
            CorrectedReasonMappings correctedReasonMappings = await Task.Run(() => CorrectedReasonMappings.Fetch());
            var mappedResponse = _mapper.Map<CorrectedReasonMappingsModel>(correctedReasonMappings);
            return mappedResponse;
        }

        public async Task<List<DictionaryEntry>> DepartmentResponsible()
        {
            var dict = new List<DictionaryEntry>();
            DepartmentResponsibles departmentResponsibles = await Task.Run(() => DepartmentResponsibles.Fetch());
            dict.Add(new DictionaryEntry("selectAll", "Select All"));
            foreach (var item in departmentResponsibles.DeptList)
            {
                dict.Add(new DictionaryEntry(item.DeptId, item.DeptName));
            }
            return dict;
        }

        public async Task<List<DictionaryEntry>> ReportingDepartment()
        {
            List<DictionaryEntry> dict = new List<DictionaryEntry>();
            ReportingDepartments reportingDepartment = await Task.Run(() => ReportingDepartments.Fetch());
            dict.Add(new DictionaryEntry("selectAll", "Select All"));

            foreach (var item in reportingDepartment.DeptList)
            {
                dict.Add(new DictionaryEntry(item.DeptId, item.DeptName));
            }
            return dict;
        }

        public async Task<List<DictionaryEntry>> GetResponsibleLabsDropdown()
        {
            List<DictionaryEntry> responsibleLabsDic = new List<DictionaryEntry>();
            var response = await Task.Run(() => ResponsibleLabs.Fetch());
            if (response != null)
            {
                foreach (var lab in response.List)
                {
                    responsibleLabsDic.Add(new DictionaryEntry(lab.ResponsibleLabId, lab.ResponsibleLabName));
                }
            }
            return responsibleLabsDic;
        }

        public async Task<List<DictionaryEntry>> GetSignificancesDropdown()
        {
            List<DictionaryEntry> significanceDic = new List<DictionaryEntry>();
            var response = await Task.Run(() => Significances.Fetch());

            if (response != null)
            {
                foreach (var sig in response.List)
                {
                    significanceDic.Add(new DictionaryEntry(sig.Id, sig.Name));
                }
            }

            return significanceDic;
        }

        public async Task<bool> SaveCorrectedReasons(List<CorrectedReasonsModel> correctedReasonList)
        {
            if (correctedReasonList == null)
            {
                throw new ArgumentNullException(nameof(correctedReasonList), "Corrected reason cannot be null");
            }

            try
            {
                if (_mapper == null)
                {
                    throw new InvalidOperationException("Mapper is not initialized");
                }

                foreach (CorrectedReasonsModel correctedReason in correctedReasonList)
                {
                    CorrectedReasons response = _mapper.Map<CorrectedReasons>(correctedReason);
                    foreach (var deptResponsible in correctedReason.DeptResponsible)
                    {
                        FillCorrectedReasonDeptResponsibleTable(deptResponsible.DeptResponsibleId, deptResponsible.DeptResponsibleName, correctedReason.TestCode, correctedReason.ReportId, correctedReason.ReportAnalyteId);
                    }                   
                    response.DeptResponsible = _correctedReasonDeptResponsibleTable;
                    foreach (var reportingDept in correctedReason.ReportingDept)
                    {
                        FillCorrectedReasonReportingDeptTable(reportingDept.ReportingDeptId, reportingDept.ReportingDeptName, correctedReason.TestCode, correctedReason.ReportId, correctedReason.ReportAnalyteId);
                    }
                    response.ReportingDepartment = _correctedReasonReportingDeptTable;
                    if (response == null)
                    {
                        throw new InvalidOperationException("Failed to map CorrectedReasonsModel to CorrectedReasons");
                    }

                    await Task.Run(() => response.Save());
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private void FillCorrectedReasonReportingDeptTable(int reportingDeptResponsible, string reportingDeptName, string testCode, int reportId, long reportAnalyteId)
        {
            // Initialize columns if table is empty
            if (_correctedReasonReportingDeptTable.Columns.Count == 0)
            {
                _correctedReasonReportingDeptTable.Columns.Add("ReportingDeptResponsible", typeof(int));
                _correctedReasonReportingDeptTable.Columns.Add("ReportingDeptName", typeof(string));
                _correctedReasonReportingDeptTable.Columns.Add("TestCode", typeof(string));
                _correctedReasonReportingDeptTable.Columns.Add("ReportId", typeof(int));
                _correctedReasonReportingDeptTable.Columns.Add("ReportAnalyteId", typeof(long));
            }

            DataRow row = _correctedReasonReportingDeptTable.NewRow();
            row["ReportingDeptResponsible"] = reportingDeptResponsible;
            row["ReportingDeptName"] = reportingDeptName;
            row["TestCode"] = testCode;
            row["ReportId"] = reportId;
            row["ReportAnalyteId"] = reportAnalyteId;

            _correctedReasonReportingDeptTable.Rows.Add(row);
        }
        private void FillCorrectedReasonDeptResponsibleTable(int deptResponsibleId, string deptResponsibleName, string testCode, int reportId, long reportAnalyteId)
        {          

            // Make sure the columns exist before adding a row
            if (_correctedReasonDeptResponsibleTable.Columns.Count == 0)
            {
                _correctedReasonDeptResponsibleTable.Columns.Add("DeptResponsibleId", typeof(int));
                _correctedReasonDeptResponsibleTable.Columns.Add("DeptResponsibleName", typeof(string));
                _correctedReasonDeptResponsibleTable.Columns.Add("TestCode", typeof(string));
                _correctedReasonDeptResponsibleTable.Columns.Add("ReportId", typeof(int));
                _correctedReasonDeptResponsibleTable.Columns.Add("ReportAnalyteId", typeof(long));
            }

            DataRow row = _correctedReasonDeptResponsibleTable.NewRow();
            row["DeptResponsibleId"] = deptResponsibleId;
            row["DeptResponsibleName"] = deptResponsibleName;
            row["TestCode"] = testCode;
            row["ReportId"] = reportId;
            row["ReportAnalyteId"] = reportAnalyteId;

            _correctedReasonDeptResponsibleTable.Rows.Add(row);
        }

        public async Task<List<ClinicalTrialsResponseModel>> GetClinicalTrailsData(ClinicalTrialsRequestModel clinicalTrialsRequestModel)
        {
            List<ClinicalTrialsResponseModel> clinicalTrialsResponseModels = new List<ClinicalTrialsResponseModel>();
            List<ClinicalTrialsResultInfo> clinicalTrialsResultInfos = new List<ClinicalTrialsResultInfo>();
            var response = OrderManager.FetchClinicalTrailsReportIDs(clinicalTrialsRequestModel.StartDate, clinicalTrialsRequestModel.EndDate, string.Join(",", clinicalTrialsRequestModel.ClientID), string.Join(",", clinicalTrialsRequestModel.TestCode)).Distinct().ToList();
           
            foreach (var id in response)
            {
                ClinicalTrialsResponseModel clinicalTrialsResponseModel = new ClinicalTrialsResponseModel();
                _logger.LogInformation("Clinical Trials Report Fetch Started" + DateTime.UtcNow.ToString());
                var report = await Task.Run(() => Bioreference.LIS.Report.Fetch(Convert.ToInt64(id), useCache: false, cacheKey: ""));
                _logger.LogInformation("Clinical Trials Report Fetch Ended" + DateTime.UtcNow.ToString());

                _logger.LogInformation("Clinical Trials Order Fetch Started" + DateTime.UtcNow.ToString());
                LIS.Order order = await Task.Run(() => OrderManager.FetchOrder(report.OrderId));
                _logger.LogInformation("Clinical Trials Order Fetch Ended" + DateTime.UtcNow.ToString());

                ClinicalTrialOrderInfo clinicalTrialOrderInfo = new ClinicalTrialOrderInfo();
                clinicalTrialOrderInfo.AccessionNumber = order.AccessionNbr;
                clinicalTrialOrderInfo.DateOfService = order.DateOfService;
                clinicalTrialOrderInfo.DateOfCollection = order.DateOfCollection;
                clinicalTrialOrderInfo.VisitNumber = order.VisitNumber;
                clinicalTrialOrderInfo.StudyNumber = order.StudyNumber;
                clinicalTrialOrderInfo.AccountNo = order.AccessionNbr;

                clinicalTrialsResponseModel.ClinicalTrialOrderInfo = clinicalTrialOrderInfo;

                ClinicalTrialsPatientInfo clinicalTrialsPatientInfo = new ClinicalTrialsPatientInfo();
                clinicalTrialsPatientInfo.FirstName = order.Patient.FirstName;
                clinicalTrialsPatientInfo.LastName = order.Patient.LastName;
                clinicalTrialsPatientInfo.PatientID = order.Patient.ID;
                clinicalTrialsPatientInfo.Age = _orderService.GetPatientAge(order);
                clinicalTrialsPatientInfo.DOB = order.Patient.DateOfBirth;
                clinicalTrialsPatientInfo.Gender = (Model.Enum.GenderModel)order.Patient.Gender;
                clinicalTrialsPatientInfo.EUID = order.EUID;
               
                clinicalTrialsResponseModel.ClinicalTrialPatientInfo = clinicalTrialsPatientInfo;

                foreach (ReportAnalyte test in report.Analytes.List)
                {
                    if (clinicalTrialsRequestModel.TestCode.Contains(test.OrderingCodes))
                    {
                        clinicalTrialsResultInfos.Add(AssembleClinicalResultsInfo(test, clinicalTrialsRequestModel));
                    }
                }

                foreach (ReportAnalytePanel panel in report.AnalytePanels.List)
                {
                    foreach (ReportAnalyte test in panel.Analytes.List)
                    {
                        if (clinicalTrialsRequestModel.TestCode.Contains(test.OrderingCodes))
                        {
                            clinicalTrialsResultInfos.Add(AssembleClinicalResultsInfo(test, clinicalTrialsRequestModel));
                        }
                    }
                }


                if (clinicalTrialsResultInfos.Count > 0)
                {
                    clinicalTrialsResponseModel.ClinicalTrialsResultInfos = clinicalTrialsResultInfos;
                }

                clinicalTrialsResponseModels.Add(clinicalTrialsResponseModel);

            }

            return clinicalTrialsResponseModels;
        }

        public async Task<List<PriorResultModel>> GetPriorResults(PriorResultRequestModel priorResultRequest)
        {
            List<PriorResultModel> priorResultResponse = new List<PriorResultModel>();
            var priorResults = await Task.Run(() => Bioreference.LIS.PriorResults.Fetch(priorResultRequest.ReportId, priorResultRequest.AnalyteCode, 
                               priorResultRequest.IncludeTNPQNS, priorResultRequest.IncludeAllStatus));

            if (priorResults != null && priorResults.List.Length>0)
            {
                priorResultResponse = _mapper.Map<List<PriorResultModel>>(priorResults.List);
            }
            return priorResultResponse;
        }
        private List<string> AssembleComments(ReportCommentList commentList)
        {
            List<string> comments = new List<string>();
            foreach(ReportComment cmt in commentList)
            {
                comments.Add(cmt.Text);
            }
            return comments;
        }
      

        private void GetRanges(ClinicalTrialsResultInfo clinicalTrialsResultInfo,ComplexResultFlag complexResultFlag)
        {
            var ranges = complexResultFlag?.Ranges?.List;

            if (ranges is { Count: > 0 })
            {
                clinicalTrialsResultInfo.RangeLow = $"{ranges[0].RangeFrom}-{ranges[0].RangeTo}";

                if (ranges.Count > 1)
                {
                    clinicalTrialsResultInfo.RangeHigh = $"{ranges[1].RangeFrom}-{ranges[1].RangeTo}";
                }
            }
        }

        private ClinicalTrialsResultInfo AssembleClinicalResultsInfo(ReportAnalyte test, ClinicalTrialsRequestModel clinicalTrialsRequestModel)
        {
            ClinicalTrialsResultInfo clinicalTrialsResultInfo = new ClinicalTrialsResultInfo();

            clinicalTrialsResultInfo.TestCode = test.Code;
            clinicalTrialsResultInfo.Department = test.RefAnalyte.DepartmentShortname;
            clinicalTrialsResultInfo.TestName = test.RefAnalyte.Name;
            clinicalTrialsResultInfo.ResultUnit = test.RefAnalyte.Units;
            clinicalTrialsResultInfo.Result = test.ResultValue;
            clinicalTrialsResultInfo.Comments = AssembleComments(test.Comments.List);
            clinicalTrialsResultInfo.Flag = test.RefAnalyte.FlagValue;
            clinicalTrialsResultInfo.Category = test.RefAnalyte.Category;
            clinicalTrialsResultInfo.OrderingTestCode = test.OrderingCodes;
            clinicalTrialsResultInfo.Loincs = GetLOINCs(Bioreference.Common.TestMaster.OrderableTest.Fetch(test.Code));

            //TODO : CLAPP - 2785: Change hardcoded testcode for fasting
            clinicalTrialsResultInfo.Fast = clinicalTrialsRequestModel.TestCode.Contains("C509") ? true : false;
            GetRanges(clinicalTrialsResultInfo, test.GetMatchingResultFlag());

            return clinicalTrialsResultInfo;
        }

        private List<string> GetLOINCs(Bioreference.Common.TestMaster.OrderableTest orderableTest)
        {
            List<string> loincs = new List<string>();
            foreach (TestTranslation tran in orderableTest.Test_Translations)
            {
                if (tran.CodeType.ToUpper() == "LOINC")
                {
                    loincs.Add(tran.AltCode);
                }
            }

            return loincs;
        }

        public async Task<bool> SendToCHM(SendToCHMModel sendToCHM)
        {
            if (sendToCHM == null)
            {               
                return false;
            }

            if (sendToCHM.ReportId == 0)
            {                
                return false;
            }

            Bioreference.LIS.Report? report = await Task.Run(() => Bioreference.LIS.Report.Fetch(sendToCHM.ReportId));
            ActivityHelper.SetAccessionLogKey(report.AccessionNbr);
            _logger.LogInformation(
                "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                report.AccessionNbr,
                DateTime.UtcNow
            );
            if (report == null)
            {               
                return false;
            }
            foreach (var CHMDetail in sendToCHM.Details)
            {
                switch (CHMDetail.ReportSourceType)
                {
                    case ReportSourceType.Analyte:
                        if (CHMDetail.ReportAnalyteId == 0)
                        {
                            continue;
                        }
                        ReportAnalyte? analyte = _eventProcessorService.GetReportAnalyte(CHMDetail.ReportAnalyteId, true, report);
                        if (analyte == null)
                        {
                            continue;
                        }
                        CheckAnalyteForSendingToCHM(analyte, report.AccessionNbr);
                        break;

                    case ReportSourceType.Panel:
                        if (CHMDetail.ReportPanelId == 0)
                        {
                            continue;
                        }
                        ReportAnalytePanel? panel = _eventProcessorService.GetAnalytePanel(CHMDetail.ReportPanelId, report);
                        if (panel == null)
                        {
                            continue;
                        }
                        CheckPanelForSendingToCHM(panel, report.AccessionNbr);
                        break;
                    default:
                        Console.WriteLine($"Unknown release source type: {CHMDetail.ReportSourceType}");
                        return false;
                }
            }
            return true;
        }

        private void CheckAnalyteForSendingToCHM(ReportAnalyte analyte, string accessionNbr)
        {
            if (!string.IsNullOrEmpty(analyte.ResultValue) &&
                (analyte.ResultValue == "TNP" || analyte.ResultValue == "QNS") &&
                analyte.ResultStatus == resultStatusType.Final)
            {
                OutboundStatusMessage_Save(accessionNbr, analyte.Code, analyte.PerformingFacility, "TNP");
            }

            if (!string.IsNullOrEmpty(analyte.ResultValue) &&
                analyte.ResultValue != "TNP" &&
                analyte.ResultValue != "QNS" &&
                analyte.ResultStatus == resultStatusType.Final)
            {
                OutboundStatusMessage_Save(accessionNbr, analyte.Code, analyte.PerformingFacility, "COMPLETED");
            }
        }

        private void CheckPanelForSendingToCHM(ReportAnalytePanel panel, string accessionNbr)
        {
            string panelResultStatus = GetPanelResultStatus(panel);

            if (panelResultStatus == "TNP" && panel.GetStatus() == resultStatusType.Final)
            {
                OutboundStatusMessage_Save(accessionNbr, panel.PanelCode, GetPanelPerformingfacility(ref panel), "TNP");
            }

            if (panelResultStatus == "COMPLETED" && panel.GetStatus() == resultStatusType.Final)
            {
                OutboundStatusMessage_Save(accessionNbr, panel.PanelCode, GetPanelPerformingfacility(ref panel), "COMPLETED");
            }
        }
        private string GetPanelResultStatus(ReportAnalytePanel panel)
        {
            string returnval = string.Empty;
            foreach (ReportAnalyte a in panel.Analytes.List)
            {
                if (a.Analyte.IsRequired)
                {
                    if (a.ResultValue == "TNP")
                    {
                        returnval = "TNP";
                    }
                    if (!string.IsNullOrEmpty(a.ResultValue) && a.ResultValue != "TNP" && a.ResultValue != "QNS")
                    {
                        returnval = "COMPLETED";
                    }
                }
            }
            return returnval;
        }

        public void OutboundStatusMessage_Save(string accessionNum, string testCode, string performingFacility, string status)
        {
            OutboundMessageCHM obCHM = new OutboundMessageCHM(accessionNum);
            obCHM.Message = GenerateMessageForCHM(accessionNum, testCode, performingFacility, status);
            obCHM.CreateFile = true;
            bool a = obCHM.IsDirty;
            obCHM.Save();
        }

        private string GenerateMessageForCHM(string accessionNum, string testCode, string performingFacility, string status)
        {
            string msg = $"{accessionNum}|{testCode}|{DateTime.Now.ToString("M/d/yyyy HH:mm:ss")}|{status}|{performingFacility}";
            return msg;
        }

        private string GetPanelPerformingfacility(ref ReportAnalytePanel panel)
        {
            string returnval = string.Empty;

            if (panel.Panel.ReferenceLabId != 0)
            {
                returnval = FindReflabCode(panel.Panel.ReferenceLabId);
            }
            else
            {
                returnval = GetFirstPerformingFacility(panel);
            }

            return returnval;
        }

        private string FindReflabCode(int refLabId)
        {
            Bioreference.Common.TestMaster.RefLabs refLabs = null;
            if (refLabs == null)
            {
                refLabs = RefLabs.Fetch();
            }

            foreach (RefLab rLab in refLabs.List)
            {
                if (refLabId == rLab.RefLabNo)
                {
                    return rLab.RefLabNo.ToString();
                }
            }

            return string.Empty;
        }

        private string GetFirstPerformingFacility(Bioreference.LIS.ReportAnalytePanel ap)
        {
            string rValue = string.Empty;

            foreach (ReportAnalyte obj in ap.Analytes.List)
            {
                if (!string.IsNullOrEmpty(obj.PerformingFacility))
                {
                    rValue = obj.PerformingFacility;
                    break;
                }
            }

            return rValue;
        }
        
    }
}
