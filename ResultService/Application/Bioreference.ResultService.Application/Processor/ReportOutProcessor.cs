using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.ReportOut;
using Bioreference.ResultService.DI.Interface;
using Bioreference.ResultService.Common.Helpers;
using HL7Parser.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using System.Data;

namespace Bioreference.ResultService.Application.Processor
{
    public class ReportOutProcessor : IReportOutProcessor
    {
        private ILogger<ReportOutProcessor> logger;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReportOut appSettings;
        private readonly IPayloadSenderApiClient payloadSenderApiClient;
        private ReportOutSettings settings;
        private string connectionString = string.Empty;
        private const int NoBillValidationTypeNumeric = 1;
        private const int NoBillValidationTypeText = 2;
        private Reports.processDataType processDataType;
        private Bioreference.ResultService.DI.Interface.ExcludedStrings _oSuppress = default;
        private static object outboundStatusMessagePathIndex = 0;
        private bool canceled = false; // 'If this is set to true, we still need to process the Report, but not send a message to Reporting
        private bool sendToFollow = false; // '''indicates if we have to send to Reporting "toFollow"
        private bool _hasAllergen = false;
        private Bioreference.ResultService.DI.Interface.Report _rptIge = default;
        private NoBillCodes noBillCodes;
        private List<string> _aeList = new List<string>();
        private const string AeStringValue = "AE";
        private string testDescriptionTitle;
        private string resultTitle;
        private string flagTitle;
        private string referenceRangeTitle;
        private string unitsTitle;
        private int pidCounter;
        private string accessionNumber;

        public ReportOutProcessor(ILogger<ReportOutProcessor> logger, IPayloadSenderApiClient payloadSenderApiClient, ISettingService settingsProvider, IOptions<AppSettingsReportOut> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
            this.payloadSenderApiClient = payloadSenderApiClient;
        }

        public async Task OnReportOutMessage(ReportOut msg)
        {
            try
            {
                ActivityHelper.SetAccessionLogKey(msg.AccessionNumber);
                logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    msg.AccessionNumber,
                    DateTime.UtcNow
                );

                string connection = settingsProvider.GetConnectionString("Bioreference.LIS");
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Received", "Received ReportOut message.", msg.AccessionNumber);


                try
                {
                    connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");
                    settings = await settingsProvider.FetchSetting<ReportOutSettings>(connectionString, "ResultOutEngine", "");
                    testDescriptionTitle = settings.TestDescriptionTitle.Replace(".", " ");
                    resultTitle = settings.ResultTitle.Replace(".", " ");
                    flagTitle = settings.FlagTitle.Replace(".", " ");
                    referenceRangeTitle = settings.ReferenceRangeTitle.Replace(".", " ");
                    unitsTitle = settings.UnitsTitle.Replace(".", " ");
                    if (settings.ResultOut_SuppressNTEs)
                    {
                        _oSuppress = new Bioreference.ResultService.DI.Interface.ExcludedStrings();
                        string success = _oSuppress.CreateList("SuppressNtes");
                        if (success != "SUCCESS")
                            logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; SuccessFlag: {Success}", "ReportOut", "SuppressedNotesCreation", "Could not create suppressed notes list.", msg.AccessionNumber, success);
                    }
                    // mzia 3/2/2010: Incremental PIDs
                    pidCounter = settings.ResultOut_FetchCount - (settings.ResultOut_FetchCount - 1);
                    string message = ProcessResult(msg);
                    if (!await payloadSenderApiClient.SendPayloadAsync(message, "REPORTING"))
                        throw new Exception("Error sending message to Iguana");
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Outbound", "Message sent to Iguana.", msg.AccessionNumber);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex,"Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "ReportOut", "OnReportOutMessageInner", "Exception occurred while processing ReportOut message (Inner).", msg.AccessionNumber, ex.Message);
                }
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Processed", "Processed ReportOut message.", msg.AccessionNumber);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "ReportOut", "OnReportOutMessageOuter", "Exception occurred while processing ReportOut message (Outer).", msg.AccessionNumber, ex.Message);
                throw;
            }
        }

        public string ProcessResult(ReportOut report)
        {
            string _message = string.Empty;
            canceled = false;
            int accessionsProcessed = 0;
            string sMessageId = DateTime.Now.ToString("yyyyMMddHHmmss");
            string responseMessage = "";
            bool isReportable = false;
            int msgCounter = 0;
            long reportId = 0;

            try
            {
                accessionNumber = report.AccessionNumber;
                noBillCodes = NoBillCodes.Fetch();
                // NA 11/14/13
                _hasAllergen = false;
                accessionsProcessed += 1;
                accessionNumber = report.AccessionNumber;
                isReportable = report.IsReportable;
                reportId = report.ReportId;
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; IsReportable: {IsReportable}", "ReportOut", "Processing", "Processing accession.", accessionNumber, isReportable);
                Bioreference.LIS.Report blReport = Bioreference.LIS.Report.Fetch(reportId);

                if (!(blReport == null) && blReport.ID > 0)
                {
                    // determine if we need to send the follow.
                    sendToFollow = IsToFollowRequired(blReport);

                    // NA: 4/18/14
                    // Do not process COC if it has not been approved by reviewer.  This is accomplished by checking the IsFinal.
                    // We don't permit sending to reporting unitl all tests have been finalized and signed.
                    if (!blReport.IsCOC || blReport.IsFinal() || sendToFollow)
                    {
                        // If (Not blReport.IsCOC) OrElse (blReport.IsFinal) Then
                        try
                        {
                            // 'MESSAGE*******************************************************************
                            string sMessage = GenerateMessage(blReport, isReportable);
                            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; GeneratedMessage: {GeneratedMessage}", "ReportOut", "ProcessResultGenerateMessage", "Generated message during ProcessResult.", accessionNumber, sMessage);
                            if (!string.IsNullOrEmpty(sMessage))
                            {
                                ProcessReport(blReport, isReportable);
                            }
                            else
                            {
                                logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "ProcessResult", "Empty message generated.", accessionNumber);
                            }
                            // '**************************************************************************

                            // Check and save.
                            if (blReport.IsValid)
                            {

                                // 'Report can still be saved to reset HasReleased column
                                if (!blReport.IsDirty)
                                    blReport.FlagDirty();
                                if (_hasAllergen)
                                {
                                    blReport.SetOuboundQueueAccession(accessionNumber);
                                }
                                blReport.Save();

                                if (canceled && string.IsNullOrEmpty(sMessage))
                                {
                                    logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "ProcessResult", "Outbound results contain only cancelled tests. No message sent to Reporting.", accessionNumber);
                                }
                                else
                                {
                                    // mzia 3/17/2010: Remove all MSH segments except first one
                                    if (msgCounter > 0)
                                    {
                                        sMessage = sMessage.Remove(0, sMessage.IndexOf('\r') + 1);
                                    }
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; SMessage: {SMessage}", "ReportOut", "ProcessResult", "Generated sMessage.", accessionNumber, sMessage);
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ResponseMessage: {ResponseMessage}", "ReportOut", "ProcessResult", "Generated responseMessage.", accessionNumber, responseMessage);
                                    responseMessage += sMessage;
                                    msgCounter += 1;
                                    //OutboundMessage = responseMessage;
                                    _message = responseMessage;
                                } // 'sMessage
                            }
                            else
                            {
                                logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; InvalidRules: {InvalidRules}", "ReportOut", "OutboundReportSave", "Outbound report save is invalid.", accessionNumber, blReport.Rules.ToString());
                            }
                        }

                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ChannelId: {ChannelId}; ErrorMessage: {ErrorMessage}", "ReportOut", "OutboundChannelValidation", "Outbound channel ID is invalid.", blReport.AccessionNbr, settings.ResultOut_ChannelId, ex.Message);
                        }
                    }
                    else
                    {
                        logger.LogWarning($"({blReport.AccessionNbr}): Bypassing COC Accession: Some analytes have not been approved for release.");
                    }
                }
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Processing", "Done processing.", accessionNumber);
            }

            catch (Exception ex)
            {
                //ErrorMessages.Add(string.Concat("Outbound Order Execution Failed: ", ex.ToString()));
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "ReportOut", "OutboundOrderExecution", "Outbound order execution failed.", accessionNumber, ex.Message);
            }
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ResultMessage: {ResultMessage}", "ReportOut", "ProcessResult", "ProcessResult message generated.", accessionNumber, _message);
            return _message;

        }

        private bool IsToFollowRequired(Bioreference.LIS.Report blReport)
        {

            if (!settings.ToFollowEnabled)
            {
                return false;
            }

            foreach (Bioreference.LIS.ReportAnalyte a in blReport.Analytes.List)
            {
                if (a.ToFollowSent == false)
                    return true;
            }
            foreach (ReportAnalytePanel p in blReport.AnalytePanels.List)
            {
                if (p.ToFollowSent == false)
                    return true;
            }

            return default;
        }

        private void ProcessReport(Bioreference.LIS.Report blReport, bool isReportable)
        {
            foreach (Bioreference.LIS.ReportAnalyte a in blReport.Analytes.List)
            {
                // '''''''''''''''''''' To Follow '''''''''''''''''''''''''''''''''''''''''''''''
                if (a.ToFollowSent == false)
                {
                    a.ToFollowSent = true;
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                if (a.TransmitStatus == transmitStatusType.Released)
                {
                    // 'If ReferenceRange is empty, there is a potential issue with Location ranges 
                    // 'and should generate an error
                    // 'Only check Reportable
                    if (isReportable && a.Analyte.IsReportable && a.Analyte.ReferenceLabId == 0 && !settings.ResultsOut_TestsNoRefBypass.Contains(a.Code))
                    {
                        if (string.IsNullOrEmpty(a.GetReferenceRange()) && a.GetComplexDropDownList().Length == 0)
                        {
                            a.MarkAsPendingReleased();
                            logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestName: {TestName}; TestCode: {TestCode}; PerformingFacility: {PerformingFacility}",
                                            "ReportOut",
                                            "TestValidation",
                                            "Reference range blank and performing facility provided. Test set back to pending.",
                                            accessionNumber,
                                            a.Analyte.Name,
                                            a.Code,
                                            a.PerformingFacility);
                        }
                    }

                    // 'If canceled allergen, it will not be generated in the message and sent to reporting. Send tnp directly to vertex.
                    if (IsCanceledAllergen(a))
                    {
                        a.GetParentReport().SendMessageToVertex(string.Concat(a.GetParentReport().AccessionNbr + "|", a.Code, "|", DateTime.Now.ToString("M/d/yyyy HH:mm:ss"), "|TNP"));
                        a.MarkAsSentToReporting(); // 'Need to mark as reported first or will throw error
                        a.MarkAsStatusSentToVertex();
                        continue;
                    }

                    if (!isReportable && !a.Analyte.IsReportable)
                    {
                        a.MarkAsReported(); // 'Results are not sent to reporting
                    }
                    else if (isReportable && a.Analyte.IsReportable)
                    {
                        a.MarkAsSentToReporting();
                        if (_aeList.Contains(a.Code))
                        {
                            a.MarkAsStatusSentToVertex();
                        }
                    }
                }
            }
            // And loop through all the panels (ie. URINALYSIS)
            foreach (ReportAnalytePanel p in blReport.AnalytePanels.List)
            {
                // '''''''''''''''''''' To Follow '''''''''''''''''''''''''''''''''''''''''''''''
                if (p.ToFollowSent == false)
                {
                    // Panel
                    p.ToFollowSent = true;
                    // Analytes
                    foreach (Bioreference.LIS.ReportAnalyte a in p.Analytes.List)
                    {
                        if (a.ToFollowSent == false)
                        {
                            a.ToFollowSent = true;
                        }
                    }
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                if (p.TransmitStatus == transmitStatusType.Released)
                {
                    // 'If canceled allergen, it will not be generated in the message and sent to reporting. Send tnp directly to vertex.
                    if (HasCanceledAllergen(p))
                    {
                        p.Parent.SendMessageToVertex(string.Concat(p.Parent.AccessionNbr + "|", p.PanelCode, "|", DateTime.Now.ToString("M/d/yyyy HH:mm:ss"), "|TNP"));
                        p.MarkAsSentToReporting(); // 'Need to mark as reported first or will throw error
                        p.MarkAsStatusSentToVertex();
                        continue;
                    }

                    if (!isReportable && !p.Panel.IsReportable)
                    {
                        p.MarkAsReported(); // 'Results are not sent to reporting
                    }
                    else if (isReportable && p.Panel.IsReportable)
                    {
                        p.MarkAsSentToReporting();
                    }
                }
            }

        }

        public string GenerateMessage(Bioreference.LIS.Report blReport, bool isReportable)
        {
            logger.LogInformation($"({blReport.AccessionNbr}): GenerateMessage HasValidPrefix='{this.HasValidPrefixForAccessionStatus(blReport.AccessionNbr)}' HasReleased='{blReport.HasReleased()}' sendToFollow='{sendToFollow}'");
            // 'ONLY for specified accessions (CanSendAccessionNotes). If does not have any tests to release, DO NOT generate message - this will only generate specimen/accession notes and status (due to add-on).
            if (this.HasValidPrefixForAccessionStatus(blReport.AccessionNbr) || blReport.HasReleased() || sendToFollow)
            {
                Bioreference.ResultService.DI.Interface.DIResult rslt = GetResultFromObject(blReport, isReportable);
                if (!(rslt == null))
                {
                    return CreateMessage(rslt, isReportable, blReport);
                }
            }
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "GenerateMessage", "No message generated.", accessionNumber);
            return string.Empty;
        }

        /// <summary>
        /// Create a HL7 message from the Results and Report
        /// </summary>
        /// <param name="rResult"></param>
        /// <param name="isReportable"></param>
        /// <param name="blReport"></param>
        /// <returns></returns>
        private string CreateMessage(Bioreference.ResultService.DI.Interface.DIResult result, bool isReportable, Bioreference.LIS.Report blReport)
        {
            string hl7Message = string.Empty;
            var oMessage = new Bioreference.ResultService.DI.Interface.ORUMessage(); //new ORUMessage(this);
            oMessage.PatientInfoComments = new List<PatientComment>();
            //oMessage.LabAccession = new List<LabAccession>();
            //oMessage.LabAccessionComment = new List<LabReportComment>();
            oMessage.LabReport = new List<LabReport>();
            string sLastName = string.Empty;
            string sFirstName = string.Empty;
            // Dim sDOB As String = String.Empty
            string sSex = string.Empty;
            string sOrderdate = string.Empty;
            string sCollectDate = string.Empty;
            string sDocName = string.Empty;
            string sTestCode = string.Empty;
            //string sAccessionNum = string.Empty;
            if (pidCounter >= settings.ResultOut_FetchCount)
            {
                pidCounter = 1;
            }

            // Dim oStatusDoc As New StatusList

            try
            {
                // MSH
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "MSH", "MSH segment processed.", accessionNumber);
                oMessage.MSH = new MSH()
                {
                    SendingApplication = settings.MSH_SendingApp,
                    SendingFacilityCode = settings.MSH_SendingFacilityCode,
                    ReceivngApplication = settings.MSH_ReceivingApp,
                    ReceivingFacility = settings.MSH_ReceivingFacility,

                    MessageTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    MessageEvent = settings.MSH_MessageEvent,
                    MessageID = DateTime.Now.ToString("yyyyMMddHHmmssfff"),

                    ProcessingId = settings.MSH_ProcessingId,
                    VersionId = settings.MSH_VersionId,
                    AcceptAckType = settings.MSH_AcceptAckType,


                    //// logger.LogInformation(rResult.rReports.Count)
                    //// For Each x As Bioreference.DI.Interface.Report In rResult.rReports
                    //// logger.LogInformation(x.AnalyteList.Count)
                    //// If x.AnalyteList.Count = 0 Then
                    //// .SetMessageType("STA")
                    //// Exit For
                    //// End If
                    //// Next
                    //// If .MessageType = "" Then .SetMessageType(My.Settings.MSH_MessageType)

                    MessageType = isReportable ? settings.MSH_MessageType : "STA"
                    //if (isReportable)
                    //{
                    //messagetype    
                    //withBlock.SetMessageType(settings.MSH_MessageType);
                    //}
                    //else
                    //{
                    //    withBlock.SetMessageType("STA");
                    //}

                };

                // PID
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "PID", "PID segment processed.", accessionNumber);
                oMessage.PatientInfo = new PatientInfo()
                {
                    Set_ID = pidCounter.ToString(),
                    ClientMRN = result.rPatient.Id,
                    PatientIdentifierList = result.rPatient.IdentifierList,
                    LastName = result.rPatient.LastName,
                    FirstName = result.rPatient.FirstName,

                    //// '8/25/09 AG - Remove date of birth
                    //// 'sDOB = rResult.rPatient.DOB
                    //// 'If IsDate(sDob) Then .SetDOB(New iNTERFACEWARE.Chameleon.ChameleonDateTime(sDob))
                    Sex = result.rPatient.Sex,
                    Address1 = result.rPatient.Street1,
                    Address2 = result.rPatient.Street2,
                    City = result.rPatient.City,
                    State = result.rPatient.State,
                    Zip = result.rPatient.Zip,
                    HomePhone = result.rPatient.HomePhone,
                    WorkPhone = result.rPatient.WorkPhone,
                    SSN = result.rPatient.SocSecNum,
                    DoctorReference = result.rPatient.PhysicianAcct,
                    UpdateTrackingId = result.rPatient.UpdateTrackingId
                };

                // PID NTEs
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "PID_NTE", "PID NTE segment processed.", accessionNumber);
                if (result.rPatient.NTEs.Count > 0)
                {
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; NTECount: {NTECount}", "ReportOut", "PID_NTE", "PID NTE count extracted.", accessionNumber, result.rPatient.NTEs.Count);
                    int rowCount = 1;
                    int currentGroupId = 0;
                    oMessage.PatientInfoComments = new List<PatientComment>();
                    foreach (NTE n in result.rPatient.NTEs)
                    {
                        if (n.GroupId != currentGroupId)
                            rowCount = 1;
                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; PatientNTE: {PatientNTE}", "ReportOut", "PID_NTE", "Patient NTE extracted.", accessionNumber, n.Text);
                        var patientInfoComment = new PatientComment()
                        {
                            NoteId = n.GroupId != 0 ? string.Concat(n.GroupId, ",", rowCount) : rowCount.ToString(),
                            NoteText = n.Text.TrimEnd()
                            //    var withBlock2 = oMessage.PatientInfoComments(oMessage.PatientInfoComments().CountOfRow() - 1);
                            //    if (n.GroupId != 0)
                            //{
                            //    withBlock2.SetNoteId(string.Concat(n.GroupId, ",", rowCount));
                            //}
                            //else
                            //{
                            //    withBlock2.SetNoteId(rowCount.ToString());
                            //}
                            //withBlock2.SetNoteText(n.Text);

                        };
                        oMessage.PatientInfoComments.Add(patientInfoComment);
                        currentGroupId = n.GroupId;
                        rowCount += 1;
                    }
                }

                // 'FOR UPDATE
                // ''ORC notes
                // 'This goes here for now since reporting ignores ORC recs
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "ORC_NOTES", "ORC notes processed.", accessionNumber);
                if (!(result.NTEs == null))
                {
                    if (oMessage.PatientInfoComments == null) oMessage.PatientInfoComments = new List<PatientComment>();
                    for (int i = 0, loopTo = result.NTEs.Count - 1; i <= loopTo; i++)
                    {
                        //oMessage.PatientInfoComments().AddRow();
                        //oMessage.PatientInfoComments(i).SetNoteId((i + 1).ToString());
                        //oMessage.PatientInfoComments(i).SetNoteText(rResult.NTEs[i].Text);
                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; NoteText: {NoteText}", "ReportOut", "ORC_NOTES", "ORC note text extracted.", accessionNumber, result.NTEs[i].Text);
                        oMessage.PatientInfoComments.Add(new PatientComment() { NoteId = (i + 1).ToString(), NoteText = result.NTEs[i].Text.TrimEnd() });
                    }
                }

                // PV1
                //oMessage.PatientVisit().AddRow();
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "PV1", "PV1 segment processed.", accessionNumber);
                oMessage.PatientVisit = new PatientVisit()
                {
                    //var withBlock3 = oMessage.PatientVisit(0);
                    //withBlock3.SetSet_ID("1");
                    //withBlock3.SetPatientClass(rResult.rPatientVisit.PatientClass);
                    //withBlock3.SetPatLocation(rResult.rPatientVisit.PatientLocation);
                    Set_ID = "1",
                    PatientClass = result.rPatientVisit.PatientClass,
                    PatientLocation = result.rPatientVisit.PatientLocation
                };

                // 'FOR UPDATE
                // 'ORC
                // If rResult.OrderStatus <> "" Then
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "ORC", "ORC segment processed.", accessionNumber);
                //oMessage.Order().AddRow();
                oMessage.Order = new Bioreference.ResultService.DI.Interface.Order()
                {
                    //var withBlock4 = oMessage.Order(0);
                    AccessionNumber = result.rPatient.IdentifierList,
                    //withBlock4.SetAccessionNumber(rResult.rPatient.IdentifierList); // '(rResult.rPatient.Id)
                    OrderStatus = result.OrderStatus,
                    //withBlock4.SetOrderStatus(rResult.OrderStatus);
                    OrderDate = result.ServiceDate.DateTimeStamp(),
                    //if (Information.IsDate(rResult.ServiceDate))
                    //    withBlock4.SetOrderDate(DateTime.ParseExact(rResult.ServiceDate,"yyyyMMddHHmmss", CultureInfo.InvariantCulture));  //withBlock4.SetOrderDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(rResult.ServiceDate));

                    ConfidentialityCode = blReport.IsCOC ? "R" : string.Empty
                    //// for COC related results:
                    //if (blReport.IsCOC)
                    //{
                    //    withBlock4.SetConfidentialityCode("R");
                    //}
                };
                // End If

                // 'For those accessions, set ORC-25 to COMM and attach notes.
                if (CanSendAccessionNotes(result.rPatient.IdentifierList))
                {
                    //oMessage.Order(0).SetHasComments("COMM");
                    oMessage.Order.HasComments = "COMM";
                    oMessage.OrderComment = new List<Bioreference.ResultService.DI.Interface.OrderComment>();
                    for (int i = 0, loopTo1 = result.OrderComments.Count - 1; i <= loopTo1; i++)
                    {
                        //oMessage.OrderComment().AddRow();
                        //oMessage.OrderComment(i).SetNoteText(rResult.OrderComments[i]);
                        oMessage.OrderComment.Add(new Bioreference.ResultService.DI.Interface.OrderComment() { NoteText = result.OrderComments[i].TrimEnd() });
                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OrderComment: {OrderComment}", "ReportOut", "ORC_OrderComments", "Order comment extracted.", accessionNumber, result.OrderComments[i]);
                    }
                }

                // OBR
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "OBR", "OBR segment processed.", accessionNumber);
                //     result.rReports = new List<DI.Interface.Report>();
                if (result.rReports.Count > 0)
                {
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OBRCount: {OBRCount}", "ReportOut", "OBR", "Processing OBR count.", accessionNumber, result.rReports.Count);
                    var containsTestCode = default(bool);
                    int obrCount = 0;
                    //var labReports = new List<LabReport>();
                    //   oMessage.LabReports = new List<LabReportWrapper>();
                    foreach (Bioreference.ResultService.DI.Interface.Report rpt in result.rReports)
                    {
                        if (IsReportPresumptive(rpt, blReport))
                        {
                            continue;
                        }
                        // Dim oRptStat As New ReportStatus
                        //oMessage.LabReports().AddRow();
                        //var labReport = new LabReport();
                        //var labReport = new LabReport();
                        //oMessage.LabReports.Add(labReport);

                        //int LabReportsRow = oMessage.LabReports().CountOfRow();
                        //var objTblGrpORULabReportsRow = oMessage.LabReports(oMessage.LabReports().CountOfRow() - 1);
                        //objTblGrpORULabReportsRow.LabReport().AddRow();
                        //int RptSetId = objTblGrpORULabReportsRow.LabReport().CountOfRow();

                        var obr = new OBR();
                        //labReport.OBR = obr;
                        // AG - set this variable here so that we can use it down below.
                        //obrRow = obr; //objTblGrpORULabReportsRow.LabReport(objTblGrpORULabReportsRow.LabReport().CountOfRow() - 1);

                        //var withBlock5 = objTblGrpORULabReportsRow.LabReport(objTblGrpORULabReportsRow.LabReport().CountOfRow() - 1);
                        //withBlock5.SetSet_ID(LabReportsRow.ToString());
                        obr.Set_ID = (obrCount + 1).ToString();
                        //sAccessionNum = rpt.AccessionNum;
                        // oRptStat.AccessionNumber = sAccessionNum
                        //withBlock5.SetAccessionNum(sAccessionNum);
                        obr.AccessionNum = rpt.AccessionNum;
                        //withBlock5.SetTestCode(rpt.TestCode);
                        sTestCode = rpt.TestCode;
                        obr.TestCode = rpt.TestCode;
                        //withBlock5.SetTestDescription(rpt.TestDescription);
                        obr.TestDescription = rpt.TestDescription;
                        //withBlock5.SetPriority(rpt.Priority);
                        obr.Priority = rpt.Priority;
                        sOrderdate = rpt.OrderDate;
                        sCollectDate = rpt.CollectDate;
                        if (Information.IsDate(sOrderdate))
                            //withBlock5.SetRequestDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(sOrderdate));
                            obr.RequestDate = sOrderdate.DateTimeStamp();


                        if (Information.IsDate(sCollectDate))
                            //withBlock5.SetCollectDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(sCollectDate));
                            obr.CollectDate = sCollectDate.DateTimeStamp();
                        //withBlock5.SetCollectedBy(rpt.Collector);
                        obr.CollectedBy = rpt.Collector;
                        if (Information.IsDate(rpt.ReceiveDate))
                            //withBlock5.SetReceivedDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(rpt.ReceiveDate));
                            obr.ReceivedDate = rpt.ReceiveDate.DateTimeStamp();
                        //withBlock5.SetProviderCode(rpt.PhysCode);
                        obr.ProviderCode = rpt.PhysCode;
                        sDocName = rpt.PhysLastName;
                        //withBlock5.SetProviderLastName(sDocName);
                        obr.ProviderLastName = rpt.PhysLastName;
                        //withBlock5.SetProviderFirstName(rpt.PhysFirstName);
                        obr.ProviderFirstName = rpt.PhysFirstName;
                        //withBlock5.SetCallBackPhone(rpt.CallBackPhone);
                        obr.CallBackPhone = rpt.CallBackPhone;

                        // mzia (only for production)
                        // 'Do not supress Lenetix testcodes
                        bool isLenetix = false;
                        if (!(settings.Lenetix_TestCodes == null))
                        {
                            if (settings.Lenetix_TestCodes.Contains(rpt.TestCode))
                            {
                                isLenetix = true;
                            }
                        }
                        if (!(settings.TestCodes == null))
                        {
                            if (settings.TestCodes.Contains(sTestCode))
                            {
                                containsTestCode = true;
                            }
                        }

                        string panelLikeTest = GetPanelLikeTest(rpt.TestDescription);

                        if (!isLenetix & string.IsNullOrEmpty(panelLikeTest))
                        {
                            if (containsTestCode)
                            {
                                //withBlock5.SetIsPanel("SUPPRESS");
                                obr.IsPanel = "SUPPRESS";
                            }
                            else
                            {
                                //withBlock5.SetIsPanel("");
                                obr.IsPanel = "";
                            }
                        }
                        else
                        {
                            //withBlock5.SetIsPanel("");
                            obr.IsPanel = "";
                        }

                        if (Information.IsDate(rpt.ResultDate))
                        {
                            //withBlock5.SetResultDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(rpt.ResultDate));
                            //// oRptStat.ResultDate = rpt.ResultDate
                            obr.ResultDate = rpt.ResultDate.DateTimeStamp();
                        }

                        //withBlock5.SetDiagID(rpt.DiagnosticService);
                        obr.DiagID = rpt.DiagnosticService;
                        //withBlock5.SetLabReportStatus(rpt.ReportStatus);
                        obr.LabReportStatus = rpt.ReportStatus;

                        //sAccessionNum = rpt.AccessionNum;
                        // AddStatusLine(rpt.AccessionNum & "|" & rpt.TestCode & "|" & rpt.ResultDate & "|Completed")

                        // OBR NTEs
                        List<LabComment> obrComments = new List<LabComment>();
                        if (rpt.NTEs.Count > 0)
                        {
                            obrComments = new List<LabComment>();
                            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OBRNTECount: {OBRNTECount}", "ReportOut", "OBR_NTE", "Processing OBR NTE count.", accessionNumber, rpt.NTEs.Count);
                            var commentCount = 0;
                            foreach (NTE n in rpt.NTEs)
                            {
                                commentCount++;
                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; NoteText: {NoteText}", "ReportOut", "OBR_NTE", "OBR note text extracted.", accessionNumber, n.Text);
                                var labReportComment = new LabComment()
                                {
                                    NoteId = commentCount.ToString(),
                                    NoteText = n.Text.TrimEnd()
                                };
                                obrComments.Add(labReportComment);
                                //objTblGrpORULabReportsRow.LabReportComment().AddRow();
                                //int cmtSetId = objTblGrpORULabReportsRow.LabReportComment().CountOfRow();
                                //{
                                //    var withBlock6 = objTblGrpORULabReportsRow.LabReportComment(cmtSetId - 1);
                                //    withBlock6.SetNoteId(cmtSetId.ToString());
                                //    withBlock6.SetNoteText(n.Text);
                                //}
                            }
                        }

                        // OBX
                        logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "OBX", "OBX segment processed.", accessionNumber);
                        List<Bioreference.ResultService.DI.Interface.Result> results = null;
                        if (rpt.AnalyteList.Count > 0)
                        {
                            results = new List<Bioreference.ResultService.DI.Interface.Result>();
                            logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OBXCount: {OBXCount}; TestCode: {TestCode}; OrderedCode: {OrderedCode}; ParentCode: {ParentCode}", "ReportOut", "OBX", "Processing OBX details.", accessionNumber, rpt.AnalyteList.Count, rpt.TestCode, rpt.OrderedCode, rpt.ParentCode);
                            int count = 0;
                            bool addSuppress = true;
                            bool isProfileTNPed = false;
                            bool isProfileATPed = false;
                            bool isPanelTNPed = false;
                            bool isPanelATPed = false;
                            Bioreference.ResultService.DI.Interface.ReportAnalyte lastAnalyte = default;
                            LIS.ReportAnalyte lastAnalyteDetails = default;
                            bool checkToFollow = false;

                            var ti = Bioreference.Common.TestMaster.TestLite.Fetch(rpt.TestCode);

                            bool isProfile = ti.CodeTypeId == 2;
                            bool isPanel = ti.CodeTypeId == 3;
                            bool isTest = ti.CodeTypeId == 1;

                            if (!(ti == null) && ti.CodeTypeId == 2) // CodeTypeId=2 is Profile
                            {
                                isProfileTNPed = !(from n in rpt.AnalyteList
                                                   where !SharedFunctions.IsTNP(n.Value)
                                                   select n).Any();
                                if (isProfileTNPed)
                                {
                                    lastAnalyte = (from ana in rpt.AnalyteList
                                                   where SharedFunctions.IsTNP(ana.Value) && ana.NTEs.Count > 0 // Added NTE condition for caution, fronend rule adding calulation for Profile, system updating calulation as TNP but not adding any comments
                                                   select ana).FirstOrDefault();
                                }
                                isProfileATPed = !(from n in rpt.AnalyteList
                                                   where !SharedFunctions.IsATP(n.Value)
                                                   select n).Any();
                                if (isProfileATPed)
                                {
                                    lastAnalyte = (from ana in rpt.AnalyteList
                                                   where SharedFunctions.IsATP(ana.Value) && ana.NTEs.Count > 0 // Added NTE condition for caution, fronend rule adding calulation for Profile, system updating calulation as TNP but not adding any comments
                                                   select ana).FirstOrDefault();
                                }
                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; IsProfileTNPed: {IsProfileTNPed}; IsProfileATPed: {IsProfileATPed}", "ReportOut", "OBX_ProfileEvaluation", "Profile evaluation flags.", accessionNumber, isProfileTNPed, isProfileATPed);
                            }
                            else
                            {
                                lastAnalyte = rpt.AnalyteList.Last();
                            } // for Panel, all Panel comments added under last analyte
                            if (!(lastAnalyte == null))
                            {
                                lastAnalyteDetails = blReport.FindAnalyte(lastAnalyte.Code, true); // Getting analyte details from blreport to verify ToFollow has been sent or not.
                            }
                            isPanelTNPed = lastAnalyte is not null && lastAnalyte.PanelCode != "" && SharedFunctions.IsTNP(lastAnalyte.Value);  // 'ITBT-3467 Dim isPanelTNP As Boolean = lastAnalyte IsNot Nothing AndAlso lastAnalyte.PanelSPMStatus = ReportAnalytePanel.SPMStatusValue.TNP
                            isPanelATPed = lastAnalyte is not null && lastAnalyte.PanelCode != "" && SharedFunctions.IsATP(lastAnalyte.Value);  // 'ITBT-3467 Dim isPanelATP As Boolean = lastAnalyte IsNot Nothing AndAlso lastAnalyte.PanelSPMStatus = ReportAnalytePanel.SPMStatusValue.ATP
                            checkToFollow = lastAnalyteDetails is not null && !lastAnalyteDetails.ToFollowSent;
                            logger.LogInformation($"({accessionNumber}): IsPanelTNPed='{isPanelTNPed}', IsPanelATPed='{isPanelATPed}', checkToFollow={checkToFollow}");

                            // Dim embeddedPanelList As New List(Of String)
                            // Dim embeddedPanelCode As String = ""
                            // Dim embeddedPanelBypass As Boolean = False
                            // Dim embeddedPanelDescription As String = ""

                            // build list of embedded panels
                            // embeddedPanelList.AddRange((From n In rpt.AnalyteList Where n.PanelCode <> "" Select n.PanelCode).Distinct())

                            isProfileTNPed = !(from n in rpt.AnalyteList
                                               where !SharedFunctions.IsTNP(n.Value)
                                               select n).Any();

                            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; IsPanelTNPed: {IsPanelTNPed}; IsPanelATPed: {IsPanelATPed}; CheckToFollow: {CheckToFollow}", "ReportOut", "OBX_PanelEvaluation", "Panel evaluation flags.", accessionNumber, isPanelTNPed, isPanelATPed, checkToFollow);

                            foreach (Bioreference.ResultService.DI.Interface.ReportAnalyte a in rpt.AnalyteList)
                            {
                                // <NO LONGER NEEDED?>
                                // 'skip if we are looping through analytes in a TNP'd panel nested in this report
                                // If a.PanelCode = embeddedPanelCode AndAlso embeddedPanelBypass Then
                                // logger.LogInformation($"Embedded Panel Bypass Panel={a.PanelCode} Code={a.Code}")
                                // Continue For
                                // End If
                                // embeddedPanelBypass = False
                                // embeddedPanelCode = ""

                                // NA 09/04/2014
                                // Add Code here to bypass certain TestCodes.  Do not send to reporting
                                if (settings.ResultOut_TestCodesToNotReport.Contains(a.Code))
                                {
                                    continue;
                                }

                                ProcessNoBills(a, blReport, rpt);

                                // <NO LONGER NEEDED?>
                                // <nested panel TNP/ATP>
                                // check for special case where a panel nested within a profile has been TNP'd or ATP'd.
                                // Send TNP/ATP to reporting at panel level and flag bypass to skip other analytes in the same panel.
                                // If (embeddedPanelList.Contains(a.PanelCode)) Then
                                // logger.LogInformation($"Embedded Panel={a.PanelCode}, code={a.Code}")
                                // embeddedPanelCode = a.PanelCode
                                // lastAnalyte = (From n In rpt.AnalyteList Where n.PanelCode = embeddedPanelCode Select n).Last()
                                // If (Not IsNothing(lastAnalyte)) Then
                                // lastAnalyteDetails = blReport.FindAnalyte(lastAnalyte.Code, True) 'Getting analyte details from blreport to verify ToFollow has been sent or not.
                                // ''ITBT-3467 isPanelTNP = lastAnalyte IsNot Nothing AndAlso lastAnalyte.PanelCode = embeddedPanelCode AndAlso lastAnalyte.PanelSPMStatus = ReportAnalytePanel.SPMStatusValue.TNP
                                // ''ITBT-3467 isPanelATP = lastAnalyte IsNot Nothing AndAlso lastAnalyte.PanelCode = embeddedPanelCode AndAlso lastAnalyte.PanelSPMStatus = ReportAnalytePanel.SPMStatusValue.ATP
                                // isPanelTNPed = lastAnalyte IsNot Nothing AndAlso lastAnalyte.PanelCode = embeddedPanelCode AndAlso lastAnalyte.Value = "TNP"
                                // isPanelATPed = lastAnalyte IsNot Nothing AndAlso lastAnalyte.PanelCode = embeddedPanelCode AndAlso SharedFunctions.IsATP(lastAnalyte.Value)
                                // checkToFollow = lastAnalyteDetails IsNot Nothing AndAlso Not lastAnalyteDetails.ToFollowSent
                                // End If
                                // End If
                                // </nested panel TNP / ATP>

                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "AddCodeToMessage", "Add code to message.", accessionNumber);

                                LIS.ReportAnalyte analyte;
                                if (isPanel)
                                {
                                    analyte = blReport.FindAnalyteInPanel(ti.TestCode, a.Code);
                                }
                                else
                                {
                                    analyte = blReport.FindAnalyte(a.Code, true);
                                }

                                if (analyte is not null && analyte.IsPresumptiveHold && a.Value == "TO FOLLOW" && analyte.ResultStatus == LIS.resultStatusType.Pending)
                                {
                                    continue;
                                }

                                count += 1;

                                //objTblGrpORULabReportsRow.Results().AddRow();
                                //int analyteSetId = objTblGrpORULabReportsRow.Results().CountOfRow();
                                //{
                                //var withBlock7 = objTblGrpORULabReportsRow.Results(analyteSetId - 1);

                                var obx = new OBX();

                                //withBlock7.Result().AddRow();
                                //{
                                //var withBlock8 = withBlock7.Result(0);

                                obx.Set_ID = count.ToString(); //analyteSetId.ToString();
                                //withBlock8.SetSet_ID(analyteSetId.ToString());
                                obx.ValueType = a.ValueType;
                                //withBlock8.SetValueType(a.ValueType);
                                if (!(analyte == null) && analyte.InstrumentId != "N/A")
                                {
                                    obx.InstrumentId = analyte.InstrumentId;
                                    //withBlock8.SetInstrumentId(analyte.InstrumentId);
                                }
                                if (a.ValueType.Equals(AeStringValue))
                                {
                                    _aeList.Add(a.Code);
                                }

                                if (analyte is not null)
                                {
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; AnalyteCode: {AnalyteCode}; ResultValue: {ResultValue}; ToFollowSent: {ToFollowSent}", "ReportOut", "OBX_Analyte", "Analyte details processed.", accessionNumber, analyte.Code, analyte.ResultValue, analyte.ToFollowSent);
                                    if ((analyte.SPMStatus == SPMStatusValue.TNP || analyte.SPMStatus == SPMStatusValue.ATP) && settings.SPMTNP)
                                    {
                                        string spmTNPFlag = "";
                                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Analyte: {Analyte}; TestCode: {TestCode}; OrderingCodes: {OrderingCodes}; ParentTestCode: {ParentTestCode}; PanelCode: {PanelCode}; IsProfile: {IsProfile}; IsProfileTNP: {IsProfileTNP}; IsPanel: {IsPanel}; IsPanelTNP: {IsPanelTNP}; IsTest: {IsTest}", "ReportOut", "SetSPMTNP", "SPM TNP evaluation.", accessionNumber, analyte.Code, rpt.TestCode, analyte.OrderingCodes, analyte.ParentTestCode, a.PanelCode, isProfile, isProfileTNPed, isPanel, isPanelTNPed, isTest);
                                        if (isProfile && (isProfileTNPed || isProfileATPed))
                                        {
                                            spmTNPFlag = rpt.TestCode;
                                        }
                                        else if (isProfile && analyte.OrderingCodes != analyte.ParentTestCode)
                                        {
                                            spmTNPFlag = analyte.ParentTestCode;
                                        }
                                        else if (isProfile && analyte.OrderingCodes == rpt.TestCode)
                                        {
                                            spmTNPFlag = "";
                                        }
                                        else
                                        {
                                            spmTNPFlag = a.PanelCode;
                                        }
                                        if (!string.IsNullOrEmpty(spmTNPFlag))
                                        {
                                            obx.SPMTNP = $"SPMTNP-{spmTNPFlag}";
                                            //withBlock8.SetSPMTNP($"SPMTNP-{spmTNPFlag}");
                                        }
                                    }
                                }

                                if (!(analyte == null) && analyte.IsPresumptiveHold)
                                {
                                    obx.PresumptiveHold = "PH";
                                    //withBlock8.SetPresumptiveHold("PH");
                                }
                                obx.Code = a.Code;
                                //withBlock8.SetCode(a.Code);
                                // AG - if the FIRST component code(analyte) is not the same as the obr code, then we suppress.
                                // mzia: do not suppress if its an allergen obr
                                string panelLikeTest1 = GetPanelLikeTest(rpt.TestDescription);

                                // '******************************************************************
                                // 'Do not supress Lenetix testcodes
                                if (!settings.Lenetix_TestCodes.Contains(rpt.TestCode) && !string.IsNullOrEmpty(panelLikeTest1))
                                {
                                    // 'obrRow.SetIsPanel("SUPPRESS")
                                    addSuppress = false;
                                }
                                else
                                {
                                    addSuppress = rpt.AddSuppress;
                                }
                                if (obx.ReferenceLabId != "0" && a.Code == rpt.TestCode) //withBlock8.ReferenceLabId() != "0"
                                {
                                    addSuppress = false;
                                }
                                // '******************************************************************
                                obx.Description = a.Description;
                                //withBlock8.SetDescription(a.Description);

                                // 'Temporary Rounding for some calculations
                                // 'REMOVED - these should be set in TESTMASTER
                                // .SetValue(MyBase.TemporaryRoundingFunction(a.Code, a.Value))
                                obx.Value = a.Value;
                                //withBlock8.SetValue(a.Value);

                                // If a.Value = "TNP" Then oRptStat.TNP = True
                                obx.Units = a.Units;
                                //withBlock8.SetUnits(a.Units);
                                string sRefRange = a.ReferenceRange.Replace(Convert.ToChar(150), '-');
                                obx.ReferenceRange = sRefRange;
                                //withBlock8.SetReferenceRange(sRefRange);

                                if (a.Value != "TO FOLLOW")
                                {
                                    obx.Flag = a.Flag;
                                    //withBlock8.SetFlag(a.Flag);
                                }
                                obx.Status = a.Status;
                                //withBlock8.SetStatus(a.Status);
                                // '***********************************************

                                // N.A. : 10/1/16
                                // commented out due to production problem

                                if (a.ReferenceLabNumber == "0")
                                {
                                    obx.PerformLocation = a.PerformingFacility;
                                    //withBlock8.SetPerformLocation(a.PerformingFacility);

                                    obx.ProducerName = "BIO";
                                    //withBlock8.SetProducerName("BIO");
                                    if (count == 1)
                                        obr.AccessionLocation = a.AccessioningFacility;
                                    //obrRow.AccessionLocation = a.AccessioningFacility;
                                    //obrRow.SetAccessionLocation(a.AccessioningFacility); // Set the accessioning location from the first analyte
                                }
                                else
                                {
                                    if (a.RefLabPerformingFacilityId == -1)
                                    {
                                        obx.ReferenceLabId = a.PerformingFacility;
                                    }
                                    else if (a.RefLabPerformingFacilityId > 0)
                                    {
                                        obx.ReferenceLabId = a.RefLabPerformingFacilityId.ToString();
                                        //withBlock8.SetReferenceLabId(a.RefLabPerformingFacilityId.ToString());
                                    }
                                    else
                                    {
                                        obx.ReferenceLabId = a.ReferenceLabNumber;
                                        //withBlock8.SetReferenceLabId(a.ReferenceLabNumber);
                                    }
                                    obx.ProducerName = "SENDOUT";
                                    //withBlock8.SetProducerName("SENDOUT");
                                }
                                // '***********************************************

                                obx.ReportDate = a.ReportDate.DateTimeStamp();
                                //if (Information.IsDate(a.ReportDate))
                                //withBlock8.SetReportDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(a.ReportDate));

                                // mzia: set ReportingType if it is FixedLendth

                                // 'COMMENT CONSOLIDATION
                                // mzia 3/18/2010: split external codes based on their types
                                if (a.CommentIdMappings.Count > 0)
                                {
                                    string s1 = "";
                                    string s2 = "";
                                    foreach (string s in a.CommentIdMappings)
                                    {
                                        string[] cmtCodeAndType = s.Split('^');
                                        switch ((Bioreference.Common.TestMaster.ExternalCommentType)Conversions.ToInteger(cmtCodeAndType[1]))
                                        {
                                            case Bioreference.Common.TestMaster.ExternalCommentType.Statement:
                                                {
                                                    s1 += string.Concat(cmtCodeAndType[0], "~");
                                                    break;
                                                }
                                            case Bioreference.Common.TestMaster.ExternalCommentType.Comment:
                                                {
                                                    s2 += string.Concat(cmtCodeAndType[0], "~");
                                                    break;
                                                }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(s1))
                                        s1 = s1.Substring(0, s1.Length - 1);
                                    if (!string.IsNullOrEmpty(s2))
                                        s2 = s2.Substring(0, s2.Length - 1);
                                    obx.Comments_Reference = s1 + "^" + s2;
                                    //withBlock8.SetComments_Reference(s1 + "^" + s2);
                                }

                                // Coc Data set here
                                if (blReport.IsCOC)
                                {
                                    obx.ReleaseUser = a.CocApproverEmpNumber;
                                    //withBlock8.SetReleaseUser(a.CocApproverEmpNumber);
                                    obx.TechUserFName = a.CocApproverFirstName;
                                    //withBlock8.SetTechUserFName(a.CocApproverFirstName);
                                    obx.TechUser = a.CocApproverLastName;
                                    //withBlock8.SetTechUser(a.CocApproverLastName);
                                    obx.TechUserMName = a.CocApproverMiddleName;
                                    //withBlock8.SetTechUserMName(a.CocApproverMiddleName);
                                    obx.COCReleasedDate = a.CocApprovedDate.ToString().DateTimeStamp();
                                    //withBlock8.SetCOCReleasedDate(new iNTERFACEWARE.Chameleon.ChameleonDateTime(a.CocApprovedDate.ToString("MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)));
                                }
                                //}

                                // OBX NTEs
                                List<LabComment> obxComments = null;
                                if (a.NTEs.Count > 0)
                                {
                                    obxComments = new List<LabComment>();
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OBXNTECount: {OBXNTECount}", "ReportOut", "OBX_NTE", "Processing OBX NTE count.", accessionNumber, a.NTEs.Count);
                                    int lrcCount = 1;
                                    foreach (NTE n in a.NTEs)
                                    {
                                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; NoteText: {NoteText}", "ReportOut", "OBX_NTE", "OBX note text extracted.", accessionNumber, n.Text);
                                        var resultComment = new LabComment()
                                        {
                                            NoteId = lrcCount.ToString(),
                                            NoteText = n.Text.TrimEnd(),
                                            CommentCode = n.ExternalId
                                        };
                                        //withBlock7.ResultComment().AddRow();
                                        //int cmtSetId = withBlock7.ResultComment().CountOfRow();
                                        //{
                                        //    var withBlock9 = withBlock7.ResultComment(cmtSetId - 1);
                                        //    withBlock9.SetNoteId(cmtSetId.ToString());
                                        //    withBlock9.SetNoteText(n.Text);
                                        //    withBlock9.SetCommentCode(n.ExternalId);
                                        //}
                                        obxComments.Add(resultComment);
                                        lrcCount++;
                                    }
                                }

                                // Attachment by lines
                                // OBX|1|ED|A052^Hemostasis Assessment^BRLI|1|B2^multipart^JPEG^Base64^rest of message
                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "OBX_Attachments", "OBX attachments processed.", accessionNumber);
                                List<OBX> attachments = null;
                                if (!(a.Attachment == null) && a.Attachment.Count > 0)
                                {
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; AttachmentCount: {AttachmentCount}", "ReportOut", "OBX_Attachments", "Processing OBX attachment count.", accessionNumber, a.Attachment.Count);
                                    // MyBase.WarningMessages.Add(" - Inside the if...")
                                    attachments = new List<OBX>();
                                    foreach (string s in a.Attachment)
                                    {
                                        count += 1;
                                        // 'MyBase.WarningMessages.Add(count.ToString() + " - Inside Foreach: .")
                                        //objTblGrpORULabReportsRow.Results().AddRow();

                                        //analyteSetId = objTblGrpORULabReportsRow.Results().CountOfRow();
                                        //{
                                        //    var withBlock10 = objTblGrpORULabReportsRow.Results(analyteSetId - 1);
                                        //    withBlock10.Result().AddRow();
                                        //    {
                                        //        var withBlock11 = withBlock10.Result(0);
                                        //        withBlock11.SetSet_ID(analyteSetId.ToString());
                                        //        withBlock11.SetValueType("ED");
                                        //        withBlock11.SetCode(a.Code);
                                        //        withBlock11.SetDescription(a.Description);
                                        //        withBlock11.SetStatus(a.Status);
                                        //        withBlock11.SetAttachmentMultipart("multipart");
                                        //        withBlock11.SetAttachmentType(a.AttachmentType);
                                        //        withBlock11.SetAttachmentBase("Base64");
                                        //        // a.AttachmentType 'description type jpeg
                                        //        withBlock11.SetValue(s); // attachment line s
                                        //    }
                                        //}
                                        var attachment = new OBX();
                                        attachment.Set_ID = count.ToString();
                                        attachment.ValueType = "ED";
                                        attachment.Code = a.Code;
                                        attachment.Description = a.Description;
                                        attachment.Status = a.Status;
                                        attachment.AttachmentMultipart = "multipart";
                                        attachment.AttachmentType = a.AttachmentType;
                                        attachment.AttachmentBase = "Base64";
                                        // a.AttachmentType 'description type jpeg
                                        attachment.Value = s; // attachment line s
                                        attachments.Add(attachment);
                                    }
                                }

                                var obxs = new List<OBX>() { obx };
                                if (attachments != null) obxs.AddRange(attachments);
                                var oResult = new DI.Interface.Result()
                                {
                                    Obx = obxs,
                                    ResultComment = obxComments != null ? obxComments : new()
                                };
                                results.Add(oResult);
                            }
                            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; AddSuppress: {AddSuppress}", "ReportOut", "OBX_Suppress", "Add suppress flag evaluated.", accessionNumber, addSuppress);
                            if (addSuppress)
                                //obrRow.SetIsPanel("SUPPRESS");
                                //obrRow.IsPanel = "SUPPRESS";
                                obr.IsPanel = "SUPPRESS";
                        }

                        oMessage.LabReport.Add(new LabReport
                        {
                            Obr = obr,
                            LabReportComment = obrComments,
                            Result = results != null ? results : new()
                        });

                        obrCount++;
                    }
                }
                //obrRow = null;
                //sOutputMessage = oMessage.GenerateMessage();
                //var jsonMessage = JsonConvert.SerializeObject(oMessage);
                //sOutputMessage = jsonMessage;
                hl7Message = ToHL7(oMessage, "ORU_Mapping_Outbound.json");
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; HL7: {HL7}", "ReportOut", "HL7_Output", "HL7 message generated.", accessionNumber, hl7Message);
            }
            // obrRow = Nothing


            // Save the Status document for this accession number
            // oStatusDoc.Save(My.Settings.StatusMsgPath, My.Settings.ReportMsgPath & "DI_Statuses\")

            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "ReportOut", "CreateMessage", "CreateMessage() failed.", accessionNumber, ex.Message);
            }

            // mzia 3/2/2010: Incremental PIDs
            pidCounter = pidCounter + 1;
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; HL7: {HL7}", "ReportOut", "GenerateMessage", "HL7 message generated.", accessionNumber, hl7Message);
            return hl7Message;
        }

        public string ToHL7(ORUMessage message, string mappingFile)
        {
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HL7Configuration", mappingFile);
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Mapping JSON files not found.");
            string mapping = File.ReadAllText(jsonPath);
            var json = JsonConvert.SerializeObject(message);
            var hl7Builder = new HL7Builder();
            string hl7 = hl7Builder.CreateHl7FromObject(mapping, json);
            return hl7;
        }

        private bool IsReportPresumptive(DI.Interface.Report rpt, LIS.Report blReport)
        {
            if (rpt.AnalyteList.Count == 0 || blReport is null)
                return false;
            foreach (DI.Interface.ReportAnalyte a in rpt.AnalyteList)
            {
                LIS.ReportAnalyte ra = blReport.FindAnalyte(a.Code, true);
                if (ra is null || !ra.IsPresumptiveHold || ra.ResultStatus != LIS.resultStatusType.Pending || a.Value != "TO FOLLOW")
                {
                    return false;
                }
            }
            return true;
        }

        private void ProcessNoBills(Bioreference.ResultService.DI.Interface.ReportAnalyte a, Bioreference.LIS.Report blReport, Bioreference.ResultService.DI.Interface.Report rpt)
        {

            // EL 01/14/2016
            // Review NoBills and bypass if threshold or trigger conditions match
            // determine validation type
            // EL 03/26/2016 - Process PanelCodes
            NoBillCode.NoBillValidationType noBillValidation = double.TryParse(a.Value.ToString(), out _) ? NoBillCode.NoBillValidationType.Numeric : NoBillCode.NoBillValidationType.Text;


            // find matching nobill code based on testcode and validation type

            List<NoBillCode> noBills;
            if (a.PanelCode != "")
            {
                noBills = noBillCodes.FindAll(a.Code, a.PanelCode, (int)noBillValidation);
                noBills.AddRange(noBillCodes.FindAll(a.Code, a.PanelCode, (int)NoBillCode.NoBillValidationType.TestExists));
            }
            else
            {
                noBills = noBillCodes.FindAll(a.Code, noBillValidation);
                noBills.AddRange(noBillCodes.FindAll(a.Code, NoBillCode.NoBillValidationType.TestExists));
            }
            foreach (NoBillCode noBill in noBills)
            {
                logger.LogInformation($"({blReport.AccessionNbr}): NoBill - Matching code found for Test Code='{noBill.TestCode}' Panel='{(a.PanelCode != "" ? a.PanelCode : "n/a")}' ID='{noBill.NoBillId}'");
                if (noBill.PerformingFacilities.Contains(a.PerformingFacility))
                {
                    logger.LogInformation($"({blReport.AccessionNbr}): NoBill - Matched performing facility '{a.PerformingFacility}'");
                    if (noBill.ValidationTypeId == NoBillCode.NoBillValidationType.Numeric && (int)noBillValidation == NoBillValidationTypeNumeric)
                    {
                        logger.LogInformation($"({blReport.AccessionNbr}): NoBill - Checking '{a.Value}' between '{noBill.ThresholdLowValue}' and '{noBill.ThresholdHighValue}'");
                        double value = Convert.ToDouble(a.Value);
                        // check numeric value against threshold
                        if (value < Convert.ToDouble(noBill.ThresholdLowValue) || value > Convert.ToDouble(noBill.ThresholdHighValue))
                        {
                            logger.LogInformation($"({blReport.AccessionNbr}): NoBill match on threshold check");
                            if (noBill.ApplyForAllTestCodes)
                            {
                                // Send test which are not components 
                                if (noBill.SendTest && a.PanelCode.Equals(""))
                                {
                                    logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex A");
                                    SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }
                                // send test components
                                if (noBill.IsComponent && !a.PanelCode.Equals(""))
                                {
                                    logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex B");
                                    SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }
                            }
                            else if (!noBill.IsComponent | noBill.SendTest)
                            {
                                logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex C");
                                SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                            }
                            // send parent panel (optional for panel)
                            if (noBill.SendParent && !a.PanelCode.Equals(""))
                            {
                                logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex D");
                                SendNoBillMessageToVertex(rpt.AccessionNum, a.PanelCode, a.PerformingFacility, noBill.PanelStatusToSend, blReport.DateServiced);
                            }
                            if (noBill.DoNotReport)
                            {
                                logger.LogInformation($"({blReport.AccessionNbr}): DoNotReport: Mark as sent to vertex to avoid filling up the queue");
                                LIS.ReportAnalyte analyte = blReport.FindAnalyte(a.Code, true);
                                if (analyte is not null)
                                {
                                    analyte.MarkAsSentToReporting();
                                    analyte.MarkAsStatusSentToVertex();
                                }
                                continue;
                            }
                        }
                    }
                    else if (noBill.ValidationTypeId == NoBillCode.NoBillValidationType.Text && noBillValidation == NoBillCode.NoBillValidationType.Text)
                    {
                        // check text value against list of text ranges
                        logger.LogInformation($"({blReport.AccessionNbr}): NoBill - Checking '{a.Value}' against text values");
                        if (noBill.TextRanges.FindIndex(x => object.Equals(a.Value.Trim(), StringComparison.OrdinalIgnoreCase)) != -1)  // ''This line replace noBill.TextRanges.Contains(a.Value)
                        {
                            logger.LogInformation($"({blReport.AccessionNbr}): NoBill match on text values");
                            if (noBill.ApplyForAllTestCodes)
                            {
                                // Send test which are not components 
                                if (noBill.SendTest && a.PanelCode.Equals(""))
                                {
                                    logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex A");
                                    SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }
                                // send test components
                                if (noBill.IsComponent && !a.PanelCode.Equals(""))
                                {
                                    logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex B");
                                    SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }
                            }
                            else if (!noBill.IsComponent | noBill.SendTest)
                            {
                                logger.LogInformation($"({blReport.AccessionNbr}): SendNoBillMessageToVertex C");
                                SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                            }
                            // send parent panel (optional for panel)
                            if (noBill.SendParent && !noBill.ParentPanelCode.Equals("") | !a.PanelCode.Equals(""))
                            {
                                logger.LogInformation($"({accessionNumber}): SendNoBillMessageToVertex D");
                                SendNoBillMessageToVertex(rpt.AccessionNum, string.IsNullOrEmpty(noBill.ParentPanelCode) ? a.PanelCode : noBill.ParentPanelCode, a.PerformingFacility, noBill.PanelStatusToSend, blReport.DateServiced);
                            }
                            if (noBill.DoNotReport)
                            {
                                logger.LogInformation($"({accessionNumber}): DoNotReport: Mark as sent to vertex to avoid filling up the queue");
                                LIS.ReportAnalyte analyte = blReport.FindAnalyte(a.Code, true);
                                if (analyte is not null)
                                {
                                    analyte.MarkAsSentToReporting();
                                    analyte.MarkAsStatusSentToVertex();
                                }
                                continue;
                            }
                        }
                    }
                    else if (noBill.ValidationTypeId == NoBillCode.NoBillValidationType.TestExists)
                    {
                        string[] tests = noBill.TestExists.Split(",");
                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestExists: {TestExists}", "ReportOut", "NoBill_Check", "Checking NoBill TestExists flag.", accessionNumber, noBill.TestExists);
                        foreach (string test in tests)
                        {
                            if (blReport.AnalyteOrPanelExists(test))
                            {
                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Test: {Test}", "ReportOut", "NoBill_Match", "NoBill match for Test Exists.", accessionNumber, test);
                                if (noBill.ApplyForAllTestCodes)
                                {
                                    // Send test which are not components 
                                    if (noBill.SendTest && a.PanelCode.Equals(""))
                                    {
                                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "NoBill_Send", "SendNoBillMessageToVertex A.", accessionNumber);
                                        SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                    }
                                    // send test components
                                    if (noBill.IsComponent && !a.PanelCode.Equals(""))
                                    {
                                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "NoBill_Send", "SendNoBillMessageToVertex B.", accessionNumber);
                                        SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                    }
                                }
                                else if (!noBill.IsComponent | noBill.SendTest)
                                {
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "NoBill_Send", "SendNoBillMessageToVertex C.", accessionNumber);
                                    SendNoBillMessageToVertex(rpt.AccessionNum, a.Code, a.PerformingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }
                                // send parent panel (optional for panel)
                                if (noBill.SendParent && !noBill.ParentPanelCode.Equals("") | !a.PanelCode.Equals(""))
                                {
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "NoBill_Send", "SendNoBillMessageToVertex D.", accessionNumber);
                                    SendNoBillMessageToVertex(rpt.AccessionNum, string.IsNullOrEmpty(noBill.ParentPanelCode) ? a.PanelCode : noBill.ParentPanelCode, a.PerformingFacility, noBill.PanelStatusToSend, blReport.DateServiced);
                                }
                                if (noBill.DoNotReport)
                                {
                                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "DoNotReport", "Marking as sent to Vertex to avoid filling up the queue.", accessionNumber);
                                    LIS.ReportAnalyte analyte = blReport.FindAnalyte(a.Code, true);
                                    if (analyte is not null)
                                    {
                                        analyte.MarkAsSentToReporting();
                                        analyte.MarkAsStatusSentToVertex();
                                    }
                                    continue;
                                }
                            }
                        }
                    }
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "ReportOut", "NoBill_Check", "End NoBill check.", accessionNumber, a.Code);
                }
            }
        }

        /// <summary>
        /// Review NoBills and bypass(return false) if threshold or trigger conditions match determine validation type
        /// </summary>
        private bool IsBypassNoBillCode(string analyteValue, string panelCode, string code, string performingFacility, DI.Interface.Report rpt, Bioreference.LIS.Report blReport)
        {
            int noBillValidation = double.TryParse(analyteValue.ToString(), out _) ? NoBillValidationTypeNumeric : NoBillValidationTypeText;
            List<NoBillCode> noBills;

            // find matching nobill codes based on testcode And validation type
            if (!string.IsNullOrEmpty(panelCode))
            {
                noBills = noBillCodes.FindAll(code, panelCode, noBillValidation);
            }
            else
            {
                noBills = noBillCodes.FindAll(code, (NoBillCode.NoBillValidationType)noBillValidation);
            }
            foreach (NoBillCode noBill in noBills)
            {
                if (noBill.PerformingFacilities.Contains(performingFacility))
                {
                    if (noBillValidation == NoBillValidationTypeNumeric)
                    {
                        double value = Convert.ToDouble(analyteValue);
                        // check numeric value against threshold
                        if (value < Convert.ToDouble(noBill.ThresholdLowValue) || value > Convert.ToDouble(noBill.ThresholdHighValue))
                        {
                            if (noBill.ApplyForAllTestCodes)
                            {
                                // Send test which are not components 
                                if (noBill.SendTest && panelCode.Equals(""))
                                {
                                    SendNoBillMessageToVertex(rpt.AccessionNum, code, performingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }

                                // send test components
                                if (noBill.IsComponent && !panelCode.Equals(""))
                                {
                                    SendNoBillMessageToVertex(rpt.AccessionNum, code, performingFacility, noBill.StatusToSend, blReport.DateServiced);
                                }
                            }
                            else if (!noBill.IsComponent | noBill.SendTest)
                            {
                                SendNoBillMessageToVertex(rpt.AccessionNum, code, performingFacility, noBill.StatusToSend, blReport.DateServiced);
                            }
                            // send parent panel (optional for panel)
                            if (noBill.SendParent && !panelCode.Equals(""))
                            {
                                SendNoBillMessageToVertex(rpt.AccessionNum, panelCode, performingFacility, noBill.PanelStatusToSend, blReport.DateServiced);
                            }
                            if (noBill.DoNotReport)
                            {
                                // [EL] find analyte and mark as sent to vertex to avoid filling up the queue
                                LIS.ReportAnalyte analyte = blReport.FindAnalyte(code, true);
                                if (analyte is not null)
                                {
                                    analyte.MarkAsSentToReporting();
                                    analyte.MarkAsStatusSentToVertex();
                                }
                                return true;    // bypass 
                            }
                        }
                    }
                    // check text value against list of text ranges
                    else if (noBill.TextRanges.FindIndex(x => Equals(analyteValue.Trim(), StringComparison.OrdinalIgnoreCase)) != -1)
                    {

                        if (noBill.ApplyForAllTestCodes)
                        {
                            // Send test which are not components 
                            if (noBill.SendTest && panelCode.Equals(""))
                            {
                                SendNoBillMessageToVertex(rpt.AccessionNum, code, performingFacility, noBill.StatusToSend, blReport.DateServiced);
                            }

                            // send test components
                            if (noBill.IsComponent && !panelCode.Equals(""))
                            {
                                SendNoBillMessageToVertex(rpt.AccessionNum, code, performingFacility, noBill.StatusToSend, blReport.DateServiced);
                            }
                        }
                        else if (!noBill.IsComponent | noBill.SendTest)
                        {
                            SendNoBillMessageToVertex(rpt.AccessionNum, code, performingFacility, noBill.StatusToSend, blReport.DateServiced);
                        }

                        // send parent panel (optional for panel)
                        if (noBill.SendParent && !noBill.ParentPanelCode.Equals("") | !panelCode.Equals(""))
                        {
                            SendNoBillMessageToVertex(rpt.AccessionNum, string.IsNullOrEmpty(noBill.ParentPanelCode) ? panelCode : noBill.ParentPanelCode, performingFacility, noBill.PanelStatusToSend, blReport.DateServiced);
                        }
                        if (noBill.DoNotReport)
                        {
                            // [EL] find analyte and mark as sent to vertex to avoid filling up the queue
                            LIS.ReportAnalyte analyte = blReport.FindAnalyte(code, true);
                            if (analyte is not null)
                            {
                                analyte.MarkAsSentToReporting();
                                analyte.MarkAsStatusSentToVertex();
                            }
                            return true;    // bypass
                        }
                    }
                }
            }
            return false;
        }

        private string SendNoBillMessageToVertex(string accessionNum, string testCode, string performingFacility, string status, DateTime dateOfService)
        {

            // build message
            string msg = accessionNum + "|" + testCode + "|" + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + "|" + status + "|" + performingFacility;

            // archive
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Archive", "Archive to database...", accessionNumber);
            string timeStamp = "";
            try
            {
                timeStamp = ResultService.Common.Utilities.ArchiveStatus(msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "ReportOut", "SendNoBillMessageToVertex", "Exception occurred in SendNoBillMessageToVertex.", accessionNumber, ex.Message);
            }
            if (string.IsNullOrEmpty(timeStamp))
            {
                timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //ErrorMessages.Add("Failed to archive vertex message to database.");
                logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Archive", "Failed to archive vertex message to database.", accessionNumber);
            }
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "Archive", "Archive to database done.", accessionNumber);
            var obCHM = new OutboundMessageCHM(accessionNum);
            obCHM.Message = msg;
            obCHM.Save();
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Msg: {Msg}; CreateFile: {CreateFile}", "ReportOut", "OutboundMessageCHM", "Writing to OutboundMessageCHM table done.", accessionNumber, msg, obCHM.CreateFile);
            // Dim fileName As String
            // fileName = String.Concat(Settings.Outbound_StatusMessagePaths(outboundStatusMessagePathIndex), Settings.BIOFilePrefix, timeStamp, ".txt")

            // logger.LogInformation(String.Concat("Writing File:", fileName))
            // Dim sw As New System.IO.StreamWriter(fileName, True)
            // sw.WriteLine(msg)
            // sw.Close()
            // logger.LogInformation("Writing File done.")
            return msg;

        }

        private bool CanSendAccessionNotes(string accessionNbr)
        {

            // 'If none exist, always send comments
            if (settings.ResultOut_SendCommentsAccStartsWith.Count == 0)
                return true;

            return accessionNbr.Length == 9 && settings.ResultOut_SendCommentsAccStartsWith.Contains(accessionNbr.Substring(0, 2));

        }

        public bool HasValidPrefixForAccessionStatus(object accessionNbr)
        {

            if (settings.ResultOut_AccStatus_ValidPrefixes.Count == 0)
                return true;

            return Convert.ToBoolean(Operators.ConditionalCompareObjectEqual(((dynamic)accessionNbr).Length, 9, false) && settings.ResultOut_AccStatus_ValidPrefixes.Contains(Conversions.ToString(((dynamic)accessionNbr).Substring(0, 2))));

        }

        private bool IsPanelLikeTest(object testName)
        {
            // mzia: look for panelLike tests e.g. Allergens
            // For Each panelLikeTest As String In My.Settings.ResultOut_AllergensPanelLikeTests.Split(",")
            // If panelLikeTest = testName Then
            // Return True
            // End If
            // Next
            return false;
        }

        private string GetPanelLikeTest(string testDescription)
        {
            // For Each panelLikeTest As String In My.Settings.ResultOut_AllergensPanelLikeTests.Split(",")
            // Dim testInfo() As String = panelLikeTest.Trim.Split("^")
            // If testInfo(1) = testDescription Then
            // Return panelLikeTest.Trim
            // End If
            // Next
            return "";
        }

        /// <summary>
        /// From B2 Reports generate the DI object which be use to create the message.
        /// </summary>
        /// <param name="lisReport"></param>
        /// <param name="isReportable"></param>
        /// <returns></returns>
        private Bioreference.ResultService.DI.Interface.DIResult GetResultFromObject(Bioreference.LIS.Report lisReport, bool isReportable)
        {

            Bioreference.ResultService.DI.Interface.DIResult interfaceResult;
            Bioreference.ResultService.DI.Interface.Report bdiRpt = default;
            // 'Dim oSuppress As Bioreference.DI.Interface.ExcludedStrings = Nothing

            Bioreference.ResultService.DI.Interface.ReportAnalyte discreteAnalyte = default;
            bool tabularNTEHeadingCreated = false;

            // Dim orderableTests As List(Of String) = New List(Of String)()

            // mzia: A released IGE Serum should be added automatically if there are Allergens in report
            _rptIge = null;

            // Dim dupComments As ReportComments 'used to process comments

            interfaceResult = new Bioreference.ResultService.DI.Interface.DIResult();
            // If My.Settings.ResultOut_SuppressNTEs = True Then
            // oSuppress = New Bioreference.DI.Interface.ExcludedStrings
            // Dim success As String = oSuppress.CreateList(My.Settings.ResultOut_SuppressNTEsPath)
            // If success <> "SUCCESS" Then
            // MyBase.ErrorMessages.Add(String.Concat("Could not create SuppressedNtes list: ", success))
            // End If
            // End If
            long lisOrderId = lisReport.OrderId;
            Bioreference.LIS.Order lisOrder = OrderManager.FetchOrder(lisOrderId);
            interfaceResult.ServiceDate = lisOrder.DateOfService.ToString();

            interfaceResult.rPatient = new Bioreference.ResultService.DI.Interface.Patient(lisOrder.Patient.PatientId, lisReport.AccessionNbr, lisOrder.Patient.LastName, lisOrder.Patient.FirstName, lisOrder.Patient.MiddleName, lisOrder.Patient.DateOfBirth, ResultService.Common.Utilities.GenderToSex(lisOrder.Patient.Gender), lisOrder.Patient.PrimaryAddress.StreetLine1, lisOrder.Patient.PrimaryAddress.StreetLine2, lisOrder.Patient.PrimaryAddress.City, lisOrder.Patient.PrimaryAddress.State, lisOrder.Patient.PrimaryAddress.ZipCode, lisOrder.Patient.HomePhoneNumber, lisOrder.Patient.WorkPhoneNumber, lisOrder.Patient.SocialSecurityNum, default, lisOrder.AccountNumber);

            interfaceResult.rPatient.UpdateTrackingId = lisOrder.Patient.UpdateTrackingId;

            interfaceResult.rPatientVisit = new Bioreference.ResultService.DI.Interface.PatientVisit(default, lisOrder.AccountNumber);


            // 'FOR UPDATE
            // '****************************************************
            // 'Set Order Status

            if (settings.ResultOut_AccStatus_Enable)
            {
                if (this.HasValidPrefixForAccessionStatus(lisReport.AccessionNbr))
                {
                    interfaceResult.OrderStatus = lisReport.IsFinal() ? "CM" : "A";

                }
            }

            // 'AccessionComments
            if (this.CanSendAccessionNotes(lisOrder.AccessionNbr))
            {
                string sComments;
                foreach (Bioreference.LIS.OrderComment cmt in lisOrder.OrderComments.List)
                {
                    sComments = ResultService.Common.Utilities.FormatFixedWidthText(cmt.Text, ResultService.Common.Utilities.NOTEFIXEDWIDTH);
                    foreach (string s in sComments.Split('\n'))
                        interfaceResult.OrderComments.Add(s.Replace("\n", ""));
                }
            }

            // 'Order/Specimen comments
            if (lisReport.SpecimenComment != "")
            {
                interfaceResult.Comments = lisReport.SpecimenComment;
            }
            // '****************************************************

            bool hasAllergen = false;
            bool igeTestExists = false;
            // Loop through all the analytes and check TransmitStatus.
            var isAdded = default(bool);
            foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
            {
                if (a.TransmitStatus == transmitStatusType.Released && a.Analyte.IsReportable == isReportable || a.ToFollowSent == false)
                // 'OrElse (Not lisReport.HasReleased() AndAlso a.HasBeenReleased())) _
                {

                    if (!settings.ResultOut_SendOrderedCode)
                    {
                        isAdded = AddAnalyteResultToReport(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, a.Code, true);
                    }
                    else
                    {
                        foreach (string s in a.OrderingAnalyteCodes)
                            // If s <> a.Code AndAlso Not orderableTests.Contains(s) Then orderableTests.Add(s)
                            isAdded = AddAnalyteResultToReport(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, s, true);
                    }

                    if (a.Code.Equals(settings.ResultOut_IGECode) && (int)a.TransmitStatus > 1)
                    {
                        igeTestExists = true;
                    }

                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; IsAdded: {IsAdded}; Category: {Category}", "ReportOut", "OBX_Category", "Category evaluation.", accessionNumber, isAdded, a.Analyte.Category.ToLower());
                    if (isAdded && a.Analyte.Category.ToLower().Equals("allergen"))
                    {
                        hasAllergen = true;
                        // NA 11/14/13
                        _hasAllergen = true;
                    }

                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Code: {Code}", "ReportOut", "OBX_Code", "Code processed.", accessionNumber, a.Code);

                }
            }

            // And loop through all the panels (ie. URINALYSIS)
            var isAdded1 = default(bool);
            foreach (ReportAnalytePanel p in lisReport.AnalytePanels.List)
            {
                if (p.TransmitStatus == transmitStatusType.Released && p.Panel.IsReportable == isReportable || p.ToFollowSent == false)
                // 'OrElse (lisReport.HasReleased() AndAlso p.HasBeenReleased())) _
                {
                    if (!settings.ResultOut_SendOrderedCode)
                    {
                        isAdded1 = AddPanelResultToReport(lisOrder, lisReport, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, p.PanelCode, true);
                    }
                    else
                    {
                        foreach (string s in p.OrderingPanelCodes)
                        {
                            if (AddPanelResultToReport(lisOrder, lisReport, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, s, true))
                            {
                                isAdded1 = true;
                            }
                        }
                    }

                    if (isAdded1 && p.Panel.Category.ToLower().Equals("allergen"))
                    {
                        hasAllergen = true;
                        // NA 11/14/13
                        _hasAllergen = true;
                    }
                }
            }
            // ' vcc: 4/4/2017
            // ' If IgE and Allergen exists, Loop through all results And resend allergens
            // '************************************************************************
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; IGETestExists: {IGETestExists}", "ReportOut", "IGE_Check", "IGE test existence evaluated.", accessionNumber, igeTestExists);
            if (igeTestExists)
            {
                if (_rptIge == null)
                {
                    _rptIge = CreateIge(lisReport, lisOrder);
                }

                foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
                {
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Code: {Code}", "ReportOut", "Foreach", "Foreach iteration code processed.", accessionNumber, a.Code);
                    if (a.Analyte.IsReportable == isReportable && a.Analyte.Category.ToLower().Equals("allergen")) // 'AndAlso IsNothing(interfaceResult.FindReportAnalyte(a.Code)) Then
                    {
                        // 'bdiRpt = GetAnalyteResultFromObject(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, "", True)
                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Code: {Code}", "ReportOut", "InsideId", "Inside Id processed.", accessionNumber, a.Code);
                        if (!a.OrderingCodes.Equals(""))
                        {
                            string[] ordCodes = a.OrderingCodes.Split(",");
                            foreach (string ordCode in ordCodes)
                            {
                                bdiRpt = CreateAllergenDIReport(ordCode, bdiRpt, lisReport, lisOrder);
                                // Don't read if already exists
                                if (!(bdiRpt == null))
                                {
                                    if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                                    {
                                        interfaceResult.rReports.Add(bdiRpt);
                                    }
                                }
                            }
                        }
                    }
                }

            }

            // mzia: Add IGE Serum to the report if not already added
            if (_rptIge is not null)
            {
                interfaceResult.rReports.Add(_rptIge);

            }

            return interfaceResult;

        }

        private bool ContainsOrderableCode(string[] codesToCheck, string orderableCode)
        {

            foreach (string c in codesToCheck)
            {
                if ((c ?? "") == (orderableCode ?? ""))
                    return true;
            }
            return false;

        }

        private bool AddAnalyteResultToReport(Bioreference.LIS.Order lisOrder, Bioreference.LIS.ReportAnalyte a, Bioreference.ResultService.DI.Interface.DIResult interfaceResult, Bioreference.ResultService.DI.Interface.ReportAnalyte discreteAnalyte, bool tabularNTEHeadingCreated, bool isReportable, string orderedTestCode, bool processIfPanelLikeTest)
        {

            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Code: {Code}; IsReportable: {IsReportable}", "ReportOut", "Adding", "Adding analyte.", accessionNumber, a.Code, isReportable);

            Bioreference.ResultService.DI.Interface.Report bdiRpt;
            bdiRpt = GetAnalyteResultFromObject(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, orderedTestCode, processIfPanelLikeTest);

            if (bdiRpt == null)
            {
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Code: {Code}", "ReportOut", "NotAdded", "Analyte not added.", accessionNumber, a.Code);
                return false;
            }

            // If this is an IGE report then add to the bottom
            if (a.Code == settings.ResultOut_IGECode)
            {
                if (_rptIge is null || _rptIge.TestCode != settings.ResultOut_IGECode)
                {
                    _rptIge = bdiRpt;
                }
            }
            // Don't readd if already exists
            else if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                interfaceResult.rReports.Add(bdiRpt);

            return true;

        }

        private Bioreference.ResultService.DI.Interface.Report GetAnalyteResultFromObject(Bioreference.LIS.Order lisOrder, Bioreference.LIS.ReportAnalyte lisAnalyte, Bioreference.ResultService.DI.Interface.DIResult bdiRslt, Bioreference.ResultService.DI.Interface.ReportAnalyte interfaceDiscreteAnalyte, bool tabularNTEHeadingCreated, string orderedTestCode, bool processIfPanelLikeTest)
        {


            interfaceDiscreteAnalyte = default;
            tabularNTEHeadingCreated = false;

            // 'If a canceled allergen, do  not send to reporting.
            if (IsCanceledAllergen(lisAnalyte))
            {
                canceled = true;
                return default;
            }

            // 'If OutboundChannelId is set, then we pass back the ordering code for the analyte.
            // 'Change: Ordering always goes out.
            string code = settings.ResultOut_ChannelId == 0 ? lisAnalyte.Analyte.Code : lisAnalyte.OrderingAnalyteCodes[0];
            string name = lisAnalyte.Analyte.Name;


            // 'Alternate Outbound***************************************
            if (object.ReferenceEquals(lisAnalyte.Analyte.GetType(), typeof(RefAnalyte)))
            {
                RefAnalyte refa = (RefAnalyte)lisAnalyte.Analyte;
                if (!refa.AltOutboundTestCode.Equals(""))
                {
                    code = refa.AltOutboundTestCode;
                }
                if (!refa.AltOutboundDescription.Equals(""))
                {
                    name = refa.AltOutboundDescription;
                }
            }
            // **********************************************************

            // 'lisAnalyte.ResultStatus
            string[] testInfo;

            // mzia: look for panelLike tests e.g. Allergens
            // **************************************************
            string panelLikeTest = GetPanelLikeTest(lisAnalyte.Analyte.Category);
            Bioreference.ResultService.DI.Interface.Report bdiRpt = default;

            string obrName = "";
            bool isPanelLikeTest = false;
            if (panelLikeTest.Contains("^") && processIfPanelLikeTest)
            {

                testInfo = panelLikeTest.Split('^');
                bdiRpt = bdiRslt.FindReport(testInfo[0]);

                obrName = testInfo[1];
                orderedTestCode = testInfo[0];
                isPanelLikeTest = true;
            }
            // **************************************************

            Bioreference.LIS.resultStatusType status;
            if (isPanelLikeTest)
            {
                status = lisAnalyte.GetParentReport().GetGroupStatusForReportingByCategory(obrName);
            }
            else
            {
                status = lisAnalyte.GetParentReport().GetGroupStatusForReporting(orderedTestCode);
            }

            if (bdiRpt == null)
            {
                if (!string.IsNullOrEmpty(orderedTestCode))
                    bdiRpt = bdiRslt.FindReport(orderedTestCode);
                if (bdiRpt == null)
                {

                    if (!isPanelLikeTest)
                    {
                        // Dim obrName As String = ""
                        Bioreference.LIS.OrderTest ot = lisOrder.Tests.Find(code, orderedTestCode);
                        if (!(ot == null))
                        {
                            obrName = ot.OrderedTestName;
                        }
                        else
                        {
                            // 'If created from a reflex will not have an OrderTest - need to get test name from another.
                            obrName = lisOrder.Tests.FindOrderedTestName(orderedTestCode);
                        }
                        if (obrName.Equals(""))
                            obrName = name;
                    }

                    bdiRpt = new Bioreference.ResultService.DI.Interface.Report(orderedTestCode, lisOrder.Comments, obrName, accessionNumber, ResultService.Common.Utilities.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, lisAnalyte.ResultDate.ToString(), lisAnalyte.Analyte.Category, ResultService.Common.Utilities.ConvertStatusToString(status), lisAnalyte.ParentTestCode);

                    // '********************************************************************************************
                    // 'If the ordered code is not an actual analyte
                    Bioreference.LIS.Report report = lisAnalyte.GetParentReport();
                    if (report.FindAnalyte(orderedTestCode, false) == null)
                    {

                        // 'For these specific type of accession, we add the suppress when the group status is final
                        if (this.HasValidPrefixForAccessionStatus(lisOrder.AccessionNbr))
                        {
                            Bioreference.LIS.resultStatusType orderedStatus = report.GetGroupStatusForReporting(orderedTestCode);

                            if (orderedStatus == LIS.resultStatusType.Final || orderedStatus == LIS.resultStatusType.Corrected)
                            {
                                bdiRpt.AddSuppress = true;
                            }
                        }
                        else
                        {
                            bdiRpt.AddSuppress = true;
                        }

                    }
                    // '********************************************************************************************
                }

            }

            if (!(FindAnalyte(bdiRpt, code) == null))
                return default; // 'Already exists in OBR, do not readd.

            // 'If status = LIS.resultStatusType.Pending OrElse bdiRpt.ReportStatus.Equals("") Then
            bdiRpt.ReportStatus = ResultService.Common.Utilities.ConvertStatusToString(status);
            // 'End If
            // ******************************************************

            if (!lisAnalyte.Analyte.IsReportable)
            {

                // bdiRslt.rReports.Add(bdiRpt) ''Adds without OBX
                return bdiRpt;
            }

            else
            {
                // Dim resultValue As String = lisAnalyte.ResultValue   


                // 'TO FOLLOW was added for orderables
                // If lisAnalyte.ResultValue = "" Then
                // resultValue = "TO FOLLOW"
                // ElseIf Not lisAnalyte.HasBeenReleased() Then
                // resultValue = "Pending"
                // End If

                // Dim resultValue As String = UpdateResultToFollow(lisAnalyte)

                var reportDate = default(DateTime);
                string resultValue = ResultService.Common.Utilities.UpdateResultToFollow(lisAnalyte, ref reportDate);

                // if the test code is in the List of Tests Needing to be sent as ST,
                bool sendAoeAsStType = settings.SendAsSTResultType.Contains(code);

                var interfaceAnalyte = new Bioreference.ResultService.DI.Interface.ReportAnalyte(code, resultValue, reportDate.ToString(), default, ResultService.Common.Utilities.ConvertResultTypeToString(lisAnalyte.Analyte.ResultType, lisAnalyte.CodeTypeId == 5 ? true : false, sendAoeAsStType), name, lisAnalyte.Analyte.Units, lisAnalyte.GetReferenceRange(), ResultService.Common.Utilities.ConvertStatusToString(lisAnalyte.ResultStatus), lisAnalyte.Analyte.ReferenceLabId.ToString(), default, lisAnalyte.FlagValue, lisAnalyte.PerformingFacility, lisAnalyte.AccessioningFacility, lisAnalyte.RefLabPerformingFacilityId); // a.GetFlaggedValue

                interfaceAnalyte.CocApproverEmpNumber = lisAnalyte.CocApproverEmpNbr;
                interfaceAnalyte.CocApproverFirstName = lisAnalyte.CocApproverFirstName;
                interfaceAnalyte.CocApproverLastName = lisAnalyte.CocApproverLastName;
                interfaceAnalyte.CocApproverMiddleName = lisAnalyte.CocApproverMiddleName;
                interfaceAnalyte.CocApprovedDate = lisAnalyte.CocApprovedDate;

                ProcessAnalyteComments(lisAnalyte, interfaceAnalyte);
                ProcessAnalyteAttachments(lisAnalyte, interfaceAnalyte);

                // mzia: if this analyte's reporting type is TabularText then add this analyte as an NTE 
                if (lisAnalyte.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText || lisAnalyte.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                {
                    if (interfaceDiscreteAnalyte is null)
                    {
                        interfaceDiscreteAnalyte = new Bioreference.ResultService.DI.Interface.ReportAnalyte(bdiRpt.TestCode, "SEE BELOW", lisAnalyte.ReleaseDate > DateTime.Parse("1900-01-01") ? lisAnalyte.ReleaseDate.ToString() : DateTime.Now.ToString("MM/dd/yyyy"), "", "ST", bdiRpt.TestDescription, "", "", "F", interfaceAnalyte.ReferenceLabNumber, "", "", "", "", interfaceAnalyte.RefLabPerformingFacilityId);
                    }
                    if (!tabularNTEHeadingCreated && lisAnalyte.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                    {
                        interfaceDiscreteAnalyte.AddNTE(new NTE(string.Concat(testDescriptionTitle, resultTitle, flagTitle, referenceRangeTitle, unitsTitle)));
                        tabularNTEHeadingCreated = true;
                    }

                    var notes = ResultService.Common.Utilities.CreateNoteFromAnalyte(lisAnalyte);
                    foreach (string s in notes)
                        interfaceDiscreteAnalyte.AddNTE(new NTE(s));

                    // _discreteAnalyte.AddNTE(New NTE(testDescriptionText + resultText + flagText + referenceRangeText + unitsText))
                    // _diReport.AddNTE(New NTE(testDescriptionText + resultText + flagText + referenceRangeText + unitsText))

                    // mzia: Also add analyte's NTEs if any since analyte is not going to display
                    foreach (NTE nte in interfaceAnalyte.NTEs)
                    {
                        nte.Text = settings.TabularTextNTEPrefix + nte.Text;
                        interfaceDiscreteAnalyte.AddNTE(nte);
                    }

                    bool analyteExists = false;
                    foreach (Bioreference.ResultService.DI.Interface.ReportAnalyte _analyte in bdiRpt.AnalyteList)
                    {
                        if (_analyte.Code == interfaceDiscreteAnalyte.Code)
                        {
                            analyteExists = true;
                            break;
                        }
                    }

                    if (!analyteExists)
                    {
                        bdiRpt.AddAnalyte(interfaceDiscreteAnalyte);
                    }

                    if (bdiRpt.AnalyteList.Count > 0 || bdiRpt.NTEs.Count > 0)
                    {
                        bdiRslt.rReports.Add(bdiRpt);
                    }
                }
                else
                {
                    bdiRpt.AddAnalyte(interfaceAnalyte);

                    // mzia: TabularText reports does not have analytes/OBXs they have rather NTEs
                    if (bdiRpt.AnalyteList.Count > 0)
                    {

                        return bdiRpt;
                    }
                    else
                    {
                        return default;
                    }
                }

            }

            return bdiRpt;

        }

        private void ProcessAnalyteAttachments(Bioreference.LIS.ReportAnalyte lisAnalyte, Bioreference.ResultService.DI.Interface.ReportAnalyte interfaceAnalyte)
        {
            Attachments attList = lisAnalyte.AnalyteAttachments;
            if (!(attList == null) && attList.List.Count > 0)
            {
                Attachment att = attList.List[0];
                // Process att
                interfaceAnalyte.AttachmentType = att.AttachmentTypeDescription;
                // ' MyBase.WarningMessages.Add(String.Concat("interfaceAnalyte.AttachmentType=", att.AttachmentTypeDescription))
                string str = att.FileContentsBase64;
                int chunkSize = 32768;
                int stringLength = str.Length;

                // MyBase.WarningMessages.Add(String.Concat("att.FileContentsBase64=", att.FileContentsBase64.Substring(0, 30), "..."))
                // MyBase.WarningMessages.Add(String.Concat("stringLength=", stringLength.ToString()))

                for (int i = 0, loopTo = stringLength; chunkSize >= 0 ? i <= loopTo : i >= loopTo; i += chunkSize)
                {
                    if (i + chunkSize > stringLength)
                    {
                        chunkSize = stringLength - i;
                    }
                    // 'MyBase.WarningMessages.Add(String.Concat(i.ToString(), " - Attachment chunk added..."))
                    interfaceAnalyte.AddAttachmentLine(str.Substring(i, chunkSize));
                }
            }
        }

        private bool CheckTNPComment(string text)
        {
            return (from a in settings.TNPCommentsToReport
                    where text.ToLower().Contains(a.ToString().ToLower())
                    select a).Any();
        }

        private bool CheckATPComment(string text)
        {
            return (from a in settings.ATPCommentsToReport
                    where text.ToLower().Contains(a.ToString().ToLower())
                    select a).Any();
        }

        private void ProcessAnalyteComments(Bioreference.LIS.ReportAnalyte lisAnalyte, Bioreference.ResultService.DI.Interface.ReportAnalyte interfaceAnalyte)
        {

            int aCount = 1;
            bool IsTNPComment = false;
            bool IsATPComment = false;

            foreach (ReportComment cmt in lisAnalyte.Comments.List)
            {

                // Blocking TNP/ATP auto comments not to send to reporting, but auto comment is TNP comment system let go
                IsTNPComment = CheckTNPComment(cmt.Text);
                if (SharedFunctions.IsTNP(lisAnalyte.ResultValue) && cmt.IsAutoAdded && !IsTNPComment)
                {
                    continue;
                }

                IsATPComment = CheckATPComment(cmt.Text);
                if (SharedFunctions.IsATP(lisAnalyte.ResultValue) && cmt.IsAutoAdded && !IsATPComment)
                {
                    continue;
                }

                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Comment: {Comment}", "ReportOut", "ProcessAnalyteComments", "Analyte comment processed.", accessionNumber, cmt.Text);
                // mzia: Individual Analyte Comments
                // mzia: if cmt.ExternalCommentCode is not empty then add to comment code to OBX otherwise add NTEs
                if (cmt.ExternalCommentCode != "")
                {
                    interfaceAnalyte.CommentIdMappings.Add(cmt.ExternalCommentCode + "^" + ((int)cmt.ExternalCommentType).ToString());
                }
                else
                {
                    // If not the first note, then put a blank line
                    if (aCount > 1)
                    {
                        interfaceAnalyte.AddNTE(new NTE(""));
                    }

                    if (settings.ResultOut_SuppressNteAsBlockRefLabIds.Contains(lisAnalyte.Analyte.ReferenceLabId.ToString()))
                    {

                        bool foundFlag = false;
                        foreach (string nte in _oSuppress.StringList)
                        {
                            if ((Strings.RTrim(cmt.Text.Replace(Constants.vbCrLf, "")) ?? "") == (nte ?? ""))
                            {
                                foundFlag = true;
                                break;
                            }
                        }
                        if (!foundFlag)
                        {
                            foreach (string s in Strings.Split(cmt.Text, Constants.vbCrLf))
                                interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(s), externalid: cmt.ExternalId));
                        }
                    }

                    else if (Strings.InStr(cmt.Text, Constants.vbCrLf) > 0)
                    {
                        string[] comments = Strings.Split(cmt.Text, Constants.vbCrLf);
                        foreach (string comment in comments)
                        {
                            if (settings.ResultOut_SuppressNTEs == true)
                            {
                                interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId), _oSuppress);
                            }
                            else
                            {
                                interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId));
                            }
                        }
                    }
                    else if (settings.ResultOut_SuppressNTEs == true)
                    {
                        interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId), _oSuppress);
                    }
                    else
                    {
                        interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId));

                    }

                    aCount += 1;

                }

            }

            foreach (NTE nt in interfaceAnalyte.NTEs)
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Text: {Text}", "ReportOut", "AfterProcessing", "After processing comment.", accessionNumber, nt.Text);


        }

        private void ProcessAllergenComments(Bioreference.LIS.ReportAnalyte lisAnalyte, Bioreference.ResultService.DI.Interface.ReportAnalyte interfaceAnalyte, Bioreference.ResultService.DI.Interface.Report interfaceReport)
        {

            int aCount = 1;
            bool IsTNPAutoComment = false;
            bool IsATPAutoComment = false;

            foreach (ReportComment cmt in lisAnalyte.Comments.List)
            {

                // Blocking TNP auto comments not to send to reporting, but auto comment is TNP comment system let go
                IsTNPAutoComment = CheckTNPComment(cmt.Text);
                if (SharedFunctions.IsTNP(lisAnalyte.ResultValue) && cmt.IsAutoAdded && !IsTNPAutoComment)
                {
                    continue;
                }

                IsATPAutoComment = CheckATPComment(cmt.Text);
                if (SharedFunctions.IsATP(lisAnalyte.ResultValue) && cmt.IsAutoAdded && !IsATPAutoComment)
                {
                    continue;
                }

                // mzia: Individual Analyte Comments
                // mzia: if cmt.ExternalCommentCode is not empty then add to comment code to OBX otherwise add NTEs
                if (cmt.ExternalCommentCode != "")
                {
                    interfaceAnalyte.CommentIdMappings.Add(cmt.ExternalCommentCode + "^" + ((int)cmt.ExternalCommentType).ToString());
                }
                else
                {
                    // If not the first note, then put a blank line
                    if (aCount > 1)
                    {
                        interfaceReport.AddNTE(new NTE(""));
                    }

                    if (settings.ResultOut_SuppressNteAsBlockRefLabIds.Contains(lisAnalyte.Analyte.ReferenceLabId.ToString()))
                    {

                        bool foundFlag = false;
                        foreach (string nte in _oSuppress.StringList)
                        {
                            if ((Strings.RTrim(cmt.Text.Replace(Constants.vbCrLf, "")) ?? "") == (nte ?? ""))
                            {
                                foundFlag = true;
                                break;
                            }
                        }
                        if (!foundFlag)
                        {
                            foreach (string s in Strings.Split(cmt.Text, Constants.vbCrLf))
                                interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(s)));
                        }
                    }


                    else if (Strings.InStr(cmt.Text, Constants.vbCrLf) > 0)
                    {
                        string[] comments = Strings.Split(cmt.Text, Constants.vbCrLf);
                        foreach (string comment in comments)
                        {
                            if (settings.ResultOut_SuppressNTEs == true)
                            {
                                interfaceReport.AddNTE(new NTE(Strings.RTrim(comment)), _oSuppress);
                            }
                            else
                            {
                                interfaceReport.AddNTE(new NTE(Strings.RTrim(comment)));
                            }
                        }
                    }
                    else if (settings.ResultOut_SuppressNTEs == true)
                    {
                        interfaceReport.AddNTE(new NTE(Strings.RTrim(cmt.Text)), _oSuppress);
                    }
                    else
                    {
                        interfaceReport.AddNTE(new NTE(Strings.RTrim(cmt.Text)));

                    }

                    aCount += 1;
                }
            }

        }

        private bool AddPanelResultToReport(Bioreference.LIS.Order lisOrder, Bioreference.LIS.Report lisReport, Bioreference.LIS.ReportAnalytePanel p, Bioreference.ResultService.DI.Interface.DIResult interfaceResult, Bioreference.ResultService.DI.Interface.ReportAnalyte discreteAnalyte, bool tabularNTEHeadingCreated, bool isReportable, string orderedTestCode, bool processIfPanelLikeTest)
        {

            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; PanelCode: {PanelCode}; PanelIsReportable: {PanelIsReportable}; IsReportable: {IsReportable}", "ReportOut", "AddPanel", "Adding panel.", accessionNumber, p.PanelCode, p.Panel.IsReportable, isReportable);

            Bioreference.ResultService.DI.Interface.Report bdiRpt;
            bdiRpt = GetPanelResultFromObject(lisOrder, lisReport, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, orderedTestCode, processIfPanelLikeTest);

            if (bdiRpt == null)
            {
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; PanelCode: {PanelCode}", "ReportOut", "PanelNotAdded", "Panel not added.", accessionNumber, p.PanelCode);
                return false;
            }
            else
            {
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOut", "PanelAdded", "Panel added.", accessionNumber);
            }

            // If this is an IGE report then add to the bottom
            if (bdiRpt.TestCode == settings.ResultOut_IGECode)
            {
                _rptIge = bdiRpt;
            }
            // Don't readd if already exists
            else if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                interfaceResult.rReports.Add(bdiRpt);

            return true;

        }

        private Bioreference.ResultService.DI.Interface.Report GetPanelResultFromObject(Bioreference.LIS.Order lisOrder, Bioreference.LIS.Report lisReport, Bioreference.LIS.ReportAnalytePanel lisPanel, Bioreference.ResultService.DI.Interface.DIResult bdiRslt, Bioreference.ResultService.DI.Interface.ReportAnalyte interfaceDiscreteAnalyte, bool tabularNTEHeadingCreated, string orderedTestCode, bool processIfPanelLikeTest)
        {


            // 'If a canceled allergen, do  not send to reporting.
            if (this.HasCanceledAllergen(lisPanel))
            {
                canceled = true;
                return default;
            }

            // For Tabular Text
            // '**************************************************************************
            var tabText = new List<string>();
            var tabTextNoHeaders = new List<string>();
            tabularNTEHeadingCreated = false;
            interfaceDiscreteAnalyte = default;

            // 'If OutboundChannelId is set, then we pass back the ordering code for the panel.
            string code = settings.ResultOut_ChannelId == 0 ? lisPanel.PanelCode : lisPanel.OrderingPanelCodes[0].ToString();
            string name = lisPanel.Panel.Name;

            // 'Alternate Outbound***************************************
            if (object.ReferenceEquals(lisPanel.Panel.GetType(), typeof(RefAnalytePanel)))
            {
                RefAnalytePanel refa = (RefAnalytePanel)lisPanel.Panel;
                if (!refa.AltOutboundTestCode.Equals(""))
                {
                    code = refa.AltOutboundTestCode;
                }
                if (!refa.AltOutboundDescription.Equals(""))
                {
                    name = refa.AltOutboundDescription;
                }
            }
            // **********************************************************

            string[] testInfo;

            // mzia: look for panelLike tests e.g. Allergens
            // **************************************************
            string panelLikeTest = GetPanelLikeTest(lisPanel.Panel.Category);
            Bioreference.ResultService.DI.Interface.Report bdiRpt = default;
            var isPanelLikeTest = default(bool);
            if (panelLikeTest.Contains("^") && processIfPanelLikeTest)
            {

                testInfo = panelLikeTest.Split('^');
                bdiRpt = bdiRslt.FindReport(testInfo[0]);

                name = testInfo[1];
                orderedTestCode = testInfo[0];
                // orderedTestCode = code
                isPanelLikeTest = true;
            }
            // **************************************************

            Bioreference.LIS.resultStatusType status;
            if (isPanelLikeTest)
            {
                status = lisPanel.Parent.GetGroupStatusForReportingByCategory(name);
            }
            else
            {
                status = lisPanel.Parent.GetGroupStatusForReporting(orderedTestCode);
            }

            if (bdiRpt == null)
            {
                if (!string.IsNullOrEmpty(orderedTestCode))
                    bdiRpt = bdiRslt.FindReport(orderedTestCode);
                if (bdiRpt == null)
                {

                    Bioreference.LIS.OrderTest ot = lisOrder.Tests.Find(code, orderedTestCode);
                    string obrName = "";
                    if (!(ot == null))
                    {
                        obrName = ot.OrderedTestName;
                    }
                    else
                    {
                        // 'If created from a reflex will not have an OrderTest - need to get test name from another.
                        obrName = lisOrder.Tests.FindOrderedTestName(orderedTestCode);
                    }
                    if (obrName.Equals(""))
                        obrName = name;

                    bdiRpt = new Bioreference.ResultService.DI.Interface.Report(orderedTestCode, default, obrName, accessionNumber, ResultService.Common.Utilities.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, lisPanel.GetLatestResultDate().ToString(), lisPanel.Panel.Category, ResultService.Common.Utilities.ConvertStatusToString(status));

                    // 'If the ordered code is not an actual analyte, then add suppress
                    if (lisPanel.Parent.FindAnalyte(orderedTestCode, false) == null)
                    {
                        bdiRpt.AddSuppress = true;
                    }

                }
            }

            if (!(FindAnalyte(bdiRpt, code) == null))
                return default; // 'Already exists in OBR, do not readd.

            // 'If status is blank or current status is pending, then set.
            // 'If status = LIS.resultStatusType.Pending OrElse bdiRpt.ReportStatus.Equals("") Then
            bdiRpt.ReportStatus = ResultService.Common.Utilities.ConvertStatusToString(status);
            // 'End If

            // For Terse analytes - we add them as a comment on the last Discrete Analyte.
            // '*************************************************************************
            string analytesAsNotes = "";
            string lastDiscreteAnalyteCode = "";
            Bioreference.ResultService.DI.Interface.ReportAnalyte lastDiscreteAnalyte = default;
            LIS.ReportAnalyte lastLisReportAnalyte = default;

            if (!lisPanel.Panel.IsReportable)
            {

                // 'bdiRslt.rReports.Add(bdiRpt)  ''OBR with no OBXs
                return bdiRpt;
            }

            else
            {

                bool generateObx = true;  // 'If Lenetix = true and all Analyte outbound test code <> panelcode, then keep true.
                bool isAllergenPanel = lisPanel.Panel.Category.ToLower().Equals("allergen");

                if (!settings.Lenetix_TestCodes.Contains(lisPanel.PanelCode))
                {
                    generateObx = false;
                }

                // We first need to loop through all analytes and find the last Discrete analyte to add Terse comments to
                // We also use it to add panel comments to. Also, use altoutboundCode to determine if we need to generate a dummy obx record.
                // 3/27/2013 - see if all results are the same - if so, send out one OBX
                // 4/3/2013 - REMOVED ResultsSame functionality
                // Dim areResultsSame As Boolean = False  '(lisPanel.GetStatus() <> LIS.resultStatusType.Corrected) 'If corrected then don't send as Same Result
                // Dim resultSameValue As String = ""

                foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                {

                    // ''' Dim adi As New DI.Interface.ReportAnalyte(a.Code, a.ResultValue, DateTime.Now, "", "", "", "", "", "", "", "", "", a.PerformingFacility, a.AccessioningFacility)
                    if (a.TransmitStatus == transmitStatusType.Released && a.ResultValue != "" && a.Analyte.IsReportable && a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.Discrete && !IsBypassNoBillCode(a.ResultValue, lisPanel.PanelCode, a.Analyte.Code, a.PerformingFacility, bdiRpt, lisReport))
                    {
                        lastDiscreteAnalyteCode = a.Analyte.Code;
                        // If areResultsSame AndAlso (resultSameValue = "" OrElse a.ResultValue = resultSameValue) AndAlso _
                        // My.Settings.ResultOut_PanelCommonResult.Contains(a.ResultValue) Then
                        // resultSameValue = a.ResultValue
                        // Else
                        // areResultsSame = False
                        // End If
                    }

                    if (((RefAnalyte)a.Analyte).AltOutboundTestCode == lisPanel.PanelCode)
                    {
                        generateObx = false;
                    }

                }
                // **************************************************************************

                bool flagDiscrete = false; // 'If tabular text and analyte is flagged, use this to flag parent obx
                string resultValue = "";


                // If areResultsSame Then

                // 'Set it to the last discrete analyte so that comments, etc. can be added to it
                // lastDiscreteAnalyte = New Bioreference.DI.Interface.ReportAnalyte(lisPanel.PanelCode, resultSameValue, _
                // lisPanel.ReleaseDate.ToString(), "", "ST", lisPanel.PanelName, _
                // "", "", "F", lisPanel.Panel.ReferenceLabId, "", "")

                // For Each a As Bioreference.LIS.ReportAnalyte In lisPanel.Analytes.List
                // If a.HasBeenReleased() AndAlso a.Comments.List.Count > 0 Then
                // If Not isAllergenPanel Then
                // ProcessAnalyteComments(a, lastDiscreteAnalyte)
                // Else
                // ProcessAllergenComments(a, lastDiscreteAnalyte, bdiRpt)
                // End If
                // End If
                // Next
                // bdiRpt.AddAnalyte(lastDiscreteAnalyte)

                // Else


                // resultValue = IIf(Not a.HasBeenReleased(), "Pending", a.ResultValue)

                var reportDate = default(DateTime);
                foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                {
                    resultValue = ResultService.Common.Utilities.UpdateResultToFollow(a, ref reportDate);

                    // 'For allergen Concentration/Classification - add on Classification result to Concentration result.
                    // '***************************************************************************
                    if (processIfPanelLikeTest && isAllergenPanel && ((RefAnalyte)a.Analyte).AltOutboundTestCode == lisPanel.PanelCode && a.Analyte.Category.ToLower().Equals("allergen"))
                    {

                        foreach (LIS.ReportAnalyte classA in lisPanel.Analytes.List)
                        {
                            if (!object.ReferenceEquals(classA, a) && ((RefAnalyte)classA.Analyte).AltOutboundTestCode == "" && classA.Analyte.Category.ToLower().Equals("allergen") && classA.ResultValue != "")
                            {
                                resultValue = string.Concat(resultValue, "~", classA.ResultValue);
                                break;
                            }
                        }

                    }
                    // '***************************************************************************

                    // AG 9/17/2008 - Added this since Analytes can now be optional (DisplayByDefault = false)
                    // We don't want to sent the analyte if it was not assigned a value thus not released.
                    // Also add ResultValue <> "" condition, should never send blank results.
                    // AG 10/20/2008 Also must be Reportable
                    // VCC 4/23/2015 Included in statement the condition for panel to follow not sent and analyte reportable
                    if ((a.HasBeenReleased() || a.Analyte.Category.ToLower().Equals("allergen")) && !string.IsNullOrEmpty(resultValue) && (a.Analyte.IsReportable || !processIfPanelLikeTest && a.Analyte.Category.ToLower().Equals("allergen")) || lisPanel.ToFollowSent == false && a.Analyte.IsReportable && !string.IsNullOrEmpty(resultValue))
                    {



                        // If type is Terse then we add it to our string of Terse analytes to then add as a comment.
                        if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.Terse)
                        {
                            analytesAsNotes += string.Concat(a.Analyte.Name, " ", resultValue, "; ");
                            continue;
                        }

                        // **************************************************************************

                        // 'This is to replace an analyte code in a one component panel with the panelcode, if setting permits.
                        // **************************************
                        string analyteCode = "";
                        if (a.Analyte.ReferenceLabId != 0 && lisPanel.Analytes.List.Count == 1)
                        {
                            var refLab = Bioreference.Common.Lab.ReferenceLab.Fetch(a.Analyte.ReferenceLabId);
                            if (!(refLab == null))
                            {
                                if (refLab.UsePanelCodeOneComponent)
                                {
                                    analyteCode = lisPanel.PanelCode;
                                }
                                else
                                {
                                    analyteCode = a.Analyte.Code;
                                }
                            }
                            else
                            {
                                analyteCode = a.Analyte.Code;
                            }
                        }
                        else
                        {
                            analyteCode = a.Analyte.Code;
                        }
                        // **************************************

                        string analyteName = a.Analyte.Name;

                        // 'Alternate Outbound***************************************
                        if (object.ReferenceEquals(a.Analyte.GetType(), typeof(RefAnalyte)))
                        {
                            RefAnalyte refa = (RefAnalyte)a.Analyte;
                            if (!refa.AltOutboundTestCode.Equals(""))
                            {
                                analyteCode = refa.AltOutboundTestCode;
                            }
                            if (!refa.AltOutboundDescription.Equals(""))
                            {
                                analyteName = refa.AltOutboundDescription;
                            }
                        }
                        // **********************************************************

                        // if the test code is in the List of Tests Needing to be sent as ST,
                        bool sendAoeAsStType = settings.SendAsSTResultType.Contains(code);

                        var anl = new Bioreference.ResultService.DI.Interface.ReportAnalyte(analyteCode, resultValue, reportDate.ToString(), default, ResultService.Common.Utilities.ConvertResultTypeToString(a.Analyte.ResultType, a.CodeTypeId == 5, sendAoeAsStType), analyteName, a.Analyte.Units, a.GetReferenceRange().Replace('\u0096', '-'), ResultService.Common.Utilities.ConvertStatusToString(a.ResultStatus), a.Analyte.ReferenceLabId.ToString(), default, a.FlagValue, a.PerformingFacility, a.AccessioningFacility, a.RefLabPerformingFacilityId); // a.GetFlaggedValue

                        anl.CocApproverEmpNumber = a.CocApproverEmpNbr;
                        anl.CocApproverFirstName = a.CocApproverFirstName;
                        anl.CocApproverLastName = a.CocApproverLastName;
                        anl.CocApproverMiddleName = a.CocApproverMiddleName;
                        anl.CocApprovedDate = a.CocApprovedDate;

                        // [EL] Include PanelCode for NoBill processing
                        anl.PanelCode = lisPanel.PanelCode;
                        anl.PanelSPMStatus = (int)lisPanel.SPMStatus;

                        if (!isAllergenPanel)
                        {
                            ProcessAnalyteComments(a, anl);
                        }
                        else
                        {
                            ProcessAllergenComments(a, anl, bdiRpt);
                        }
                        // 'vcc: 9/21/2015 Include image attachments in the message
                        ProcessAnalyteAttachments(a, anl);

                        if (a.Code == lastDiscreteAnalyteCode)
                        {
                            lastDiscreteAnalyte = anl;
                            lastLisReportAnalyte = a;
                        }

                        // mzia: if this analyte's reporting type is TabularText
                        if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText || a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                        {
                            if (generateObx || string.IsNullOrEmpty(lastDiscreteAnalyteCode))
                            {
                                interfaceDiscreteAnalyte = new Bioreference.ResultService.DI.Interface.ReportAnalyte(code, "SEE BELOW", (a.ReleaseDate > DateTime.Parse("1900-01-01") ? a.ReleaseDate.ToString() : DateTime.Now.ToString("MM/dd/yyyy")), "", "ST", name, "", "", "F", anl.ReferenceLabNumber, "", "", "", "", a.RefLabPerformingFacilityId);
                            }
                            if (!tabularNTEHeadingCreated && a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                            {
                                tabText.Add(string.Concat(testDescriptionTitle, resultTitle, flagTitle, referenceRangeTitle, unitsTitle));
                                tabularNTEHeadingCreated = true;
                            }

                            var notes = ResultService.Common.Utilities.CreateNoteFromAnalyte(a);

                            if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                            {
                                tabText.AddRange(notes);
                                foreach (NTE nte in anl.NTEs)
                                    tabText.Add(settings.TabularTextNTEPrefix + nte.Text);
                            }
                            else if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                            {
                                tabTextNoHeaders.AddRange(notes);
                                foreach (NTE nte in anl.NTEs)
                                    tabTextNoHeaders.Add(settings.TabularTextNTEPrefix + nte.Text);
                            }

                            if (a.FlagValue != "")
                                flagDiscrete = true;
                        }

                        else
                        {
                            bdiRpt.AddAnalyte(anl);
                        }

                    }
                }

                // End If

                if (!(interfaceDiscreteAnalyte == null))
                {
                    if (flagDiscrete)
                        interfaceDiscreteAnalyte.AddFlag("*");
                    bdiRpt.AddAnalyte(interfaceDiscreteAnalyte);
                    lastDiscreteAnalyte = interfaceDiscreteAnalyte;
                }
                if (!(lastDiscreteAnalyte == null))
                {
                    if (tabTextNoHeaders.Count > 0)
                    {
                        foreach (string s in tabTextNoHeaders)
                            lastDiscreteAnalyte.AddNTE(new NTE(s));
                    }
                }

                if (tabText.Count > 0)
                {
                    foreach (string s in tabText)
                        lastDiscreteAnalyte.AddNTE(new NTE(s));
                }

                // Once we finish looping through all the analytes, we attach all Terse analytes to the
                // last analyte in the panel.
                if (!(lastDiscreteAnalyte == null) && analytesAsNotes.Length > 2)
                {
                    analytesAsNotes = analytesAsNotes.Substring(0, analytesAsNotes.Length - 2); // Remove last " ;"
                    string[] comments = Strings.Split(ResultService.Common.Utilities.FormatFixedWidthText(analytesAsNotes, 78, ';'), Constants.vbCrLf);
                    foreach (string s in comments)
                        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(s.Trim(' '))));
                }

                bool IsTNPAutoComment = false;
                bool IsATPAutoComment = false;

                // We add the panel comments to the last obx record for reporting.
                // *****************************************************************
                if (lastDiscreteAnalyte is not null)
                {
                    foreach (ReportComment cmt in lisPanel.Comments.List)
                    {

                        // Blocking TNP auto comments not to send to reporting, but auto comment is TNP comment system let go
                        IsTNPAutoComment = CheckTNPComment(cmt.Text);
                        IsATPAutoComment = CheckATPComment(cmt.Text);
                        if (lastLisReportAnalyte is not null && SharedFunctions.IsTNP(lastLisReportAnalyte.ResultValue) && cmt.IsAutoAdded && !IsTNPAutoComment)
                        {
                            continue;
                        }
                        if (lastLisReportAnalyte is not null && SharedFunctions.IsATP(lastLisReportAnalyte.ResultValue) && cmt.IsAutoAdded && !IsATPAutoComment)
                        {
                            continue;
                        }

                        // mzia: Panel Comments
                        // mzia: if cmt.ExternalCommentCode is not empty then add to comment code to OBX otherwise add NTEs
                        if (cmt.ExternalCommentCode != "")
                        {
                            lastDiscreteAnalyte.CommentIdMappings.Add(cmt.ExternalCommentCode + "^" + ((int)cmt.ExternalCommentType).ToString());
                        }
                        else
                        {
                            // If not the first note, then put a blank line
                            if (lastDiscreteAnalyte.NTEs.Count > 0)
                            {
                                if (lastDiscreteAnalyte.NTEs[lastDiscreteAnalyte.NTEs.Count - 1].Text.Trim() != "")
                                    lastDiscreteAnalyte.AddNTE(new NTE(""));
                            }

                            if (Strings.InStr(cmt.Text, Constants.vbCrLf) > 0)
                            {
                                string[] comments = Strings.Split(cmt.Text, Constants.vbCrLf);
                                foreach (string comment in comments)
                                {
                                    if (settings.ResultOut_SuppressNTEs == true)
                                    {
                                        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId), _oSuppress);
                                    }
                                    else
                                    {
                                        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId));
                                    }
                                }
                            }
                            else if (settings.ResultOut_SuppressNTEs == true)
                            {
                                lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId), _oSuppress);
                            }
                            else
                            {
                                lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId));
                            }
                        }
                    }
                }
                // *****************************************************************

            }

            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "ReportOut", "PanelResult", "Panel result TestCode processed.", accessionNumber, bdiRpt.TestCode);
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; AnalyteCount: {AnalyteCount}", "ReportOut", "PanelResult", "Panel result analyte count processed.", accessionNumber, bdiRpt.AnalyteList.Count);

            // mzia: TabularText reports does not have analytes/OBXs they have rather NTEs
            if (!lisPanel.Panel.IsReportable || bdiRpt.AnalyteList.Count > 0 || bdiRpt.NTEs.Count > 0)
            {
                return bdiRpt;
            }
            else
            {
                if (!canceled)
                {
                    logger.LogError($"{bdiRpt.AccessionNum} - '{bdiRpt.TestCode}' panel does not have any released analytes.  Panel may be marked as Released in error.");
                }
                return default;
            }
        }

        /// <summary>
        /// Create a DI Report ready to create one OBR for one allerge profile and all the OBX of the allergen for that profile
        /// </summary>
        /// <param name="analyteCode"></param>
        /// <param name="lisReport"></param>
        /// <param name="lisOrder"></param>
        /// <returns>DI Report</returns>
        private Bioreference.ResultService.DI.Interface.Report CreateAllergenDIReport(string analyteCode, DI.Interface.Report bdiRpt, Bioreference.LIS.Report lisReport, Bioreference.LIS.Order lisOrder)
        {
            try
            {
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; AnalyteCode: {AnalyteCode}", "ReportOut", "CreateAllergenDIReport", "Creating allergen DI report.", accessionNumber, analyteCode);
                if (lisReport == null)
                    return null;
                if (!(bdiRpt == null))
                {
                    if (!(this.FindAnalyte(bdiRpt, analyteCode) == null))
                        return null; // 'Already exists in OBR, do not readd.
                }
                Bioreference.ResultService.DI.Interface.Report allergenRpt = default;
                Bioreference.LIS.ReportAnalyte analyte = FindOrderedAnalyte(analyteCode, lisReport);
                Bioreference.Common.TestMaster.TestInfo ti = TestMasterWrapper.Fetch(analyteCode, order: ref lisOrder);
                if (analyte == null)
                    return null;
                if (ti == null)
                    return null;
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; AnalyteCode: {AnalyteCode}", "ReportOut", "CreateAllergenDIReport", "Allergen analyte found.", accessionNumber, analyte.Code);
                allergenRpt = new Bioreference.ResultService.DI.Interface.Report(analyteCode, lisOrder.Comments, ti.Name, accessionNumber, ResultService.Common.Utilities.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, analyte.ResultDate.ToString(), analyte.Analyte.Category, ResultService.Common.Utilities.ConvertStatusToString(analyte.ResultStatus));
                Bioreference.ResultService.DI.Interface.ReportAnalyte anl;
                foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
                {
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Code: {Code}", "ReportOut", "CreateAllergenDIReport", "Processing allergen code.", accessionNumber, a.Code);
                    if (a.Analyte.IsReportable && a.Analyte.Category.ToLower().Equals("allergen"))
                    {
                        if (!(bdiRpt == null))
                        {
                            if (!(this.FindAnalyte(bdiRpt, analyteCode) == null))
                                continue;
                        }
                        // If Not a.ResultValue.ToString().Trim().Equals("") AndAlso a.ResultStatus > 1 Then
                        // a.MarkAsReleased()
                        // logger.LogInformation(String.Concat("adding:", a.Code))
                        anl = new Bioreference.ResultService.DI.Interface.ReportAnalyte(a.Code, a.ResultValue, a.ReleaseDate.ToString(), default, ResultService.Common.Utilities.ConvertResultTypeToString(a.Analyte.ResultType), a.Analyte.Name, a.Analyte.Units, a.GetReferenceRange(), ResultService.Common.Utilities.ConvertStatusToString(a.ResultStatus), a.Analyte.ReferenceLabId.ToString(), default, a.FlagValue, a.PerformingFacility, a.AccessioningFacility, a.RefLabPerformingFacilityId);
                        ProcessAnalyteComments(a, anl);
                        allergenRpt.AddAnalyte(anl);
                        // End If
                    }
                }
                return allergenRpt;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        private LIS.ReportAnalyte FindOrderedAnalyte(string orderedCode, Bioreference.LIS.Report rpt)
        {
            if (rpt == null)
                return default;
            foreach (LIS.ReportAnalyte a in rpt.Analytes.List)
                if (a.OrderingCodes.Contains(orderedCode))
                    return a;
            return default;
        }

        private Bioreference.ResultService.DI.Interface.Report CreateIge(Bioreference.LIS.Report lisReport, Bioreference.LIS.Order lisOrder)
        {
            Bioreference.ResultService.DI.Interface.Report interfaceReportIge = default;
            // Create IGE Serum Analyte here
            Bioreference.LIS.ReportAnalyte analyteIge = lisReport.FindAnalyte(settings.ResultOut_IGECode, true);
            if (analyteIge is not null && analyteIge.TransmitStatus != transmitStatusType.Released)
            {
                string igeCode = Conversions.ToString(settings.ResultOut_ChannelId == 0 ? analyteIge.Analyte.Code : analyteIge.OrderingAnalyteCodes[0]);
                string resultValue = !analyteIge.HasBeenReleased() ? "Pending" : analyteIge.ResultValue;
                interfaceReportIge = new Bioreference.ResultService.DI.Interface.Report(igeCode, lisOrder.Comments, analyteIge.Analyte.Name, accessionNumber, ResultService.Common.Utilities.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, analyteIge.ResultDate.ToString(), analyteIge.Analyte.Category, ResultService.Common.Utilities.ConvertStatusToString(analyteIge.ResultStatus));
                var anlIge = new Bioreference.ResultService.DI.Interface.ReportAnalyte(igeCode, resultValue, analyteIge.ReleaseDate.ToString(), default, ResultService.Common.Utilities.ConvertResultTypeToString(analyteIge.Analyte.ResultType), analyteIge.Analyte.Name, analyteIge.Analyte.Units, analyteIge.GetReferenceRange(), ResultService.Common.Utilities.ConvertStatusToString(analyteIge.ResultStatus), analyteIge.Analyte.ReferenceLabId.ToString(), default, analyteIge.FlagValue, analyteIge.PerformingFacility, analyteIge.AccessioningFacility, analyteIge.RefLabPerformingFacilityId); // _analyte_IGE.GetFlaggedValue)
                ProcessAnalyteComments(analyteIge, anlIge);
                interfaceReportIge.AddAnalyte(anlIge);
            }
            return interfaceReportIge;
        }

        public bool IsCanceledAllergen(Bioreference.LIS.ReportAnalyte analyte)
        {
            return (analyte.ResultValue.ToLower().StartsWith("cancel") && analyte.Analyte.Category.ToLower().Equals("allergen"));
        }

        public bool HasCanceledAllergen(ReportAnalytePanel panel)
        {
            foreach (Bioreference.LIS.ReportAnalyte a in panel.Analytes.List)
                if (IsCanceledAllergen(a))
                    return true;
            return false;
        }

        private Bioreference.ResultService.DI.Interface.ReportAnalyte FindAnalyte(Bioreference.ResultService.DI.Interface.Report rpt, string code)
        {
            if (rpt == null)
                return null;
            foreach (Bioreference.ResultService.DI.Interface.ReportAnalyte r in rpt.AnalyteList)
                if (r.Code == code)
                    return r;
            return null;
        }
    }


}
