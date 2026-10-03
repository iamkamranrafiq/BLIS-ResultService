using Bioreference.Data.Audit;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Common;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.AppSettings.InboundReporting;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;

namespace Bioreference.ResultService.Application.Processor
{
    public class InboundReportingProcessor : IInboundReportingProcessor
    {
        private ILogger<InboundReportingProcessor> _logger;
        private readonly ISettingService _settingsProvider;
        private InboundReportingSettings _inboundSettings = null;
        private readonly AppSettingsInboundReporting _appSettings;
        private readonly ICommonResultService _commonResult;
        public InboundReportingProcessor(ILogger<InboundReportingProcessor> logger, ISettingService settingsProvider, IOptions<AppSettingsInboundReporting> options, ICommonResultService commonResult)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _appSettings = options.Value;
            _commonResult = commonResult;
        }
        public async Task InitializeAsync()
        {
            string connection = _settingsProvider.GetConnectionString("Bioreference.LIS");
            _inboundSettings = await _settingsProvider.FetchSetting<InboundReportingSettings>(
              connection, "InboundReportingEngine");

        }
        public async Task ProcessORUMessage(ORUMessage message)
        {
            await InitializeAsync();
            var sw = Stopwatch.StartNew();
            string sMessageId = string.Empty;
            string sAccessionNum = string.Empty;
            DateTime dDateServiced = new DateTime(1900, 1, 1);
            string sTestCode = string.Empty;
            string sCode = string.Empty;
            string sValue = string.Empty;
            string performingFacility = string.Empty;
            string status = string.Empty;
            string receiveApp = string.Empty;
            Bioreference.LIS.Report report = null;
            string[] formats ={"yyyyMMdd", "yyyyMMddHHmmss"};
            try
            {
                sMessageId = message.MSH.MessageID;
                receiveApp = message.MSH.ReceivngApplication;

                sAccessionNum = message.LabAccession[0].AccessionNumber;

                ActivityHelper.SetAccessionLogKey(sAccessionNum);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    sAccessionNum,
                    DateTime.UtcNow
                );

                if (string.IsNullOrEmpty(sAccessionNum)) sAccessionNum = message.LabAccession[0].AltField1;

                if (!DateTime.TryParseExact(message.LabAccession[0].OrderDate.ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dDateServiced)) dDateServiced = new DateTime(1900, 1, 1);

                sAccessionNum = ValidateAccession(sAccessionNum);

                if (!string.IsNullOrEmpty(sAccessionNum))
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; MessageId: {MessageId}; ReceiveApp: {ReceiveApp}", "ResultReporting", "ProcessORUMessage", "Processing accession.", sAccessionNum, sMessageId, receiveApp);
                    if (dDateServiced != new DateTime(1900, 1, 1) && _inboundSettings.SearchResultAccessionWithDOS) report = Bioreference.LIS.Report.Fetch(sAccessionNum, dDateServiced); else report = Bioreference.LIS.Report.Fetch(sAccessionNum);
                }

                if (report != null)
                {
                    short iReportCount = (short)message.LabReport.Count;

                    for (short iThisReport = 0; iThisReport < iReportCount; iThisReport++)
                    {
                        var labReport = message.LabReport[iThisReport];
                        sTestCode = labReport.Obr.TestCode;

                        if (labReport.Result.Count > 0)
                        {
                            short iResultCount = (short)labReport.Result.Count;

                            for (short iThisResult = 0; iThisResult < iResultCount; iThisResult++)
                            {
                                var result = labReport.Result[iThisResult].Obx[0];
                                sCode = result.Code;
                                sValue = result.Value;
                                status = result.Status;
                                performingFacility = result.PerformLocation;

                                if (receiveApp.Contains("B2-4K"))
                                {
                                    performingFacility = status;
                                    Process4kMessage(sAccessionNum, sValue, sCode, performingFacility, report);
                                }
                                else if (receiveApp.Contains("B2-QNS"))
                                {
                                    ProcessHospitalQNSMessage(sAccessionNum, sValue, sCode, report);
                                }
                                else
                                {
                                    await ProcessMicropathMessage(sAccessionNum, sValue, sCode, status, performingFacility, report, dDateServiced.ToString());
                                }
                            }
                        }
                    }

                    report.Save();
                    sw.Stop();
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedTime} ms", "ResultReporting", "ProcessORUMessage", "Accession processed successfully.", sAccessionNum, sw.ElapsedMilliseconds);
                }
                else
                {
                    sw.Stop();
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedTime} ms", "ResultReporting", "ProcessORUMessage", "No report found for accession.", sAccessionNum, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}; ElapsedTime: {ElapsedTime} ms", "ResultReporting", "ProcessORUMessage", "Error while processing ORU message.", sAccessionNum, ex.Message, sw.ElapsedMilliseconds);
            }
        }

        private async Task ProcessMicropathMessage(string sAccessionNum, string sValue, string sCode, string status, string performingFacility, LIS.Report report, string dateServiced)
        {
            if (sValue.ToUpper() == "COMPLETED")
            {
                ProcessMicropathStatus(sAccessionNum, sValue, sCode, GetStatusMicropath(status).ToString(), performingFacility, report, dateServiced);
            }
            else if (sValue.ToUpper() == "TNP")
            {
                ProcessMicropathStatus(sAccessionNum, "CANCEL", sCode, GetStatusMicropath(status).ToString(), performingFacility, report, dateServiced);
            }
            else if (sValue.ToUpper().Contains("ADD_COMP"))
            {
                //MessageToAuditChannel(GenerateMessageForAudit(sAccessionNum, sValue, sCode, report));
                await MessageToAuditKafka(GenerateMessageForAudit(sAccessionNum, sValue, sCode, report));
                OutboundStatusMessage_Save(sAccessionNum, sCode, status, sValue, dateServiced);
            }
        }
        private async Task MessageToAuditKafka(string sMessage)
        {
            await _commonResult.ProduceAuditOutMessage(sMessage);
            _logger.LogDebug("Writing to Kafka Done.");
        }
        private void MessageToAuditChannel(string sMessage)
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string fileName = string.Concat(_appSettings.AuditChannelPath, _appSettings.AuditFilePrefix, timeStamp, ".txt");

            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; FilePath: {FilePath}", "Audit", "Write", "Writing audit file.", fileName);

            using (var sw = new System.IO.StreamWriter(fileName, true))
            {
                sw.WriteLine(sMessage);
            }

            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; FilePath: {FilePath}", "Audit", "Write", "Writing audit file completed.", fileName);
        }


        private void ProcessMicropathStatus(string sAccessionNum, string resultValue, string testCode, string status, string performingFacility, LIS.Report report, string dateServiced)
        {
            LIS.ReportAnalyte analyte = report.FindAnalyte(testCode, true);
            if (analyte != null)
            {
                if (analyte.PerformingFacility != performingFacility) analyte.OverridePerformingFacility(performingFacility);
                analyte.SetResultValue(resultValue, false);
                analyte.SetResultStatus(status);
                analyte.MarkAsReleased();
                if (analyte.ParentPanel != null) analyte.ParentPanel.MarkAsReleased();
                OutboundStatusMessage_Save(sAccessionNum, testCode, performingFacility, resultValue, dateServiced);
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}; PerformingFacility: {PerformingFacility}; Status: {Status}", "ResultReporting", "ProcessMicropathStatus", "Micropath status processed and saved to outbound status queue.", sAccessionNum, testCode, performingFacility, status);
            }
            else
            {
                _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "ResultReporting", "ProcessMicropathStatus", "Analyte not found for test code.", sAccessionNum, testCode);
            }
        }
        private void OutboundStatusMessage_Save(string accessionNum, string testCode, string performingFacility, string status, string dateServiced)
        {
            var sw = Stopwatch.StartNew();
            var obCHM = new OutboundMessageCHM(accessionNum);
            obCHM.Message = GenerateMessageForCHM(accessionNum, testCode, performingFacility, status, dateServiced);
            obCHM.Save();
            sw.Stop();
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}; PerformingFacility: {PerformingFacility}; Status: {Status}; DateServiced: {DateServiced}; ElapsedTime: {ElapsedTime} ms", "ResultReporting", "OutboundStatusMessage_Save", "Outbound status message saved to OutboundMessageCHM.", accessionNum, testCode, performingFacility, status, dateServiced, sw.ElapsedMilliseconds);
        }

        private string GenerateMessageForCHM(string accessionNum, string testCode, string performingFacility, string status, string dateServiced)
        {
            string msg = accessionNum + "|" + testCode + "|" + dateServiced + "|" + status + "|" + performingFacility;
            return msg;
        }

        private string GenerateMessageForAudit(string accessionNum, string status, string testCode, LIS.Report report)
        {
            string msg = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff") + "|0|" + report.ID + "|Bioreference.LIS.Report" + "|reportingchanneluser|Recieved Test Code : " + testCode + " with " + status + " Status|" + report.AccessionIdentifier + "|" + report.AccessionIdentifierType + "|0|0|";
            return msg;
        }


        private resultStatusType GetStatusMicropath(string status)
        {
            resultStatusType resultstatus = default;
            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToUpper() == "F")
                {
                    resultstatus = resultStatusType.Final;
                }
                else if (status.ToUpper() == "C")
                {
                    resultstatus = resultStatusType.Corrected;
                }
            }
            return resultstatus;
        }

        private void ProcessHospitalQNSMessage(string sAccessionNum, string value, string testCode, LIS.Report report)
        {
            LIS.ReportAnalyte analyte = report.FindAnalyte(testCode, true);

            if (analyte == null)
            {
                ReportAnalytePanel panel = report.FindAnalytePanel(testCode);
                if (panel != null)
                {
                    ProcessHospitalQNSpanel(value, panel);
                }
            }
            else
            {
                ProcessHospitalQNSAnalyte(value, analyte);
                if (analyte.IsResultStatusChanged())
                {
                    string auditMsg = $"Reporting Status update from {analyte.OrgResultStatus} to {analyte.ResultStatus} status.";
                    AuditManager.LogCustomObjectAction(analyte, auditMsg);
                }
            }
        }
        private void ProcessHospitalQNSAnalyte(string value, LIS.ReportAnalyte analyte)
        {
            if (analyte != null)
            {
                switch (value.ToUpper())
                {
                    case "ENROUTE":
                        SetResultValueToQNS(analyte);
                        analyte.SampleStatus = (int)SampleRequestType.SampleEnRoute;
                        analyte.AddComment("Additional sample has been requested from the client and will be reported on another accession once received.");
                        analyte.MarkAsReleased();
                        break;
                    case "NOSAMPLE":
                        SetResultValueToQNS(analyte);
                        analyte.SampleStatus = (int)SampleRequestType.NoSample;
                        analyte.AddComment("No additional sample was available.");
                        analyte.MarkAsReleased();
                        break;
                    case "CLOSED":
                        SetResultValueToQNS(analyte);
                        analyte.SampleStatus = (int)SampleRequestType.Closed;
                        analyte.AddComment("Unable to obtain additional sample.");
                        analyte.MarkAsReleased();
                        break;
                }
            }
        }
        private void ProcessHospitalQNSpanel(string value, LIS.ReportAnalytePanel panel)
        {
            foreach (LIS.ReportAnalyte analyte in panel.Analytes.List)
            {
                if (!string.IsNullOrEmpty(analyte.ResultValue) || analyte.Analyte.IsRequired)
                {
                    ProcessHospitalQNSpanelAnalyte(value, analyte);
                }
            }

            switch (value.ToUpper())
            {
                case "NOSAMPLE":
                    panel.AddComment("No additional sample was available.");
                    HospitalQNSpanelMarkAsReleased(panel);
                    break;
                case "CLOSED":
                    panel.AddComment("Unable to obtain additional sample.");
                    HospitalQNSpanelMarkAsReleased(panel);
                    break;
                case "ENROUTE":
                    panel.AddComment("Additional sample has been requested from the client and will be reported on another accession once received.");
                    HospitalQNSpanelMarkAsReleased(panel);
                    break;
            }

            foreach (LIS.ReportAnalyte analyte in panel.Analytes.List)
            {
                if (!string.IsNullOrEmpty(analyte.ResultValue) || analyte.Analyte.IsRequired)
                {
                    if (analyte.IsResultStatusChanged())
                    {
                        string auditMsg = $"Reporting Status update from {analyte.OrgResultStatus} to {analyte.ResultStatus} status.";
                        AuditManager.LogCustomObjectAction(analyte, auditMsg);
                    }
                }
            }
        }
        private void HospitalQNSpanelMarkAsReleased(LIS.ReportAnalytePanel panel)
        {
            if (!panel.PanelIsPartiallyResulted())
            {
                panel.MarkAsReleased();
            }
        }
        private void ProcessHospitalQNSpanelAnalyte(string value, LIS.ReportAnalyte analyte)
        {
            if (analyte != null)
            {
                switch (value.ToUpper())
                {
                    case "ENROUTE":
                        SetResultValueToQNS(analyte);
                        analyte.SampleStatus = (int)SampleRequestType.SampleEnRoute;
                        break;
                    case "NOSAMPLE":
                        SetResultValueToQNS(analyte);
                        analyte.SampleStatus = (int)SampleRequestType.NoSample;
                        break;
                    case "CLOSED":
                        SetResultValueToQNS(analyte);
                        analyte.SampleStatus = (int)SampleRequestType.Closed;
                        break;
                }
            }
        }
        private void SetResultValueToQNS(LIS.ReportAnalyte analyte)
        {
            if (analyte.ResultValue != "QNS")
            {
                analyte.SetResultValue("QNS", false);
            }
        }


        private void Process4kMessage(string sAccessionNum, string status, string testCode, string performingFacility, LIS.Report report)
        {
            if (!string.IsNullOrEmpty(testCode))
            {
                ProcessPanel(status, testCode, report);
                ProcessAnalyteInReport(status, testCode, report);
            }
        }
        private void ProcessAnalyteInReport(string status, string testCode, LIS.Report report)
        {
            foreach (LIS.ReportAnalyte analyte in report.Analytes.List)
            {
                if (analyte.OrderingAnalyteCodes.Contains(testCode))
                {
                    SetStatus(analyte, ref status, testCode);
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; TestCode: {TestCode}; AnalyteCode: {AnalyteCode}; Status: {Status}", "ResultReporting", "ProcessAnalyteInReport", "Analyte matched and status set in report.", testCode, analyte.Code, status);
                }
            }
        }

        private void ProcessPanel(string status, string testCode, LIS.Report report)
        {
            foreach (LIS.ReportAnalytePanel panel in report.AnalytePanels.List)
            {
                if (panel.OrderingPanelCodes.Contains(testCode))
                {
                    ProcessAnalyteInPanel(status, panel, GetLastAnalyteCode(panel));
                    ProcessPanelForTNP(status, panel);
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; TestCode: {TestCode}; PanelCode: {PanelCode}; Status: {Status}", "ResultReporting", "ProcessPanel", "Panel matched and processed.", testCode, panel.PanelCode, status);
                }
            }
        }

        private void ProcessPanelForTNP(string status, ReportAnalytePanel panel)
        {
            if (status.ToUpper() == "TNPREQUEST")
            {
                panel.ProcessTNPFromReporting4k();
            }
        }
        private void ProcessAnalyteInPanel(string status, LIS.ReportAnalytePanel panel, string testCode)
        {
            foreach (LIS.ReportAnalyte analyte in panel.Analytes.List)
            {
                SetStatus(analyte, ref status, testCode);
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; PanelCode: {PanelCode}; AnalyteCode: {AnalyteCode}; Status: {Status}", "ResultReporting", "ProcessAnalyteInPanel", "Analyte processed in panel.", panel.PanelCode, analyte.Code, status);
            }
        }

        private void SetStatus(LIS.ReportAnalyte reportAnalyte, ref string status, string testCode = "")
        {
            if (reportAnalyte != null)
            {
                switch (status.ToUpper())
                {
                    case "HOLD":
                        reportAnalyte.SetReportingHold();
                        break;
                    case "RELEASEHOLD":
                        reportAnalyte.LiftReportingHold();
                        break;
                    case "TNPREQUEST":
                        reportAnalyte.ProcessTNP4k(testCode);
                        break;
                    case "TNP":
                        reportAnalyte.ProcessTNPFromReporting(testCode);
                        break;
                }
            }
        }
        private string GetLastAnalyteCode(ReportAnalytePanel panel)
        {
            LIS.ReportAnalyte reqAnalyte = null;
            foreach (LIS.ReportAnalyte analyte in panel.Analytes.List)
            {
                if (((RefAnalyte)analyte.Analyte).IsRequired)
                {
                    reqAnalyte = analyte;
                }
            }
            if (reqAnalyte != null)
            {
                return reqAnalyte.Code;
            }
            return string.Empty;
        }


        private string ValidateAccession(string sAccessionNum)
        {
            if (!string.IsNullOrEmpty(sAccessionNum))
            {
                if (sAccessionNum.Length == 9 && sAccessionNum.Substring(0, 2) == "10")
                {
                    return sAccessionNum.Remove(0, 2);
                }
            }
            return sAccessionNum;
        }

    }
}
