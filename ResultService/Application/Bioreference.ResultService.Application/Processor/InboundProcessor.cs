using Bioreference.LIS;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Abstractions.Application.Processor;
using System.Globalization;
using Microsoft.Extensions.Options;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.DI.Interface;
using Bioreference.Common;
using Bioreference.Common.TestMaster;
using Bioreference.ResultService.Abstractions.Application.Common;
using System.Diagnostics;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Processor
{

    public class InboundProcessor : IInboundProcessor
    {
        private ILogger<InboundProcessor> _logger;
        private const string IDENTIFER_PANELCMT = "PANEL_CMT";
        public const int NOTEFIXEDWIDTH = 78;
        private readonly ISettingService _settingsProvider;
        private readonly AppSettingsInboundEngine _appSettings;
        private InboundEngineSettings _inboundSettings = null;
        private NoBillCodes _noBillCodes = null;
        private Bioreference.Common.TestMaster.RefLabs _refLabs = null;
        private readonly ICommonResultService _commonResult;
        private string HL7String = string.Empty;


        public InboundProcessor(ILogger<InboundProcessor> logger, ISettingService settingsProvider, IOptions<AppSettingsInboundEngine> options, ICommonResultService commonResult)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _appSettings = options.Value;
            _commonResult = commonResult;

        }
        public async Task InitializeAsync()
        {
            string connection = _settingsProvider.GetConnectionString("Bioreference.LIS");
            _inboundSettings = await _settingsProvider.FetchSetting<InboundEngineSettings>(
                connection, "InboundEngine");
        }
        public async Task OnORUMessage(ORUMessage message, string HL7String)
        {                    
                await InitializeAsync();
                this.HL7String = HL7String;
                var referenceLabId = Convert.ToInt32(_appSettings.ReferenceLabId);
                _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; ReferenceLabId: {ReferenceLabId}", "ORU", "Fetch", "ReferenceLabId detected.", referenceLabId);

                if (referenceLabId == 0)
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "ORU", "ParseInstrumentResult", "Routing ORU message to instrument result parser.");

                    await ParseInstrumentResult(message);
                }
                else
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "ORU", "ParseRefLabResult", "Routing ORU message to reference lab result parser.");

                    await ParseRefLabResult(message);
                }        

       }

        public async Task OnORMMessage(ORMMessage message)
        {
            await InitializeAsync();
            _logger.LogDebug("In OnORMmessage");

            if (message.Order[0].ClientAccountNumber == "")
            {
                throw new Exception("Invalid Client Account: Client Account Number is blank. This message will not be processed. (" + message.Order[0].AccessionNumber + ")");
            }

            if (ParseDateTime(message.Order[0].OrderDate) < DateTime.Now.AddYears(-1))
            {
                throw new Exception(string.Format("Error creating order for Accession #{0}: {1} DOS is over a year old.", message.Order[0].AccessionNumber, FormatDateTime(message.Order[0].OrderDate, "MM/dd/yyyy HH:mm:ss")));
            }

            string sMessageId = string.Empty;
            string sLastName = string.Empty;
            string sFirstName = string.Empty;
            string sDOB = string.Empty;
            string sSex = string.Empty;
            string sAccessionNum = string.Empty;
            string sDocName = string.Empty;
            string sOrderDate = string.Empty;
            string sCollectDate = string.Empty;

            List<DI.Interface.OrderTest> cOrders = new();
            string sClientOrderNum = string.Empty;
            string sRefLabNum = string.Empty;
            string sClientAcctNum = string.Empty;
            string sPriority = string.Empty;
            string sOCText = string.Empty;
            string sTestCode = string.Empty;
            string sOrdRowText = string.Empty;

            try
            {
                try
                {
                    sMessageId = message.MSH.MessageID;
                    sLastName = message.PatientInfo.LastName;
                    sFirstName = message.PatientInfo.FirstName;
                    sDOB = FormatDateTime(message.PatientInfo.DOB, "MM/dd/yyyy HH:mm:ss");
                    if (sDOB == "12/31/1840") sDOB = string.Empty;
                    sSex = SexToGender(message.PatientInfo.Sex).ToString();

                    sClientOrderNum = message.Order[0].ClientOrderNumber;
                    sAccessionNum = message.Order[0].AccessionNumber;
                    sRefLabNum = message.Order[0].ReferenceLabNumber;
                    sClientAcctNum = message.Order[0].ClientAccountNumber;
                    sDocName = message.Order[0].OrderingDoctorName;
                    sPriority = message.Order[0].Priority;
                    sOrderDate = FormatDateTime(message.Order[0].OrderDate, "MM/dd/yyyy");
                    sCollectDate = FormatDateTime(message.Order[0].CollectDate, "MM/dd/yyyy");

                    if (message.OrderComment.Count > 0)
                    {
                        sOCText = DoComments(message.OrderComment, note => note.NoteText);
                    }

                    int iTestCount = message.Order.Count;
                    for (short iThisTest = 0; iThisTest < iTestCount; iThisTest++)
                    {
                        sTestCode = message.Order[iThisTest].TestCode;
                        if (sTestCode == "0000") continue;

                        if (message.Order[iThisTest].Comment.Count > 0)
                        {
                            sOrdRowText = DoComments(message.Order[iThisTest].Comment, note => note.NoteText);
                        }

                        DI.Interface.OrderTest o = new(sTestCode, sOrdRowText);
                        cOrders.Add(o);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed setting local variables.\n{ex}");
                }

                var st = OrderManager.CreateOrder(sClientAcctNum, sAccessionNum, sOrderDate, 0, true);
                if (st.StatusType == OrderManager.StatusType.Failure)
                {
                    throw new Exception(string.Format("Error creating order for Accession #{0}: {1}", sAccessionNum, st.Message));
                }
                else
                {
                    Bioreference.LIS.Order oda = (Bioreference.LIS.Order)st.ReturnValue;
                    oda.RelaxValidation();

                    try
                    {
                        if (oda.AccountNumber != sClientAcctNum)
                        {
                            oda = oda.SetAccountNumber(sClientAcctNum);
                        }
                    }
                    catch (Exception ex)
                    {
                        //ErrorMessages.Add($"OnORMMessage - Unable to reset account to {sClientAcctNum} for AccessionNbr #{sAccessionNum}.\n{ex}");
                        _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ClientAccount: {ClientAccount}; Accession: {Accession}", "Result", "OnORMMessage", "Unable to reset account.", sClientAcctNum, sAccessionNum);
                    }

                    try
                    {
                        oda.Patient.LastName = sLastName;
                        oda.Patient.FirstName = sFirstName;
                        if (!string.IsNullOrEmpty(sDOB)) oda.Patient.DateOfBirth = sDOB;
                        oda.Patient.Gender = SexToGender(sSex); ;
                        oda.AccessionNbr = sAccessionNum;
                        oda.PrimaryPhysician.LastName = sDocName;

                        oda.DateOfService = !string.IsNullOrEmpty(sOrderDate) ? Convert.ToDateTime(sOrderDate) : DateTime.Now;
                        if (!string.IsNullOrEmpty(sCollectDate))
                        {
                            DateTime collect = Convert.ToDateTime(sCollectDate);
                            oda.DateOfCollection = collect > oda.DateOfService ? oda.DateOfService : collect;
                        }
                        else
                        {
                            oda.DateOfCollection = oda.DateOfService;
                        }

                        foreach (var test in cOrders)
                        {
                            oda.Tests.AddTest(test.TestCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        //ErrorMessages.Add($"OnORMMessage - Failed loading Order.\n{ex}");
                        _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "OnORMMessage", "Failed loading order.");

                    }

                    if (oda.IsValid)
                    {
                        try
                        {
                            _logger.LogDebug("Saving order.");
                            oda.Save();
                        }
                        catch (Exception ex)
                        {
                            //ErrorMessages.Add($"OnORMMessage - Failed to save Order.\n{ex}");
                            _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "OnORMMessage", "Failed to save order.");

                        }

                        if (cOrders.Count > 0)
                        {
                            var s = OrderManager.CreateReport(oda, true);
                            if (s.StatusType == OrderManager.StatusType.Failure)
                            {
                                //ErrorMessages.Add("OnORMMessage - Report creation failed. Reason: " + s.Message);
                                _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Reason: {Reason}", "Result", "OnORMMessage", "Report creation failed.", s.Message);

                            }
                            else if (s.StatusType == OrderManager.StatusType.Warning)
                            {
                                _logger.LogWarning(s.Message);
                            }
                        }
                    }
                    else
                    {
                        //ErrorMessages.Add("OnORMMessage - Failed to save order: " + oda.GetCompleteRules().ToString());
                        _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Rules: {Rules}", "Result", "OnORMMessage", "Failed to save order.", oda.GetCompleteRules().ToString());

                    }
                }
            }
            catch (Exception ex)
            {
                //ErrorMessages.Add($"OnORMMessage - {ex}");
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "OnORMMessage", "Unhandled exception in OnORMMessage.");

            }
        }
        public async Task OnSSUMessage(SSUMessage message)
        {
            await InitializeAsync();
            _logger.LogDebug("In OnSSUMessage...");

            List<DI.Interface.Report> cReport = new List<DI.Interface.Report>();
            string sMessageId = string.Empty;
            string sAccessionNum = string.Empty;
            string sValueType = string.Empty;
            string sCode = string.Empty;
            string sDesc = string.Empty;
            string sValue = string.Empty;
            string sUnits = string.Empty;
            string sRefRange = string.Empty;
            string sStatus = string.Empty;
            string status = string.Empty;
            string sMachSeq = string.Empty;
            string sRack = string.Empty;
            string sRackPos = string.Empty;
            string sInstId = string.Empty;
            string sReportDate = string.Empty;
            string sTechUser = string.Empty;
            string sReleaseUser = string.Empty;
            string sPerformingLoc = string.Empty;
            string Querytime = string.Empty;
            string EquipmentContainerIdentifier = string.Empty;
            string InstrumentName = string.Empty;
            string PrimaryContainerIdentifier = string.Empty;
            string SpecimenSource = string.Empty;

            try
            {
                sMessageId = message.MSH[0].MessageID;
                InstrumentName = message.EquipmentDetail.EquipmentInstanceIdentifier;
                Querytime = FormatDateTime(message.EquipmentDetail.EventDateTime, "MM/dd/yyyy HH:mm:ss");

                if (message.SpecimenContainerDetail != null)
                {
                    var specimenDetail = message.SpecimenContainerDetail;
                    sAccessionNum = specimenDetail.AccessionIdentifier;
                    EquipmentContainerIdentifier = specimenDetail.EquipmentContainerIdentifier;
                    PrimaryContainerIdentifier = specimenDetail.PrimaryContainerIdentifier;
                    SpecimenSource = specimenDetail.SpecimenSource;
                }

                ActivityHelper.SetAccessionLogKey(sAccessionNum);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    sAccessionNum,
                    DateTime.UtcNow
                );

                var r = new DI.Interface.Report();
                if (message.Result.Count > 0)
                {
                    int iResultCount = message.Result.Count;
                    for (int iThisResult = 0; iThisResult < iResultCount; iThisResult++)
                    {
                        sCode = message.Result[iThisResult].Code;

                        var a = new DI.Interface.ReportAnalyte(sCode, sValue, sReportDate, "", "", "", "", "", status)
                        {
                            Code = sCode
                        };
                        r.AddAnalyte(a);
                    }
                    cReport.Add(r);
                }

                _logger.LogDebug("about to write results.");
                CaptureInstrumentQuery(cReport, sAccessionNum, Querytime, InstrumentName);
            }
            catch (Exception ex)
            {
                //  base.ErrorMessages.Add($"OnSSUMessage - Error in Accession #: {sAccessionNum}\n{ex}");
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Result", "OnSSUMessage", "Error in SSU message.", sAccessionNum);


            }
            finally
            {
                cReport = null;
            }
        }
        public async Task OnSTSMessage(STSMessage message)
        {
            await InitializeAsync();
            _logger.LogDebug("In OnSTSMessage...");
            try
            {
                string accessionNumber = string.Empty;
                DateTime dateServiced = new DateTime(1900, 1, 1);
                string resultDate = string.Empty;
                string testCode = string.Empty;
                string status = string.Empty;
                Bioreference.LIS.Report _report = null;
                ReportAnalytePanel _analytePanel = null;
                Bioreference.LIS.ReportAnalyte _reportAnalyte = null;
                List<Bioreference.LIS.ReportAnalyte> _reportAnalyteList;
                string _outboundMessage = "";
                List<string> statusList;
                StatusList _statusDoc = new StatusList();

                _noBillCodes = NoBillCodes.Fetch();

                var msaGroup = message.MSAGroup[0];
                _logger.LogDebug("MessageID='{0}'", message.MSH.MessageID);

                for (int i = 0; i < msaGroup.MSA.Count; i++)
                {
                    try
                    {
                        string statusMsg = msaGroup.MSA[i].StatusMsg;

                        if (string.IsNullOrEmpty(statusMsg))
                            continue;

                        string[] msgFields = statusMsg.Trim().Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

                        accessionNumber = msgFields[0];
                        _logger.LogDebug("AccessionNbr='{0}'", accessionNumber);
                        if (accessionNumber.Length == 9 && accessionNumber.StartsWith("10"))
                        {
                            accessionNumber = accessionNumber.Substring(2);
                        }

                        resultDate = string.Empty;
                        dateServiced = new DateTime(1900, 1, 1);

                        if (msgFields.Length == 5)
                        {
                            resultDate = msgFields[1] + " " + msgFields[2];
                            testCode = msgFields[3];
                            status = msgFields[4];
                        }
                        else if (msgFields.Length == 4)
                        {
                            dateServiced = ParseDOS(msgFields[1]);
                            testCode = msgFields[2];
                            status = msgFields[3];
                        }
                        _logger.LogDebug("AccessionNbr='{0}', TestCode='{1}', DOS='{2}', ResultDate='{3}', Status='{4}'",
                            accessionNumber, testCode, dateServiced, resultDate, status);

                        if (string.IsNullOrEmpty(accessionNumber))
                            continue;

                        if (_report == null || _report.AccessionNbr != accessionNumber)
                        {
                            _report = dateServiced != new DateTime(1900, 1, 1) && _inboundSettings.SearchResultAccessionWithDOS
                                ? Bioreference.LIS.Report.Fetch(accessionNumber, dateServiced)
                                : Bioreference.LIS.Report.Fetch(accessionNumber);
                        }

                        _logger.LogDebug("Loaded Report='{0}', DOS:='{1}'", _report.AccessionNbr, _report.DateServiced);

                        if (string.IsNullOrEmpty(testCode))
                        {
                            _logger.LogWarning($"OnSTSMessage - Testcode is missing for Accession # {accessionNumber}");
                            continue;
                        }

                        if (status.ToUpper() == "REJECTED")
                        {
                            _logger.LogWarning($"OnSTSMessage - Accession # {accessionNumber} REJECTED by Reporting.");
                            continue;
                        }

                        statusList = new List<string>();
                        _reportAnalyte = _report.FindAnalyte(testCode, true, 0, transmitStatusType.SentToReporting);
                        if (_reportAnalyte != null)
                        {
                            statusList.Add(testCode);
                            _logger.LogDebug("--> Analyte='{0}'", _reportAnalyte.Code);
                        }
                        else
                        {
                            _analytePanel = _report.FindAnalytePanel(testCode);
                            if (_analytePanel != null)
                            {
                                statusList.Add(testCode);
                                _logger.LogDebug("--> Panel='{0}'", _analytePanel.PanelCode);
                            }
                            else
                            {
                                _reportAnalyteList = _report.FindProfileAnalytes(testCode, transmitStatusType.SentToReporting);
                                _logger.LogDebug("--> Profile='{0}', Count={1}", testCode, _reportAnalyteList.Count);
                                if (_reportAnalyteList.Count > 0)
                                {
                                    foreach (var ra in _reportAnalyteList)
                                    {
                                        if (ra.RefAnalyte.IsReportable)
                                        {
                                            statusList.Add(ra.Code);
                                            _logger.LogDebug("    ---> ComponentCode='{0}'", ra.Code);
                                        }
                                    }
                                }
                                else
                                {
                                    statusList.Add(testCode);
                                    _logger.LogDebug("---> ? Analyte='{0}'", testCode);
                                }
                            }
                        }

                        foreach (string innerCode in statusList)
                        {
                            bool saveReport = true;
                            DI.Interface.ReportStatus _reportStatus = new DI.Interface.ReportStatus();
                            bool doNotSendToVertex = false;
                            bool isAnalyte = false;
                            bool isPanel = false;
                            bool isProfile = false;

                            _reportStatus.AccessionNumber = accessionNumber;
                            _reportStatus.TestCode = innerCode;
                            _reportAnalyteList = new List<Bioreference.LIS.ReportAnalyte>();

                            _logger.LogDebug("starting accessionNumber='{0}', innerCode='{1}', isAnalyte='{2}', isPanel='{3}', isProfile='{4}', _reportAnalyteList.Count={5}, doNotSendToVertex='{6}'",
                                accessionNumber, innerCode, isAnalyte, isPanel, isProfile, _reportAnalyteList.Count, doNotSendToVertex);

                            _reportAnalyte = _report.FindAnalyte(innerCode, true, 0, transmitStatusType.SentToReporting);
                            if (_reportAnalyte != null)
                            {
                                _logger.LogDebug("Add Analyte: reportAnalyteList.Add('{0}')", _reportAnalyte.Code);
                                _reportAnalyteList.Add(_reportAnalyte);
                                isAnalyte = true;
                            }
                            else
                            {
                                _analytePanel = _report.FindAnalytePanel(innerCode);
                                if (_analytePanel != null)
                                {
                                    isPanel = true;
                                }
                                else
                                {
                                    isPanel = false;
                                    _reportAnalyteList = _report.FindProfileAnalytes(innerCode, transmitStatusType.SentToReporting);
                                    if (_reportAnalyteList.Count > 0)
                                    {
                                        _logger.LogDebug("Add Profile: _reportAnalyteList.Count={0}", _reportAnalyteList.Count);
                                        isProfile = true;
                                    }
                                }
                            }

                            _logger.LogDebug("InnerCode='{0}', isAnalyte={1} isPanel={2} isProfile={3}", innerCode, isAnalyte, isPanel, isProfile);

                            if (isAnalyte || isProfile)
                            {
                                _logger.LogDebug("isAnalyte or isProfile --> ReportAnalyteList.Count={0}", _reportAnalyteList.Count);
                                foreach (var _analyte in _reportAnalyteList)
                                {
                                    _logger.LogDebug("processing {0}", _analyte.Code);
                                    doNotSendToVertex = VerifyIfBypassVertexNotification(_analyte, _report);
                                    _logger.LogDebug("doNotSendToVertex='{0}', isSendOut='{1}'", doNotSendToVertex, _analyte.Analyte.IsSendOut);

                                    if (_analyte.Analyte.IsSendOut)
                                    {
                                        if (_analyte.RefLabPerformingFacilityId == -1)
                                        {
                                            _reportStatus.LocationCode = _analyte.PerformingFacility;
                                        }
                                        else
                                        {
                                            _reportStatus.LocationCode = FindReflabCode(_analyte.Analyte.ReferenceLabId);
                                        }
                                        
                                        if (_analyte.RefLabPerformingFacilityId != -1 && ! string.IsNullOrEmpty(_appSettings.SendOutLabId) && _appSettings.SendOutLabId != "0")
                                        {
                                            _analyte.SetPerformingFacility(_appSettings.SendOutLabId);
                                        }
                                    }
                                    else
                                    {
                                        _reportStatus.LocationCode = _analyte.PerformingFacility;
                                    }

                                    if (_analyte.Parent is ReportAnalytePanel p)
                                    {
                                        _reportStatus.TestCode = p.Panel.PanelCode.PadLeft(4, '0');
                                        _logger.LogDebug("ParentType=ReportAnalytePanel --> Panel='{0}', TransmitStatus='{1}', IsPreliminaryReleased='{2}', ResultValue='{3}'",
                                            p.PanelCode, p.TransmitStatus, p.IsPreliminaryReleased, _analyte.ResultValue);

                                        if (p.TransmitStatus == transmitStatusType.SentToReporting)
                                        {
                                            if (!p.IsPreliminaryReleased)
                                            {
                                                p.MarkAsStatusSentToVertex();
                                                saveReport = true;
                                                if (_analyte.ResultValue.ToLower().Equals("to follow"))
                                                {
                                                    _reportStatus.IsToFollow = true;
                                                }
                                            }
                                            else
                                            {
                                                saveReport = false;
                                                _reportStatus.IsPreliminary = true;
                                            }
                                        }
                                        else
                                        {
                                            saveReport = false;
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogDebug("ParentType Not ReportAnalytePanel --> TransmitStatus={0}, IsPreliminaryReleased={1}, ResultValue={2}",
                                            _analyte.TransmitStatus, _analyte.IsPreliminaryReleased, _analyte.ResultValue);
                                        _reportStatus.TestCode = innerCode;
                                        if (_analyte.TransmitStatus == transmitStatusType.SentToReporting &&
                                            (_analyte.ResultStatus == Bioreference.LIS.resultStatusType.Final ||
                                             _analyte.ResultStatus == Bioreference.LIS.resultStatusType.Corrected))
                                        {
                                            if (!_analyte.IsPreliminaryReleased)
                                            {
                                                _analyte.MarkAsStatusSentToVertex();
                                                saveReport = true;
                                                if (_analyte.ResultValue.ToLower().Equals("to follow"))
                                                {
                                                    _reportStatus.IsToFollow = true;
                                                }
                                            }
                                            else
                                            {
                                                saveReport = false;
                                                _reportStatus.IsPreliminary = true;
                                            }
                                        }
                                        else
                                        {
                                            saveReport = false;
                                        }
                                    }
                                    _reportStatus.Status = GetResultStatus(_analyte.ResultValue);
                                    _logger.LogDebug("_reportStatus.Status={0}", _reportStatus.Status);
                                }
                            }
                            else if (isPanel)
                            {
                                _logger.LogDebug("isPanel --> analytePanel.Analytes.List.Count={0}", _analytePanel.Analytes.List.Count);
                                _reportStatus.TestCode = innerCode;
                                if (_analytePanel.Panel.ReferenceLabId != 0)
                                {
                                    _logger.LogDebug("Panel.ReferenceLabId has value --> ReferenceLabId={0}", _analytePanel.Panel.ReferenceLabId);
                                    String firstAnalytePF = FindFirstPerfomingFacility(_analytePanel);
                                    if (!string.IsNullOrEmpty(firstAnalytePF))
                                    {
                                        _reportStatus.LocationCode = firstAnalytePF;
                                    }
                                    else
                                    {
                                        _reportStatus.LocationCode = FindReflabCode(_analytePanel.Panel.ReferenceLabId);
                                        if (!string.IsNullOrEmpty(_appSettings.SendOutLabId) && !_appSettings.SendOutLabId.Equals("0"))
                                        {
                                            _analytePanel.SetPerformingFacility(_appSettings.SendOutLabId);
                                        }
                                    }
                                }
                                else
                                {
                                    _reportStatus.LocationCode = GetFirstPerformingFacility(_analytePanel);
                                }

                                if (_analytePanel.TransmitStatus == Bioreference.LIS.transmitStatusType.SentToReporting)
                                {
                                    if (!_analytePanel.IsPreliminaryReleased)
                                    {
                                        _analytePanel.MarkAsStatusSentToVertex();
                                        saveReport = true;
                                    }
                                    else
                                    {
                                        saveReport = false;
                                        _reportStatus.IsPreliminary = true;
                                    }

                                    DI.Interface.ResultStatusType panelStatus = DI.Interface.ResultStatusType.None;
                                    ReportAnalyteList pList = _analytePanel.Analytes.List;
                                    for (int x = 0; x < pList.Count; x++)
                                    {
                                        var ra = pList[x];
                                        if (ra.HasBeenReleased())
                                        {
                                            if (panelStatus == DI.Interface.ResultStatusType.None)
                                            {
                                                panelStatus = GetResultStatus(ra.ResultValue);
                                            }
                                            else if (!string.IsNullOrEmpty(ra.ResultValue) && panelStatus != GetResultStatus(ra.ResultValue))
                                            {
                                                panelStatus = DI.Interface.ResultStatusType.None;
                                                break;
                                            }
                                            _logger.LogDebug("PanelStatus AccessionNbr='{0}', TestCode='{1}', ResultValue='{2}', PanelStatus='{3}'",
                                                _reportStatus.AccessionNumber, ra.Code, ra.ResultValue, panelStatus);
                                        }
                                    }
                                    _reportStatus.Status = panelStatus;
                                }
                                else
                                {
                                    saveReport = false;
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Unknown ReportAnalyte Type");
                                _logger.LogWarning($"OnසOnSTSMessage - Status returned for unknown ReportAnalyte {innerCode} for Accession # {accessionNumber}.");
                                continue;
                            }

                            if (DateTime.TryParse(resultDate, out DateTime parsedResultDate))
                            {
                                _reportStatus.ResultDate = parsedResultDate;
                            }
                            else
                            {
                                _reportStatus.ResultDate = DateTime.Now;
                            }

                            _logger.LogDebug("_reportStatus.IsPreliminary='{0}', doNotSendToVertex='{1}', _report.IsValid='{2}', saveReport='{3}'",
                                _reportStatus.IsPreliminary, doNotSendToVertex, _report.IsValid, saveReport);

                            if (_reportStatus.IsPreliminary)
                            {
                                if (!doNotSendToVertex)
                                {
                                    string msg = MessageToVertex(_reportStatus, _report.DateServiced);
                                    _logger.LogDebug("MessageToVertex.1='{0}'", msg);
                                    if (string.IsNullOrEmpty(msg))
                                        continue;
                                    _outboundMessage += "\r\n" + msg;
                                }

                                if (!_statusDoc.ReportExists(_reportStatus.AccessionNumber, _reportStatus.TestCode))
                                {
                                    _statusDoc.Add(_reportStatus);
                                }
                            }
                            else if (saveReport)
                            {
                                if (!_report.IsValid)
                                {
                                    _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; InvalidRules: {InvalidRules}", "Result", "OnSTSMessage", "Inbound result status save invalid.", accessionNumber, _report.Rules);
                                }
                                else
                                {
                                    if (!doNotSendToVertex)
                                    {
                                        string msg = MessageToVertex(_reportStatus, _report.DateServiced);
                                        _logger.LogDebug("MessageToVertex.2='{0}'", msg);
                                        if (string.IsNullOrEmpty(msg))
                                            continue;
                                        _outboundMessage += "\r\n" + msg;
                                    }

                                    _logger.LogWarning("Saving report");

                                    List<string> olauditList = _report.GetFormattedAuditItems;

                                    _report.Save();

                                    if (_appSettings.Inbound_SendAuditToAuditChannel)
                                    {
                                        string sAudit = String.Empty;
                                        try
                                        {
                                            List<string> auditList = _report.GetFormattedAuditItems.Except(olauditList).ToList();
                                            if (auditList?.Count > 0)
                                            {
                                                sAudit = ConvertToSingleString(auditList);
                                                _logger.LogDebug("Calling MessageToAuditChannel");
                                                //MessageToAuditChannel(sAudit);
                                                await MessageToAuditKafka(sAudit);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogDebug($"Unable to write audit: {ex}");
                                            _logger.LogWarning($"OnSTSMessage - Unable to write Audit for Accession: {_report.AccessionNbr}\r\n{sAudit}\r\n{ex}");
                                        }
                                    }

                                    if (!_statusDoc.ReportExists(_reportStatus.AccessionNumber, _reportStatus.TestCode))
                                    {
                                        _statusDoc.Add(_reportStatus);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"OnSTSMessage - Unable to update status for AccessionNbr:{accessionNumber} Code:{testCode}.\r\n{ex}");
                    }
                }

                _logger.LogDebug("End MessageId='{0}'", message.MSH.MessageID);
                _logger.LogDebug("Outbound Message: {0}", _outboundMessage);
                //OutboundMessage = _outboundMessage;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"OnSTSMessage - {ex}");
            }
        }
        private void CaptureInstrumentQuery(List<DI.Interface.Report> reportList, string accessionNum, string queryTime, string instrumentName)
        {
            Bioreference.LIS.Report report;
            List<string> analyteList = new List<string>();

            foreach (var rpt in reportList)
            {
                foreach (var diAnalyte in rpt.AnalyteList)
                {
                    analyteList.Add(diAnalyte.Code);
                }
            }

            _logger.LogDebug("Start instrument query processing");
            DateTime expirationDate = DateTime.Now.AddDays(_inboundSettings.InstrumentQueryExpirationDays);

            if (reportList.Count > 0)
            {
                _logger.LogDebug("reportList.Count={0}", reportList.Count);
                report = OrderManager.FetchReport(accessionNum);
                if (report != null && SharedFunctions.AccessionAgeInRange(report.DateServiced))
                {
                    _logger.LogDebug("report fetched");
                    foreach (DI.Interface.Report rpt in reportList)
                    {
                        foreach (DI.Interface.ReportAnalyte diAnalyte in rpt.AnalyteList)
                        {
                            _logger.LogDebug("checking analyte={0}", diAnalyte.Code);

                            string panelCode = string.Empty;
                            ReportAnalytePanel analytePanel = null;

                            LIS.ReportAnalyte analyte = report.FindAnalyte(diAnalyte.Code, true);
                            if (analyte == null)
                            {
                                LIS.ReportAnalytePanel panel = report.FindAnalytePanel(diAnalyte.Code);
                                if (panel != null)
                                {
                                    _logger.LogDebug("panel found");
                                    foreach (LIS.ReportAnalyte a in panel.Analytes.List)
                                    {
                                        _logger.LogDebug("analyte found, InstrumentLoadTime={0}", queryTime);
                                        a.InstrumentLoadTime = DateTime.ParseExact(queryTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogDebug("analyte found, InstrumentLoadTime={0}", queryTime);
                                analyte.InstrumentLoadTime = DateTime.ParseExact(queryTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            }

                            _logger.LogDebug("looping through analytes");
                            foreach (LIS.ReportAnalyte a in report.Analytes.List)
                            {
                                foreach (var item in ((RefAnalyte)a.Analyte).TestInstruments)
                                {
                                    if (instrumentName.ToLower() == item.InstrumentName.ToLower())
                                    {
                                        _logger.LogDebug("setting analyte {0} with {1}", a.Code, queryTime);
                                        a.InstrumentLoadTime = DateTime.ParseExact(queryTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                    }
                                }
                            }

                            //Logger.Debug("looping through panels");
                            _logger.LogDebug("looping through panels");
                            foreach (ReportAnalytePanel p in report.AnalytePanels.List)
                            {
                                _logger.LogDebug("panel {0}", p.PanelCode);
                                foreach (LIS.ReportAnalyte a in p.Analytes.List)
                                {
                                    foreach (var item in ((RefAnalyte)a.Analyte).TestInstruments)
                                    {
                                        if (instrumentName.ToLower() == item.InstrumentName.ToLower())
                                        {
                                            _logger.LogDebug("setting analyte {0} with {1}", a.Code, queryTime);
                                            a.InstrumentLoadTime = DateTime.ParseExact(queryTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }
                                    }
                                }
                            }

                            report.Save();
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("insert instrumentQueue accessionNbr={0}, analyteList.Count={1}, instrumentName={2}, queryTime={3}", accessionNum, analyteList.Count, instrumentName, queryTime);
                    InstrumentQueries.Insert(accessionNum, analyteList, instrumentName, queryTime, expirationDate);
                    return;
                }
            }
            else if (reportList.Count == 0)
            {
                _logger.LogDebug("reportList.Count={0}", reportList.Count);
                report = OrderManager.FetchReportWithState(accessionNum);
                if (report != null && SharedFunctions.AccessionAgeInRange(report.DateServiced))
                {
                    _logger.LogDebug("report fetched");
                    _logger.LogDebug("looping through analytes");
                    foreach (LIS.ReportAnalyte a in report.Analytes.List)
                    {
                        _logger.LogDebug("checking analyte={0}", a.Code);
                        foreach (var item in ((RefAnalyte)a.Analyte).TestInstruments)
                        {
                            if (instrumentName.ToLower() == item.InstrumentName.ToLower())
                            {
                                _logger.LogDebug("analyte found, InstrumentLoadTime={0}", queryTime);
                                a.InstrumentLoadTime = DateTime.ParseExact(queryTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    _logger.LogDebug("looping through panels");
                    foreach (ReportAnalytePanel p in report.AnalytePanels.List)
                    {
                        _logger.LogDebug("panel {0}", p.PanelCode);
                        foreach (LIS.ReportAnalyte a in p.Analytes.List)
                        {
                            _logger.LogDebug("checking analyte={0}", a.Code);
                            foreach (var item in ((RefAnalyte)a.Analyte).TestInstruments)
                            {
                                if (instrumentName.ToLower() == item.InstrumentName.ToLower())
                                {
                                    _logger.LogDebug("setting analyte {0} with {1}", a.Code, queryTime);
                                    a.InstrumentLoadTime = DateTime.ParseExact(queryTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                }
                            }
                        }
                    }
                    report.Save();
                }
                else
                {
                    _logger.LogDebug("insert instrumentQueue accession={0}, analyteList.Count={1}, instrumentName={2}, queryTime={3}", accessionNum, analyteList.Count, instrumentName, queryTime);
                    InstrumentQueries.Insert(accessionNum, analyteList, instrumentName, queryTime, expirationDate);
                }
            }

            _logger.LogDebug("End instrument query processing");
        }

        private async Task ParseRefLabResult(ORUMessage message)
        {
            var cReport = new List<DI.Interface.Report>();

            string sMessageId = string.Empty;
            string sAddress1 = string.Empty, sAddress2 = string.Empty, sCity = string.Empty, sState = string.Empty, sZip = string.Empty;
            string sHomePhone = string.Empty, sWorkPhone = string.Empty, sSsn = string.Empty;
            string sLastName = string.Empty, sFirstName = string.Empty, sDOB = string.Empty, sSex = string.Empty;
            string sAccessionNum = string.Empty, sDocName = string.Empty, sOrderDate = string.Empty, sCollectDate = string.Empty;
            string sRefLabNum = string.Empty, sClientAcctNum = string.Empty;
            string sDrLastName = string.Empty, sDrFirstName = string.Empty, sDrMiddleName = string.Empty, sDrSuffix = string.Empty;
            string sLACText = string.Empty, sTestCode = string.Empty, sTestDescr = string.Empty, sLRCText = string.Empty;
            string sValueType = string.Empty, sCode = string.Empty, sDesc = string.Empty, sValue = string.Empty, sUnits = string.Empty;
            string sRefRange = string.Empty, sStatus = string.Empty, sReportDate = string.Empty, sRefLab = string.Empty;
            string sRCText = string.Empty, sPrelimRelease = string.Empty, sLabReportStatus = string.Empty;

            //Logger.Debug("In ParseRefLabResult...");
            _logger.LogDebug("In ParseRefLabResult...");

            try
            {
                sMessageId = message.MSH.MessageID;
                var patient = message.PatientInfo;
                sLastName = patient.LastName;
                sFirstName = patient.FirstName;
                sDOB = FormatDateTime(patient.DOB, "MM/dd/yyyy");
                sSex = patient.Sex;
                sAddress1 = patient.Address1;
                sAddress2 = patient.Address2;
                sCity = patient.City;
                sState = patient.State;
                sZip = patient.Zip;
                sHomePhone = patient.HomePhone;
                sWorkPhone = patient.WorkPhone;
                sSsn = patient.SSN;

                var labAccession = message.LabAccession[0];
                sAccessionNum = labAccession.AccessionNumber;
                sRefLabNum = labAccession.ReferenceLabNumber;
                sClientAcctNum = labAccession.ClientAccountNumber;
                sDrLastName = labAccession.OrderingDoctorLast;
                sDrFirstName = labAccession.OrderingDoctorFirst;
                sDrMiddleName = labAccession.OrderingDoctorMiddle;
                sDrSuffix = labAccession.OrderingDoctorName;
                sCollectDate = FormatDateTime(labAccession.CollectDate, "MM/dd/yyyy");

                if (message.LabAccessionComment?.Count > 0)
                {
                    sLACText = DoComments(message.LabAccessionComment, note => note.NoteText);
                }

                foreach (var labReportGroup in message.LabReport)
                {
                    if (!_appSettings.InboundEngine_DisregardPanelCode)
                    {
                        sTestCode = labReportGroup.Obr.TestCode;
                    }

                    //Logger.Debug(sTestCode);
                    _logger.LogDebug(sTestCode);
                    sLabReportStatus = labReportGroup.Obr.LabReportStatus;

                    if (sLabReportStatus == "C" && _appSettings.InboundEngine_ErrorOnCorrected)
                    {
                        //ErrorMessages.Add($"ORUMessage - Accession #: {sAccessionNum} Message: Restricted from processing corrected results - TestCode: {sTestCode}");
                        _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "Result", "ORUMessage", "Restricted from processing corrected results.", sAccessionNum, sTestCode);
                        return;
                    }

                    if (sLabReportStatus.ToUpper() == "X")
                    {
                        //ErrorMessages.Add($"ORUMessage - Accession #: {sAccessionNum} Message: Restricted from processing canceled results. Status: 'X' and TestCode: {sTestCode}");
                        _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "Result", "ORUMessage", "Restricted from processing canceled results (Status 'X').", sAccessionNum, sTestCode);
                        return;
                    }

                    sTestDescr = labReportGroup.Obr.TestDescription;
                    sPrelimRelease = labReportGroup.Obr.PreliminaryRelease;

                    if (labReportGroup.LabReportComment?.Count > 0)
                    {
                        sLRCText = DoComments(labReportGroup.LabReportComment, note => note.NoteText);
                    }

                    var r = new DI.Interface.Report(sTestCode, sLRCText, sTestDescr)
                    {
                        IsPreliminaryRelease = sPrelimRelease != "F",
                        AccessionNum = sAccessionNum,
                        LabReportStatus = sLabReportStatus
                    };

                    if (sLRCText.Length > 0 && _inboundSettings.Inbound_AutoGen_PanelCmtInDummyOBX)
                    {
                        var a = new DI.Interface.ReportAnalyte(sTestCode, "IDENTIFER_PANELCMT", "")
                        {
                            AnalyteComment = sLRCText
                        };
                        r.AddAnalyte(a);
                    }

                    if (labReportGroup.Result?.Count > 0)
                    {
                        foreach (var resultGroup in labReportGroup.Result)
                        {
                            var result = resultGroup.Obx[0];
                            sCode = result.Code;
                            sValueType = result.ValueType;
                            sDesc = result.Description;
                            sValue = result.Value;
                            sUnits = result.Units;
                            sRefRange = result.ReferenceRange;
                            sStatus = result.Status;

                            //Logger.DebugFormat("Value: '{0}', RefRange: '{1}', Units: '{2}'", sValue, sRefRange, sUnits);
                            _logger.LogDebug("Value: '{0}', RefRange: '{1}', Units: '{2}'", sValue, sRefRange, sUnits);

                            if (sStatus == "C" && _appSettings.Inbound_ErrorOnCorrected)
                            {
                                //ErrorMessages.Add($"ORUMessage - Unable to process corrected result for {sCode}.");
                                _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Code: {Code}", "Result", "ORUMessage", "Unable to process corrected result.", sCode);
                                return;
                            }

                            if (sStatus.ToUpper() == "X")
                            {
                                //ErrorMessages.Add($"ORUMessage - Unable to process canceled result for {sCode}. Status:'X'");
                                _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Code: {Code}", "Result", "ORUMessage", "Unable to process canceled result (Status 'X').", sCode);
                                return;
                            }

                            sReportDate = FormatDateTime(result.ReportDate, "MM/dd/yyyy");
                            sRefLab = result.ReferenceLabId;

                            if (resultGroup.ResultComment?.Count > 0)
                            {
                                sRCText = DoComments(resultGroup.ResultComment, note => note.NoteText);
                            }

                            var a = new DI.Interface.ReportAnalyte(sCode, sValue, sReportDate)
                            {
                                ReferenceLabNumber = sRefLab,
                                ValueType = sValueType,
                                Description = sDesc,
                                Units = sUnits,
                                ReferenceRange = sRefRange,
                                Status = sStatus
                            };

                            r.AddAnalyte(a);
                        }
                    }

                    cReport.Add(r);
                }

                if (_appSettings.Inbound_DeleteDuplicateCmts)
                {
                    DeleteDuplicateComments(cReport);
                }

                //Logger.Debug("about to write results.");
                _logger.LogDebug("about to write results.");
                var stopwatch = Stopwatch.StartNew();
                await WriteResultsToObjects(cReport, sAccessionNum);
                stopwatch.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}, ElapsedTime {Elapsed} ms", "Result", "WriteResultsToObjects", "Writing to objects completed.", stopwatch.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                //ErrorMessages.Add($"ORUMessage - Error in Accession #: {sAccessionNum}\n{ex}");
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Result", "ORUMessage", "Error in ORU message.", sAccessionNum);

            }
            finally
            {
                cReport = null;
            }
        }
        private void DeleteDuplicateComments(List<DI.Interface.Report> repList)
        {
            // PrintReport(repList, "Before")
            foreach (DI.Interface.Report r in repList)
            {
                List<string> lstTmp;
                var lstOBX = new Dictionary<string, bool>();
                List<int> lstMakedToDeleted;
                int i;
                int y;
                // '''''''''''''''''''''''''''''''''''' OBX comments '''''''''''''''''''''''''''''''''''''
                lstTmp = new List<string>();

                for (y = r.AnalyteList.Count - 1; y >= 0; y -= 1)
                {
                    lstMakedToDeleted = new List<int>();
                    var loopTo = r.AnalyteList[y].NTEs.Count - 1;
                    for (i = 0; i <= loopTo; i++) // OBX Level
                    {
                        if (!lstOBX.ContainsKey(r.AnalyteList[y].NTEs[i].Text))
                        {
                            // Added
                            lstOBX.Add(r.AnalyteList[y].NTEs[i].Text, false);
                        }
                    }
                }

                for (y = r.AnalyteList.Count - 1; y >= 0; y -= 1)
                {
                    lstMakedToDeleted = new List<int>();
                    var loopTo1 = r.AnalyteList[y].NTEs.Count - 1;
                    for (i = 0; i <= loopTo1; i++) // OBX Level
                    {
                        if (lstOBX.ContainsKey(r.AnalyteList[y].NTEs[i].Text))
                        {
                            if (lstOBX[r.AnalyteList[y].NTEs[i].Text] == false)
                            {
                                lstOBX[r.AnalyteList[y].NTEs[i].Text] = true;
                            }
                            else
                            {
                                // Delete
                                lstMakedToDeleted.Add(i);
                            }
                        }
                    }
                    for (i = r.AnalyteList[y].NTEs.Count - 1; i >= 0; i -= 1)
                    {
                        if (lstMakedToDeleted.Contains(i))
                        {
                            r.AnalyteList[y].NTEs.RemoveAt(i);
                        }
                    }
                }

                // '''''''''''''''''''''''''''''''''''' OBX & OBR comments '''''''''''''''''''''''''''''''''''''
                lstTmp = new List<string>();
                lstMakedToDeleted = new List<int>();
                foreach (DI.Interface.ReportAnalyte a in r.AnalyteList)
                {
                    var loopTo2 = a.NTEs.Count - 1;
                    for (i = 0; i <= loopTo2; i++)
                        lstTmp.Add(a.NTEs[i].Text);  // Add all comments to OBX level
                }
                var loopTo3 = r.NTEs.Count - 1;
                for (i = 0; i <= loopTo3; i++) // OBR Level
                {
                    if (lstTmp.Contains(r.NTEs[i].Text))
                    {
                        // Delete
                        lstMakedToDeleted.Add(i);
                    }
                    else
                    {
                        // Added
                        lstTmp.Add(r.NTEs[i].Text);
                    }
                }
                for (i = r.NTEs.Count - 1; i >= 0; i -= 1)
                {
                    if (lstMakedToDeleted.Contains(i))
                    {
                        r.NTEs.RemoveAt(i);
                    }
                }
                // '''''''''''''''''''''''''''''''''''' OBR Comments '''''''''''''''''''''''''''''''''''''
                lstTmp = new List<string>();
                lstMakedToDeleted = new List<int>();
                var loopTo4 = r.NTEs.Count - 1;
                for (i = 0; i <= loopTo4; i++)
                {
                    if (lstTmp.Contains(r.NTEs[i].Text))
                    {
                        // Delete
                        lstMakedToDeleted.Add(i);
                    }
                    else
                    {
                        // Added
                        lstTmp.Add(r.NTEs[i].Text);
                    }
                }
                for (i = r.NTEs.Count - 1; i >= 0; i -= 1)
                {
                    if (lstMakedToDeleted.Contains(i))
                    {
                        r.NTEs.RemoveAt(i);
                    }
                }
                // dummy OBX
                int ii;
                int yy;
                // Remove dummy if it don't have comments
                for (ii = r.AnalyteList.Count - 1; ii >= 0; ii -= 1)
                {
                    if (r.TestCode.Equals(r.AnalyteList[ii].Code))
                    {
                        // a is a dummy
                        var loopTo5 = r.AnalyteList[ii].NTEs.Count - 1;
                        for (yy = 0; yy <= loopTo5; yy++)
                            r.NTEs.Add(r.AnalyteList[ii].NTEs[yy]);
                        // if there are not comments at dummy level then remove the dummy
                        if (r.AnalyteList[ii].NTEs == null || r.AnalyteList[ii].NTEs.Count == 0)
                        {
                            r.AnalyteList.RemoveAt(ii);
                        }
                    }
                }
            }
            // PrintReport(repList, "After")
        }

        public string FormatDateTime(string rawDate, string format = "yyyy/MM/dd HH:mm:ss")
        {
            if (DateTime.TryParseExact(rawDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.ToString(format);
            }
            else
            {
                return string.Empty; // or handle invalid date case
            }
        }

        public DateTime? ParseDateTime(string rawDate)
        {
            if (DateTime.TryParseExact(rawDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }
            else
            {
                return null; // Use nullable DateTime to indicate invalid parse
            }
        }
        protected string DoComments(List<LabComment> tTable, Func<LabComment, string> fieldSelector)
        {
            string sComment = "";
            int iCountEmptyLine = 0;
            List<string> commentList = new List<string>();

            try
            {
                foreach (var note in tTable)
                {
                    string fieldValue = fieldSelector(note);

                    if (!string.IsNullOrEmpty(fieldValue))
                    {
                        //if (!My.Settings.BaseEngine_EnableDuplicateNTESuppress || !commentList.Contains(fieldValue))
                        if (!_appSettings.BaseEngine_EnableDuplicateNTESuppress || !commentList.Contains(fieldValue))
                        {
                            commentList.Add(fieldValue);
                        }
                        iCountEmptyLine = 0;
                    }
                    else
                    {
                        iCountEmptyLine++;
                        if (iCountEmptyLine > 1)
                        {
                            if (sComment.Length > Environment.NewLine.Length)
                            {
                                sComment = sComment.Substring(0, sComment.Length - Environment.NewLine.Length);
                                commentList.Add(sComment);
                            }
                            break;
                        }
                        sComment += Environment.NewLine;
                    }
                }

                foreach (string c in commentList)
                {
                    sComment += c + Environment.NewLine;
                }

                foreach (string asci in _appSettings.BaseEngine_NtesLineBreakAscii)
                {
                    char ascii = (char)Convert.ToInt32(asci);
                    sComment = sComment.Replace(ascii.ToString(), Environment.NewLine);
                }

                //Log.Debug(sComment);
                _logger.LogDebug(sComment);
                return sComment;
            }
            catch (Exception ex)
            {
                throw new Exception("DoComments (lambda selector) " + ex.Message);
            }
        }
        private async Task ParseInstrumentResult(ORUMessage message)
        {
            var cReport = new List<DI.Interface.Report>();

            // MSH Variables
            string sMessageId = string.Empty;

            // PatientInfo Variables
            string sLastName = string.Empty, sFirstName = string.Empty, sDOB = string.Empty, sSex = string.Empty, sAccessionNum = string.Empty, sDocName = string.Empty, sOrderDate = string.Empty, sCollectDate = string.Empty;

            // LabAccession Variables
            string sClientAcctNum = string.Empty;

            // LabAccessionComment Variables
            string sLACText = string.Empty;

            // LabReport Variables
            string sTestCode = string.Empty;

            // Result Variables
            string sCode = string.Empty, sValue = string.Empty, sReportDate = string.Empty, sMachSeq = string.Empty, sRack = string.Empty, sRackPos = string.Empty, sInstId = string.Empty, sTechUser = string.Empty, sReleaseUser = string.Empty, sPerformingLoc = string.Empty, status = string.Empty;


            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "ParseInstrumentResult", "In ParseInstrumentResult...");


            try
            {
                sMessageId = message.MSH.MessageID;

                var patient = message.PatientInfo;
                sLastName = patient.LastName;
                sFirstName = patient.FirstName;
                sDOB = FormatDateTime(patient.DOB, "MM/dd/yyyy");
                sSex = patient.Sex;

                var labAccession = message.LabAccession[0];
                sAccessionNum = labAccession.AccessionNumber;
                sClientAcctNum = labAccession.ClientAccountNumber;
                sDocName = labAccession.OrderingDoctorLast;
                sCollectDate = FormatDateTime(labAccession.CollectDate, "MM/dd/yyyy");
                
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    sAccessionNum,
                    DateTime.UtcNow
                );

                if (!_appSettings.Inbound_SkipNTESegments && message.LabAccessionComment.Count > 0)
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; LabAccessionComment: {LabAccessionComment}", "Result", "ParseInstrumentResult", "Lab accession comment found.", message.LabAccessionComment);

                    sLACText = DoComments(message.LabAccessionComment, note => note.NoteText);
                }

                int iReportCount = message.LabReport.Count;

                for (int iThisReport = 0; iThisReport < iReportCount; iThisReport++)
                {
                    var labReportGroup = message.LabReport[iThisReport];
                    sTestCode = labReportGroup.Obr.TestCode;

                    var r = new DI.Interface.Report(sTestCode);

                    if (labReportGroup.Obr.LabReportStatus.ToUpper() == "X")
                    {
                        if (_inboundSettings.InboundEngine_ProcessTNPInOBR && IsAllowedAccessionsForTNPOBR(sAccessionNum))
                        {
                            string comment = DoComments(labReportGroup.LabReportComment, note => note.NoteText);
                            r.OBRComment = SharedFunctions.FormatCommentText(comment, 90);
                            r.OrderedCode = sTestCode;
                            r.ReportResult = labReportGroup.Obr.LabReportStatus;
                        }
                        else
                        {
                            _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "Result", "ORUMessage", "Restricted from processing TNP results in OBR (Status 'X').", sAccessionNum, sTestCode);
                        }
                        r.IsOBRProcessing = true;
                    }

                    if (labReportGroup.Result.Count > 0)
                    {
                        int iResultCount = labReportGroup.Result.Count;
                        for (int iThisResult = 0; iThisResult < iResultCount; iThisResult++)
                        {
                            var result = labReportGroup.Result[iThisResult].Obx[0];

                            sCode = result.Code;
                            sValue = result.Value;
                            status = result.Status;
                            sMachSeq = result.MachineSequence;
                            sRack = result.Rack;
                            sRackPos = result.RackPosition;
                            sInstId = result.InstrumentId;
                            sReportDate = FormatDateTime(result.ReportDate, "MM/dd/yyyy");
                            sTechUser = result.TechUser;
                            sReleaseUser = result.ReleaseUser;
                            sPerformingLoc = result.PerformLocation;

                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; TechUser: {TechUser}; ReleaseUser: {ReleaseUser}", "Result", "ParseInstrumentResult", "Tech and release user extracted.", sTechUser, sReleaseUser);


                            string sRCText = string.Empty;
                            if (!_appSettings.Inbound_SkipNTESegments && labReportGroup.Result[iThisResult].ResultComment.Count > 0)
                            {
                                sRCText = DoComments(labReportGroup.Result[iThisResult].ResultComment, note => note.NoteText);
                                sRCText = ResultService.Common.Utilities.FormatFixedWidthText(sRCText, NOTEFIXEDWIDTH);
                                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; RCText: {RCText}", "Result", "ParseInstrumentResult", "RC text extracted.", sRCText);

                            }

                            var a = new DI.Interface.ReportAnalyte(sCode, sValue, sReportDate, "", "", "", "", "", status)
                            {
                                MachineSequence = sMachSeq,
                                Rack = sRack,
                                RackPosition = sRackPos,
                                InstrumentId = sInstId,
                                TechUser = sTechUser,
                                ReleaseUser = sReleaseUser,
                                PerformingFacility = sPerformingLoc
                            };

                            // int iFlagCount = labReportGroup.Results[iThisResult].FlagGroup[0].Flags.Count;
                            int iFlagCount = labReportGroup?.Result?[iThisResult]?.FlagGroup?[0]?.Flags?.Count ?? 0;
                            for (int iThisFlag = 0; iThisFlag < iFlagCount; iThisFlag++)
                            {
                                List<string>? flags = labReportGroup?.Result?[iThisResult]?.FlagGroup?[0]?.Flags?[iThisFlag]?.Flag;
                                if (flags != null)
                                {
                                    foreach (string flag in flags)
                                    {
                                        a.AddAlert(flag);
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(sRCText))
                            {
                                a.AnalyteComment = sRCText;
                                sRCText = string.Empty;
                            }

                            r.AddAnalyte(a);
                        }
                    }

                    cReport.Add(r);
                }



                var accNbr = new AccessionNbr(sAccessionNum);
                var stopwatch = Stopwatch.StartNew();

                if (accNbr.IsValid)
                {
                    ActivityHelper.SetAccessionLogKey(sAccessionNum);
                    await WriteResultsToObjects(cReport, sAccessionNum);
                }
                else
                {
                    ActivityHelper.SetLogKey(sAccessionNum);
                    _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Result", "WriteResultsToObjects", "Bypassing message due to invalid accession number.", sAccessionNum);                 
                }

                stopwatch.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}, ElapsedTime {Elapsed} ms", "Result", "WriteResultsToObjects", "Writing to objects completed.", stopwatch.ElapsedMilliseconds);

            }
            catch (Exception ex)
            {
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "Result", "ORUMessage", "Error encountered while processing accession.", sAccessionNum, ex.Message);

                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Result", "ORUMessage", "Unhandled exception while processing ORU message.", sAccessionNum);

            }
            finally
            {
                cReport = null;
            }
        }


        public bool SuppressTestCode(string testCode, string acctNbr)
        {
            if (_appSettings.SupressTestCodes)
            {
                if (_appSettings.SupressTestCodes_AcctNbrByPass != null && _appSettings.SupressTestCodes_AcctNbrByPass.Contains(acctNbr))
                    return false;

                testCode = testCode.TrimStart('0').Trim();

                if (_appSettings.SupressTestCodes_AllowCodes != null && _appSettings.SupressTestCodes_AllowCodes.Contains(testCode))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
        private bool IsAllowedAccessionsForTNPOBR(string sAccessionNum)
        {
            if (_appSettings.Inbound_AccessionPrefixTNPOBR != null)
            {
                foreach (string prefix in _appSettings.Inbound_AccessionPrefixTNPOBR)
                {
                    if (sAccessionNum.StartsWith(prefix))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void HandleResultsInOBR(DI.Interface.Report rpt, string sAccessionNum)
        {
            if (rpt.ReportResult.ToUpper() == "X")
            {
                LIS.Report report = OrderManager.FetchReport(sAccessionNum);
                if (report is not null)
                {
                    foreach (LIS.ReportAnalyte a in report.Analytes.List)
                    {
                        if (a.ContainsOrderCode(rpt.OrderedCode) && (a.ResultValue != "" || a.Analyte.IsRequired))
                        {
                            a.ResultValue = "TNP";
                            a.AddComment(rpt.OBRComment);
                        }
                    }
                    foreach (ReportAnalytePanel p in report.AnalytePanels.List)
                    {
                        foreach (LIS.ReportAnalyte a in p.Analytes.List)
                        {
                            if (a.ContainsOrderCode(rpt.OrderedCode) && (a.ResultValue != "" || a.Analyte.IsRequired))
                            {
                                a.ResultValue = "TNP";
                                p.AddComment(rpt.OBRComment);
                            }
                        }
                    }
                    report.Save();
                }
            }
        }
        private async Task WriteResultsToObjects(List<DI.Interface.Report> cReportList, string sAccessionNum)
        {

            bool clearAllSet = false; // Determine if clear set for this incoming accession.
            bool IsAnalyteResultValueSeeNote = false;

            //Logger.DebugFormat("WriteResultsToObjects(): AccessionNbr- '{0}'", sAccessionNum);
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Result", "WriteResultsToObjects", "WriteResultsToObjects called.", sAccessionNum);


            // First create an OrderManger.Results object to store your updates
            var u = new OrderManager.Results(sAccessionNum);
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "WriteResultsToObjects", "Created results.");


            string acctNbr = "";
            if (_appSettings.SupressTestCodes)
            {
                var fetchOrderWatch = Stopwatch.StartNew();

                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                    "Result",
                    "FetchOrder",
                    "Fetching order from database.",
                    sAccessionNum);

                LIS.Order localOrder = OrderManager.FetchOrder(sAccessionNum);

                fetchOrderWatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedTime} ms",
                    "Result",
                    "FetchOrder",
                    "Order fetch completed.",
                    sAccessionNum,
                    $"{fetchOrderWatch.ElapsedMilliseconds} ms"); if (!(localOrder == null))
                {
                    acctNbr = localOrder.AccountNumber;
                }
            }
            // If My.Settings.ReferenceLabId = 0
            // Dim datedifference As Integer= DateDiff(DateInterval.Day, localOrder.DateOfService, DateTime.Now.Date)
            // If (datedifference > Settings.DiffDateOfService ) Then
            // Logger.Debug(String.Concat("Can not process result for the Accession " + sAccessionNum + " as DateOfService is Greater Than " + _settings.DiffDateOfService + " Days."))
            // Return
            // End If
            // End If
            int _refLabId = 0;
            int.TryParse(_appSettings.ReferenceLabId, out _refLabId);

            //Logger.DebugFormat("ReportList.Count={0}", cReportList.Count);
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ReportCount: {ReportCount}", "Result", "WriteResultsToObjects", "Report list count.", cReportList.Count);

            RefLabs TMRefLabs = Bioreference.Common.TestMaster.RefLabs.Fetch();
            // Then add values like so:
            foreach (DI.Interface.Report rpt in cReportList)
            {
                string sTestCode = rpt.TestCode;

                //if (!string.IsNullOrEmpty(sTestCode) && Utilities.SuppressTestCode(sTestCode, acctNbr))
                if (!string.IsNullOrEmpty(sTestCode) && SuppressTestCode(sTestCode, acctNbr))
                {
                    //WarningMessages.Add(string.Format("Inbound result supressed for accession# {0} testcode {1}", sAccessionNum, sTestCode));
                    _logger.LogWarning(string.Format("Inbound result supressed for accession# {0} testcode {1}", sAccessionNum, sTestCode));
                    continue;
                }

                OrderManager.Result r;

                //Logger.DebugFormat("rpt.AnalyteList.Count={0}", rpt.AnalyteList.Count);
                _logger.LogDebug("rpt.AnalyteList.Count={0}", rpt.AnalyteList.Count);

                // Handle results in OBR, currently being used for TNP may be can use to update other results as well.
                HandleResultsInOBR(rpt, sAccessionNum);

                // 'This occurs when there is OBR with no OBX - still need to send to b2
                if (rpt.AnalyteList.Count == 0 && !rpt.IsOBRProcessing)
                {

                    r = new OrderManager.Result("", "");
                    r.PanelCode = sTestCode;
                    r.LabReportStatus = rpt.LabReportStatus;
                    if (_refLabId != 0)
                        r.ReferenceLabId = _refLabId;
                    u.List.Add(r);
                }

                else
                {

                    foreach (DI.Interface.ReportAnalyte analyte in rpt.AnalyteList)
                    {
                        // Dim r As OrderManager.Result

                        //if (string.IsNullOrEmpty(sTestCode) && Utilities.SuppressTestCode(analyte.Code, acctNbr))
                        if (string.IsNullOrEmpty(sTestCode) && SuppressTestCode(analyte.Code, acctNbr))
                        {
                            //WarningMessages.Add(string.Format("Inbound result supressed for accession# {0} testcode {1}", sAccessionNum, analyte.Code));
                            _logger.LogWarning(string.Format("Inbound result supressed for accession# {0} testcode {1}", sAccessionNum, analyte.Code));
                            continue;
                        }

                        if (_appSettings.Inbound_BypassAnalyte != null && _appSettings.Inbound_BypassAnalyte.Contains(analyte.Code.ToUpper()))
                        {
                            //WarningMessages.Add(string.Format("Inbound result byapassed for accession# {0} testcode {1}", sAccessionNum, analyte.Code));
                            _logger.LogWarning(string.Format("Inbound result byapassed for accession# {0} testcode {1}", sAccessionNum, analyte.Code));
                            continue;
                        }

                        // ****************************************************************
                        // For dummy OBX record used to send Notes for OBR (Panels).
                        if (analyte.Value == IDENTIFER_PANELCMT)
                        {
                            r = new OrderManager.Result("", analyte.Value);
                            if (_appSettings.Inbound_ClearExistingData)
                            {
                                r.ResetAll = true;
                            }
                            r.PanelCode = analyte.Code;
                            if (analyte.AnalyteComment != string.Empty)
                            {
                                // Dim noteArray As String() = analyte.AnalyteComment.Split("|")
                                string[] noteArray = new string[] { analyte.AnalyteComment };
                                r.Comments = noteArray;
                            }

                            if (_refLabId != 0)
                            {
                                r.ReferenceLabId = _refLabId;
                            }

                            u.List.Add(r);
                            continue;
                        }
                        // ****************************************************************

                        // NA 1/8/14 
                        // check if results are to be bypassed based on incoming value.
                        if (_appSettings.Inbound_BypassResultValues != null && _appSettings.Inbound_BypassResultValues.Contains(analyte.Value.ToUpper()))
                        {
                            //WarningMessages.Add(string.Format("Inbound result bypassed for accession# {0} testcode {1} with value {2}", sAccessionNum, analyte.Code, analyte.Value));
                            _logger.LogWarning(string.Format("Inbound result bypassed for accession# {0} testcode {1} with value {2}", sAccessionNum, analyte.Code, analyte.Value));
                            continue;
                        }

                        // Bypass Blank Results with Given Status

                        if (_appSettings.Inbound_FlagBlankResultsForStatus != null && _appSettings.Inbound_FlagBlankResultsForStatus.Any() && analyte.Value.Trim() == string.Empty && _appSettings.Inbound_FlagBlankResultsForStatus.Contains(analyte.Status.ToUpper()))
                        {
                            // WarningMessages.Add(string.Format("Inbound result bypassed for accession# {0} testcode {1} with value {2} and status {3}", sAccessionNum, analyte.Code, analyte.Value, analyte.Status));
                            _logger.LogWarning(string.Format("Inbound result bypassed for accession# {0} testcode {1} with value {2} and status {3}", sAccessionNum, analyte.Code, analyte.Value, analyte.Status));
                            continue;
                        }

                        // nart here:
                        // if the status = X , we need to TNP the result value.

                        if (_appSettings.Inbound_TNPResultsForStatus != null && _appSettings.Inbound_TNPResultsForStatus.Any() && !string.IsNullOrWhiteSpace(analyte.Status) && _appSettings.Inbound_TNPResultsForStatus.Contains(analyte.Status.ToUpper()))
                        {
                            //WarningMessages.Add(string.Format("Inbound result value changed to TNP for accession# {0} testcode {1} with value {2} and status {3}", sAccessionNum, analyte.Code, analyte.Value, analyte.Status));
                            _logger.LogWarning(string.Format("Inbound result value changed to TNP for accession# {0} testcode {1} with value {2} and status {3}", sAccessionNum, analyte.Code, analyte.Value, analyte.Status));
                            analyte.Value = "TNP";
                        }

                        if (analyte.Alerts == string.Empty)
                        {
                            r = new OrderManager.Result(analyte.Code, analyte.Value);
                        }
                        else
                        {
                            string[] alertArray = analyte.Alerts.Split(",");
                            r = new OrderManager.Result(analyte.Code, analyte.Value, alertArray);
                        }
                        if (_appSettings.Inbound_ClearExistingData)
                        {
                            r.ResetAll = true;
                        }
                        //Need to Disscuss
                        //if (ForceUpdate == true)
                        //    r.ForceUpdate = true;
                        //else
                        r.ForceUpdate = false;

                        // 'AG - 8/28/2008 Only for RefLab specific channels
                        // If from referencence lab, we build a unique testcode based on padding and prefix.
                        // AG - 1/14/2009 - Truncate codes to 6 digits - limitation of reporting.
                        if (_refLabId != 0)
                        {
                            r.AnalyteCodeAlternate = string.Format("{0}{1}", _appSettings.ReferencePrefix, analyte.Code.PadLeft(_appSettings.TestCodePadding, '0'));
                            if (r.AnalyteCodeAlternate.Length > 6)
                                r.AnalyteCodeAlternate = r.AnalyteCodeAlternate.Substring(0, 6);
                            r.PanelCode = rpt.TestCode;
                            r.PanelName = rpt.TestDescription;
                            r.PanelCodeAlternate = string.Format("{0}{1}", _appSettings.ReferencePrefix, rpt.TestCode.PadLeft(_appSettings.TestCodePadding, '0'));
                            if (r.PanelCodeAlternate.Length > 6)
                                r.PanelCodeAlternate = r.PanelCodeAlternate.Substring(0, 6);
                        }

                        //Logger.Debug("Updating result.");
                        _logger.LogDebug("Updating result.");

                        // ' vcc: 11/2/2016 
                        // ' If analyte is sendout override in B2 the performingLocation with the LabId defined for that channel in app.settings
                        if (!string.IsNullOrEmpty(_appSettings.SendOutLabId) && !_appSettings.SendOutLabId.Equals("0"))
                        {
                            r.PerformingFacility = _appSettings.SendOutLabId;
                        }
                        else
                        {
                            r.PerformingFacility = analyte.PerformingFacility;
                        }


                        r.TechUser = analyte.TechUser;
                        r.ReleaseUser = analyte.ReleaseUser;
                        r.AnalyteName = analyte.Description;
                        r.ReferenceRange = analyte.ReferenceRange;
                        r.Units = analyte.Units;
                        r.SpecimenRackId = analyte.Rack;
                        r.SpecimenRackPosition = analyte.RackPosition;
                        r.SpecimenRackSequence = analyte.MachineSequence;
                        r.InstrumentId = analyte.InstrumentId;
                        if (analyte.Flag != string.Empty)
                            r.FlagValue = analyte.Flag;
                        if (analyte.AnalyteComment != string.Empty)
                        {
                            // Dim noteArray As String() = analyte.AnalyteComment.Split("|")
                            string[] noteArray = new string[] { analyte.AnalyteComment };
                            r.Comments = noteArray;
                        }
                        r.IsPreliminaryRelease = rpt.IsPreliminaryRelease;
                        if ((analyte.Status == "P" || analyte.Status == "F") && _inboundSettings.PrelimReleaseTestCodes.Contains(analyte.Code) && _inboundSettings.PrelimReleaseFlag)
                        // if ((analyte.Status == "P" || analyte.Status == "F"))
                        {
                            LIS.ReportAnalyte reportAnalyte;

                            var fetchReportWatch = Stopwatch.StartNew();

                            _logger.LogDebug(
                                "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                                "Result",
                                "FetchReport.Start",
                                "Fetching report with state.",
                                u.AccessionNumber);

                            LIS.Report report = OrderManager.FetchReportWithState(u.AccessionNumber);

                            fetchReportWatch.Stop();

                            _logger.LogInformation(
                                "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedTime} ms",
                                "Result",
                                "FetchReport.End",
                                "Report fetch completed.",
                                u.AccessionNumber,
                                $"{fetchReportWatch.ElapsedMilliseconds} ms");
                            string panelCode = string.Empty;
                            ReportAnalytePanel analytePanel = default;
                            foreach (ReportAnalytePanel panel in report.AnalytePanels.List)
                            {
                                reportAnalyte = panel.Analytes.FindFirst(r.AnalyteCode, r.ReferenceLabId);
                                if (reportAnalyte is not null)
                                {
                                    panelCode = panel.PanelCode;
                                    analytePanel = panel;
                                }
                            }
                            if (_inboundSettings.PrelimReleaseTestCodes.Contains(panelCode))
                            {
                                r.AutoReleaseResult = true;
                                if (analyte.Status == "P")
                                {
                                    r.PrelimReleaseTestCodes = _inboundSettings.TestCodeListForPrelimRelease.ToList();
                                    if (analytePanel is not null)
                                    {
                                        analytePanel.MarkedAsPreliminary = true;
                                    }
                                    r.IsPreliminaryRelease = true;
                                }
                                if (analyte.Status == "F")
                                {
                                    r.SetDeleteComment(true);
                                    if (analytePanel is not null)
                                    {
                                        analytePanel.MarkedAsPreliminary = false;
                                    }
                                }

                                var reportSaveWatch = Stopwatch.StartNew();

                                _logger.LogInformation(
                                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                                    "Result",
                                    "DB.ReportSave.Start",
                                    "Saving report changes.",
                                    u.AccessionNumber);

                                report.Save();

                                reportSaveWatch.Stop();

                                _logger.LogInformation(
                                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedTime} ms",
                                    "Result",
                                    "DB.ReportSave.End",
                                    "Report saved successfully.",
                                    u.AccessionNumber,
                                    $"{reportSaveWatch.ElapsedMilliseconds} ms");
                            }
                        }

                        if (_inboundSettings.IsTNPFromComment && analyte.Value.ToLower() == "see note" && analyte.AnalyteComment.ToLower().Contains("test not performed"))
                        {
                            //ErrorMessages.Add(string.Format("For Accession {0}, B2 found {1} as TNP", ": ", sAccessionNum, analyte.Code));
                            _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "Result", "B2_TNP", "B2 flagged analyte as TNP.", sAccessionNum, analyte.Code);
                            IsAnalyteResultValueSeeNote = true;
                        }

                        //Logger.Debug("Updated Result.");
                        _logger.LogDebug("Updated Result.");

                        // 'REFERENCE LAB ID.  If the ReferenceLabId is set within the config file
                        // 'that means this channel is set up specifically for that Lab.  You SHOULD NOT
                        // 'use the ReferenceLabId passed in from the message
                        // 'If not in config, use id from message.
                        if (_refLabId != 0)
                        {
                            _logger.LogDebug($"Reference Lab Id is configured to process. Entering processing block. RefLabId = {_refLabId}");
                            r.ReferenceLabId = _refLabId;
                            if (_inboundSettings.ProcessReferenceFacility)
                            {
                                _logger.LogDebug($"ProcessReferenceFacility setting is TRUE");
                                if (!string.IsNullOrEmpty(analyte.ReferenceLabNumber))
                                {
                                    RefLab rl = null;
                                    rl = TMRefLabs.FindByExtRefLabCode(analyte.ReferenceLabNumber); // OBX-15

                                    if (rl != null)
                                    {
                                        _logger.LogDebug($"TM Reference lab found. RefLabNo={rl.RefLabNo}, PerformingFacility = {r.PerformingFacility}");

                                        r.PerformingFacility = rl.RefLabNo.ToString(); // Modify this to show on LOD
                                        r.RefLabPerformingFacilityId = -1; // Set PerformingFacility B2 to Reporting flow
                                    }
                                    else
                                    {
                                        // B2 Implementation only if TM is not available
                                        _logger.LogDebug("TM reference lab NOT available. Executing B2 fallback implementation.");
                                        _logger.LogWarning(string.Format("Accession # {0}, OBR ordering code {1} : Performing facility(OBX-15) {2} is not setup in TM(Ref Lab > Ext Ref Lab Code).", sAccessionNum, rpt.TestCode, analyte.ReferenceLabNumber));

                                        // TODO: Once 'Ext Ref Lab Code' data is setup in TM, below code can be removed, and above WarningMessages must change to ErrorMessages.                                        
                                        RefLabPerformingFacility refLab = RefLabPerformingFacilityList.Filter(RefLabPerformingFacilityList.Fetch(r.ReferenceLabId).List, analyte.ReferenceLabNumber);

                                        if (refLab == null)
                                        {
                                            _logger.LogDebug($"No matching RefLabPerformingFacility found in B2. ReferenceLabId={r.ReferenceLabId}, AnalyteRefLabNo={analyte.ReferenceLabNumber}");
                                            _logger.LogError(string.Format("Accession # {0}, OBR ordering code {1} : Performing facility(OBX-15) {2} is neither setup in TM nor B2", sAccessionNum, rpt.TestCode, analyte.ReferenceLabNumber));
                                        }
                                        else
                                        {
                                            _logger.LogDebug($"RefLabPerformingFacility found in B2. ReferenceLabId={r.ReferenceLabId}, FacilityId={refLab.FacilityId}, AnalyteRefLabNo={analyte.ReferenceLabNumber}");
                                            r.RefLabPerformingFacilityId = refLab.FacilityId;
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogDebug("Analyte.ReferenceLabNumber is NULL or EMPTY.");
                                }
                            }
                            else
                            {
                                _logger.LogDebug("ProcessReferenceFacility setting is FALSE or not Set");
                            }
                        }
                        else
                        {
                            _logger.LogDebug("RefLabId is zero, not configured to this channel. Skipping reference lab processing.");
                            if (! String.IsNullOrEmpty(analyte.ReferenceLabNumber))
                            {
                                _logger.LogDebug($"Updated ReferenceLabId from Analyte.ReferenceLabNumber. Value = {analyte.ReferenceLabNumber}");
                                r.ReferenceLabId = Convert.ToInt32(analyte.ReferenceLabNumber);
                            }
                        }

                        //Logger.Debug("Check Testcodes for auto release");
                        _logger.LogDebug("Check Testcodes for auto release");

                        // AG-3/16/09- allow autoreleasing at the test level.
                        if (_inboundSettings.TestCodesForAutoRelease.Contains(r.AnalyteCode))
                        {
                            r.AutoReleaseResult = true;
                        }
                        // *********************************************************************

                        //Logger.Debug("Done Testcodes for auto release");
                        _logger.LogDebug("Done Testcodes for auto release");

                        if (Information.IsDate(analyte.ReportDate))
                            r.ResultDate = Convert.ToDateTime(analyte.ReportDate);
                        if (!IsAnalyteResultValueSeeNote)
                        {
                            u.List.Add(r);
                        }

                        // 'Logger.Debug(analyte.Code)
                        // 'Logger.Debug(String.Concat("Result - Units: ", r.Units, " RefRange: ", r.ReferenceRange))
                    }

                }
            }

            //Logger.Debug("Updating results...");
            _logger.LogDebug("Updating results...");

            OrderManager.Statuses ss = default;
            try
            {
                // Then pass that into the UpdateResults function
                // AG - 8/20/2008 - pass in setting for dynamically adding tests and for automatically releasing results.
                var updateResultsWatch = Stopwatch.StartNew();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                    "Result",
                    "UpdateResults.Start",
                    "Updating results in database.",
                    sAccessionNum);

                ss = OrderManager.UpdateResults(
                    u,
                    _appSettings.Inbound_DynamicAddTests,
                    _appSettings.Inbound_AutoReleaseResults,
                    HL7String);

                updateResultsWatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedTime} ms",
                    "Result",
                    "UpdateResults.End",
                    "Results update completed.",
                    sAccessionNum,
                    $"{updateResultsWatch.ElapsedMilliseconds} ms");
            }
            // ss = OrderManager.UpdateResults(u, My.Settings.DynamicAddTests)
            catch (Exception ex)
            {
                //ErrorMessages.Add($"Failed on OrderManager.UpdateResults function call.{Constants.vbCrLf}{ex}");
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "UpdateResults", "Failed on OrderManager.UpdateResults function call.");

            }

            //Logger.Debug("Results updated.");
            _logger.LogDebug("Results updated.");


            //Logger.DebugFormat("ss.IsError={0} - ", ss.IsError);
            _logger.LogDebug("ss.IsError={0} - ", ss.IsError);

            // A Statuses object is returned, check the IsError property to see if there were any issues:
            if (ss.IsError)
            {

                // If an error occured, you can check them through the List of Status Objects.
                foreach (OrderManager.Status x in ss.List)
                {
                    if (x.StatusType == OrderManager.StatusType.Failure)
                    {
                        // AddErrorLogEvent(x.Message)
                        // ErrorMessages.Add(string.Concat(u.AccessionNumber, ": ", x.Message));
                        _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "Result", "UpdateResults", "Error updating results.", u.AccessionNumber, x.Message);

                    }
                }
            }

            else
            {
                // try to write the AuditRecords returned 
                // if we can't for some reason, log into Iguana as warnings.

                //Logger.DebugFormat("Audit List Count returned: {0}", ss.AuditList.Count);
                _logger.LogDebug("Audit List Count returned: {0}", ss.AuditList.Count);
                //Logger.DebugFormat("SendAuditToAuditChannel: {0}", _settingsProvider.GetConfiguration().GetBool("Bioreference.Processor:Inbound_SendAuditToAuditChannel"));
                _logger.LogDebug("SendAuditToAuditChannel: {0}", _appSettings.Inbound_SendAuditToAuditChannel);

                if (ss.AuditList.Count > 0 && _appSettings.Inbound_SendAuditToAuditChannel)
                {
                    string sAudit = ConvertToSingleString(ss.AuditList);
                    try
                    {
                        //Logger.Debug("calling MessageToAuditChannel");
                        _logger.LogDebug("calling MessageToAuditChannel");
                        //MessageToAuditChannel(sAudit);
                        await MessageToAuditKafka(sAudit);
                    }
                    catch (Exception ex)
                    {
                        //Logger.ErrorFormat("Unable to write audit: {0}", ex);
                        _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "AuditWrite", "Unable to write audit.");

                        //WarningMessages.Add($"Unable to write Audit for Accession: {u.AccessionNumber}{Constants.vbCrLf}{sAudit}{Constants.vbCrLf}{ex}");
                        _logger.LogWarning($"Unable to write Audit for Accession: {u.AccessionNumber}{Constants.vbCrLf}{sAudit}{Constants.vbCrLf}{ex}");
                    }
                }

                // 'WE MAY WANT TO MOVE THIS FUNCTIONALITY TO MAIN MESSAGE RECEIVED

                bool logAsError = false;
                var localWarnings = new List<string>();
                foreach (OrderManager.Status s in ss.List)
                {
                    if (s.StatusType == OrderManager.StatusType.Warning)
                    {
                        localWarnings.Add(s.Message);
                        //Logger.DebugFormat("Message: {0}", s.Message);
                        _logger.LogDebug("Message: {0}", s.Message);
                    }
                }

                if (localWarnings.Count > 0)
                {
                    if (!_appSettings.Inbound_ResultThrowErrorForWarning)
                    {
                        // see if we want to log warnings or not.
                        if (_appSettings.Inbound_LogWarnings)
                        {
                            // All values were successfully saved.
                            // WarningMessages.AddRange(localWarnings);
                        }
                    }
                    else
                    {
                        //ErrorMessages.AddRange(localWarnings);
                        return;
                    }
                }
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Time: {Time}", "Result", "SaveResults", "Successful save of results.", sAccessionNum, DateTime.Now.ToString("HH:mm:ss.fff"));

            }

            //Logger.Debug("Write Results out!");
            _logger.LogDebug("Write Results out!");

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

            //Logger.DebugFormat("Writing Audit File: {0}", fileName);
            _logger.LogDebug("Writing Audit File: {0}", fileName);
            using (StreamWriter sw = new StreamWriter(fileName, true))
            {
                sw.WriteLine(sMessage);
            }
            //Logger.Debug("Writing Audit File Done.");
            _logger.LogDebug("Writing Audit File Done.");
        }

        private string ConvertToSingleString(List<string> lst)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string s in lst)
                sb.Append(s + Constants.vbCrLf);
            return sb.ToString();
        }
        protected Gender SexToGender(string sSex)
        {
            if (sSex == "M")
            {
                return Gender.Male;
            }
            else if (sSex == "F")
            {
                return Gender.Female;
            }
            else
            {
                return Gender.Unknown;
            }
        }
        private DateTime ParseDOS(string s)
        {
            try
            {
                if (s.Length != 14)
                    return new DateTime(1900, 1, 1);

                string year = s.Substring(0, 4);
                string month = s.Substring(4, 2);
                string day = s.Substring(6, 2);
                string hour = s.Substring(8, 2);
                string minute = s.Substring(10, 2);
                string second = s.Substring(12, 2);
                string t = $"{month}/{day}/{year} {hour}:{minute}:{second}";

                if (DateTime.TryParse(t, out DateTime d))
                {
                    return d;
                }
                return new DateTime(1900, 1, 1);
            }
            catch (Exception)
            {
                return new DateTime(1900, 1, 1);
            }
        }
        private bool VerifyIfBypassVertexNotification(LIS.ReportAnalyte pAnalyte, LIS.Report pReport)
        {
            NoBillCode.NoBillValidationType noBillValidation = double.TryParse(pAnalyte.ResultValue, out _)
                ? NoBillCode.NoBillValidationType.Numeric
                : NoBillCode.NoBillValidationType.Text;

            // find matching nobill codes based on testcode and validation type
            string panelCode = string.Empty;
            if (pAnalyte.Parent is ReportAnalytePanel panel && panel != null)
            {
                panelCode = panel.PanelCode;
            }

            // find matching nobill codes based on testcode and validation type
            List<NoBillCode> noBills;
            if (!string.IsNullOrEmpty(panelCode))
            {
                noBills = _noBillCodes.FindAll(pAnalyte.Code, panelCode, (int)noBillValidation);
                noBills.AddRange(_noBillCodes.FindAll(pAnalyte.Code, panelCode, (int)NoBillCode.NoBillValidationType.TestExists));
            }
            else
            {
                noBills = _noBillCodes.FindAll(pAnalyte.Code, noBillValidation);
                noBills.AddRange(_noBillCodes.FindAll(pAnalyte.Code, NoBillCode.NoBillValidationType.TestExists));
            }

            // check each nobill 
            foreach (NoBillCode noBill in noBills)
            {
                if (noBill.PerformingFacilities.Contains(pAnalyte.PerformingFacility))
                {
                    // check each NoBill based on numeric or text
                    if (noBill.ValidationTypeId == NoBillCode.NoBillValidationType.Numeric && noBillValidation == NoBillCode.NoBillValidationType.Numeric)
                    {
                        double value = Convert.ToDouble(pAnalyte.ResultValue);
                        if (value < Convert.ToDouble(noBill.ThresholdLowValue) || value > Convert.ToDouble(noBill.ThresholdHighValue))
                        {
                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Code: {Code}", "Result", "NoBill_Skip", "NoBill skip analyte.", pAnalyte.Code);

                            return true;
                        }
                    }
                    else if (noBill.ValidationTypeId == NoBillCode.NoBillValidationType.Text && noBillValidation == NoBillCode.NoBillValidationType.Text)
                    {
                        // check text value against list of text ranges                    
                        if (noBill.TextRanges.Contains(pAnalyte.ResultValue))
                        {
                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Code: {Code}", "Result", "NoBill_Skip", "NoBill skip analyte.", pAnalyte.Code);

                            return true;
                        }
                    }
                    else if (noBill.ValidationTypeId == NoBillCode.NoBillValidationType.TestExists)
                    {
                        string[] tests = noBill.TestExists.Split(',');
                        foreach (string test in tests)
                        {
                            if (pReport.AnalyteOrPanelExists(test))
                            {
                                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Code: {Code}", "Result", "NoBill_Skip", "NoBill skip analyte.", pAnalyte.Code);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private string FindReflabCode(int refLabId)
        {
            if (_refLabs == null)
            {
                _refLabs = RefLabs.Fetch();
            }

            foreach (RefLab rLab in _refLabs.List)
            {
                if (refLabId == rLab.RefLabNo)
                {
                    return rLab.RefLabNo.ToString(); // rLab.MNE
                }
            }

            return "";
        }
        private string FindFirstPerfomingFacility(ReportAnalytePanel panel)
        {
            foreach (Bioreference.LIS.ReportAnalyte analyte in panel.Analytes.List)
            {
                if (analyte.RefLabPerformingFacilityId == -1)
                {
                    return analyte.PerformingFacility;
                }
            }

            return string.Empty;
        }

        private ResultStatusType GetResultStatus(string resultValue)
        {
            if (_appSettings.Inbound_TNPEquivalent != null && _appSettings.Inbound_TNPEquivalent.Contains(resultValue))
            {
                _logger.LogDebug("Found TNP Equivalent from settings.");
                return ResultStatusType.TNP;
            }
            else
            {
                switch (resultValue.ToUpper())
                {
                    // "TNP", "ATP" and "ALT. TEST PERFORMED" can be moved to settings "Inbound_TNPEquivalent" and above if will cover this case as well.
                    // Preferably do it after moving setting to db.
                    case "TNP":
                    case "ATP":
                    case "ALT. TEST PERFORMED":
                        _logger.LogDebug("Found 'TNP', 'ATP', 'ALT. TEST PERFORMED'");
                        return ResultStatusType.TNP;
                    case "SEE BELOW":
                        _logger.LogDebug("Found 'SEE BELOW'");
                        return ResultStatusType.SeeBelow;
                    case "QNS":
                        _logger.LogDebug("Found 'QNS'");
                        return ResultStatusType.QNS;
                }
            }

            return ResultStatusType.None;
        }
        private string GetFirstPerformingFacility(Bioreference.LIS.ReportAnalytePanel ap)
        {
            string rValue = "";
            foreach (LIS.ReportAnalyte obj in ap.Analytes.List)
            {
                if (!string.IsNullOrEmpty(obj.PerformingFacility))
                {
                    rValue = obj.PerformingFacility;
                    break;
                }
            }
            return rValue;
        }
        private string MessageToVertex(ReportStatus reportStatus, DateTime dateOfService)
        {
            // Do not send to Vertex
            if (reportStatus.IsToFollow)
            {
                return "";
            }

            // Get message
            string msg = reportStatus.GetMessage();
            _logger.LogDebug("reportStatus.GetMessage='{0}'", msg);

            // Archive
            _logger.LogDebug("Archive to database...");
            string timeStamp = "";
            try
            {
                timeStamp = ChannelCommon.ArchiveStatus(msg, _settingsProvider.GetConnectionString("Bioreference.ConnString_ArchiveStatus"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }
            if (string.IsNullOrEmpty(timeStamp))
            {
                timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                _logger.LogWarning("Failed to archive Vertex message to database.");
            }

            _logger.LogDebug("Archive to database done.");

            OutboundMessageCHM obCHM = new OutboundMessageCHM(reportStatus.AccessionNumber);
            obCHM.Message = msg;
            obCHM.Save();
            _logger.LogDebug("Writing to table 'OutboundMessageCHM' done, msg='{0}'", msg);

            return msg;
        }

    }
}

