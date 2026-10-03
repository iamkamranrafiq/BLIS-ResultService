using Bioreference.Common;
using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.ApiClient;
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
using System.Data.SqlClient;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Bioreference.ResultService.Abstractions.Application.Payload;
using DocumentFormat.OpenXml.Bibliography;
using log4net.Repository.Hierarchy;
using log4net;
using Microsoft.IdentityModel.Tokens;
using Bioreference.Common.Lab;
using ReportAnalyte = Bioreference.LIS.ReportAnalyte;
using DocumentFormat.OpenXml.Office.CustomUI;
using Bioreference.Common.Client;
using DocumentFormat.OpenXml.Wordprocessing;
using Bioreference.Common.TestMaster;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Bioreference.ResultService.Application.Payload
{
    public class ORUPayloadService : IORUPayloadService
    {
        private ILogger<ORUPayloadService> logger;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReportOut appSettings;
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
        private int pidCounter = 1;
        private string accessionNumber;
        private string mappingFileName = "ORU_Mapping.json";

        public ORUPayloadService(ILogger<ORUPayloadService> logger, ISettingService settingsProvider, IOptions<AppSettingsReportOut> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }

        async Task<string> IORUPayloadService.Message(string accessionNbr, DateTime dateServiced)
        {
            string ORUMessages = string.Empty;
            bool isReportable = true;
            Bioreference.LIS.Report blReport = Bioreference.LIS.Report.Fetch(accessionNbr, dateServiced);
            if (!(blReport == null) && blReport.ID > 0)
            {
                try
                {
                    //Generate Message
                    ORUMessages = GenerateMessage(blReport, isReportable);
                    logger.LogInformation("({Accession}): IORUPayload_ORUMessage='{Payload}'", accessionNumber, string.Join(Environment.NewLine + "---" + Environment.NewLine, string.IsNullOrEmpty(ORUMessages)?"": ORUMessages));

                    if (ORUMessages == null || ORUMessages.Length == 0)
                    {
                        logger.LogWarning($"({accessionNumber}): Empty message generated");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"({accessionNumber}): ORUMessage Failed='{ex.Message}'");
                }
            }
            return ORUMessages;
        }

        public string GenerateMessage(Bioreference.LIS.Report blReport, bool isReportable)
        {
            string msg = string.Empty;
            logger.LogInformation($"({blReport.AccessionNbr}): GenerateMessage HasValidPrefix='HasReleased='{blReport.HasReleased()}' sendToFollow='{sendToFollow}'");


            Bioreference.ResultService.DI.Interface.DIResult rslt = GetResultFromObject(blReport, isReportable);
            if (!(rslt == null))
            {
                msg = CreateMessage(rslt, isReportable, blReport);
            }

            logger.LogInformation($"({accessionNumber}): GenerateMessage: No message generated.");
            return msg;
        }


        /// Create a HL7 message from the Results and Report
        private string CreateMessage(Bioreference.ResultService.DI.Interface.DIResult result, bool isReportable, Bioreference.LIS.Report blReport)
        {
            string hl7Message = string.Empty;
            var timestamp = blReport.DateServiced.ToString("yyyyMMddHHmmss");
            var oMessage = new Bioreference.ResultService.DI.Interface.ORUMessage(); //new ORUMessage(this);
            oMessage.PatientInfoComments = new List<PatientComment>();
            oMessage.LabReport = new List<LabReport>();
            string sLastName = string.Empty;
            string sFirstName = string.Empty;
            string sSex = string.Empty;
            string sOrderdate = string.Empty;
            string sCollectDate = string.Empty;
            string sDocName = string.Empty;
            string sTestCode = string.Empty;

            try
            {
                // ---------- Common MSH ----------
                logger.LogInformation($"({accessionNumber}): MSH");
                oMessage.MSH = new MSH()
                {
                    MessageType = "ORU",
                    MessageEvent = "R01",
                    SendingApplication = "DI",
                    SendingFacilityCode = "BRLI",
                    //SendingFacility      = "",
                    ReceivngApplication = "B2",
                    ReceivingFacility = "BRLI",
                    MessageTime = timestamp,
                    MessageID = timestamp,                    
                    ProcessingId = "P",
                    VersionId = Convert.ToString(2.3),
                    //AcceptAckType        = "AL",
                    SequenceNumber       = Convert.ToString(blReport.OrderId)
                };


                Bioreference.LIS.Order bOrder = Bioreference.LIS.Order.FetchBySPMOrderId(blReport.OrderId);
                string dobString = bOrder.Patient.DateOfBirth;
                int age = 0;

                if (DateTime.TryParse(dobString, out DateTime dob))
                {
                    age = DateTime.Today.Year - dob.Year;

                    if (dob.Date > DateTime.Today.AddYears(-age))
                        age--;
                }

                //PID
                oMessage.PatientInfo = new PatientInfo()
                {
                    Set_ID = pidCounter.ToString(),
                    ClientMRN = result.rPatient.Id,
                    PatientIdentifierList = result.rPatient.IdentifierList,
                    LastName = result.rPatient.LastName,
                    FirstName = result.rPatient.FirstName,
                    MiddleName = result.rPatient.MiddleName,
                    SocSecNum = result.rPatient.SocSecNum,
                    SSN = result.rPatient.SocSecNum,
                    DOB = result.rPatient.DOB,
                    Sex = result.rPatient.Sex
                    //Address1 = result.rPatient.Street1,
                    //Address2 = result.rPatient.Street2,
                    //City = result.rPatient.City,
                    //State = result.rPatient.State,
                    //Zip = result.rPatient.Zip,
                    //HomePhone = result.rPatient.HomePhone,
                    //WorkPhone = result.rPatient.WorkPhone,
                    //SSN = result.rPatient.SocSecNum,
                    //DoctorReference = result.rPatient.PhysicianAcct,
                    //UpdateTrackingId = result.rPatient.UpdateTrackingId
                    //PatientAccountNumber = ""
                };

                // PV1                
                logger.LogInformation($"({accessionNumber}): PV1");
                oMessage.PatientVisit = new PatientVisit()
                {
                    Set_ID = "1",
                    PatientClass = result.rPatientVisit.PatientClass,
                    PatientLocation = result.rPatientVisit.PatientLocation
                };

                // PID NTEs                
                logger.LogInformation($"({accessionNumber}): PID NTE");
                if (result.rPatient.NTEs.Count > 0)
                {
                    logger.LogInformation($"({accessionNumber}): PID NTE Count='{result.rPatient.NTEs.Count}'");
                    int rowCount = 1;
                    int currentGroupId = 0;
                    oMessage.PatientInfoComments = new List<PatientComment>();
                    foreach (NTE n in result.rPatient.NTEs)
                    {
                        if (n.GroupId != currentGroupId)
                            rowCount = 1;
                        logger.LogInformation($"({accessionNumber}): Patient NTE='{n.Text}'");
                        var patientInfoComment = new PatientComment()
                        {
                            NoteId = n.GroupId != 0 ? string.Concat(n.GroupId, ",", rowCount) : rowCount.ToString(),
                            NoteText = n.Text.TrimEnd()
                        };
                        oMessage.PatientInfoComments.Add(patientInfoComment);
                        currentGroupId = n.GroupId;
                        rowCount += 1;
                    }
                }

                // 'ORC            
                logger.LogInformation($"({accessionNumber}): ORC");
                oMessage.Order = new Bioreference.ResultService.DI.Interface.Order()
                {
                    OrderControlId = "NW"
                    //AccessionNumber = result.rPatient.IdentifierList,
                    //OrderStatus = result.OrderStatus,
                    //OrderDate = result.ServiceDate.DateTimeStamp(),
                    //HasComments = "COMM",
                    //ConfidentialityCode = blReport.IsCOC ? "R" : string.Empty
                };


                // ''ORC notes            
                logger.LogInformation($"({accessionNumber}): ORC NOTES");
                if (!(result.NTEs == null))
                {
                    if (oMessage.PatientInfoComments == null) oMessage.PatientInfoComments = new List<PatientComment>();
                    for (int i = 0, loopTo = result.NTEs.Count - 1; i <= loopTo; i++)
                    {
                        logger.LogInformation($"({accessionNumber}): ORC NoteText='{result.NTEs[i].Text}'");
                        oMessage.PatientInfoComments.Add(new PatientComment() { NoteId = (i + 1).ToString(), NoteText = result.NTEs[i].Text.TrimEnd() });
                    }
                }


                // OBR
                logger.LogInformation($"({accessionNumber}): OBR");
                //     result.rReports = new List<DI.Interface.Report>();
                if (result.rReports.Count > 0)
                {
                    logger.LogDebug($"({accessionNumber}): Processing OBR Count='{result.rReports.Count}'");
                    var containsTestCode = default(bool);
                    int obrCount = 0;
                    foreach (Bioreference.ResultService.DI.Interface.Report rpt in result.rReports)
                    {
                        var obr = new OBR();
                        obr.Set_ID = (obrCount + 1).ToString();
                        obr.AccessionNum = result.rPatient.IdentifierList;
                        obr.FillerOrderNumber = rpt.AccessionNum;
                        sTestCode = rpt.TestCode;
                        obr.TestCode = rpt.TestCode;
                        //withBlock5.SetTestDescription(rpt.TestDescription);
                        obr.TestDescription = rpt.TestDescription;
                        //withBlock5.SetPriority(rpt.Priority);
                        obr.Priority = rpt.Priority;
                        sOrderdate = rpt.OrderDate;
                        sCollectDate = rpt.CollectDate;
                        if (Information.IsDate(sOrderdate))
                            obr.RequestDate = sOrderdate.DateTimeStamp();
                        if (Information.IsDate(sCollectDate))
                            obr.CollectDate = sCollectDate.DateTimeStamp();
                        obr.CollectedBy = rpt.Collector;
                        if (Information.IsDate(rpt.ReceiveDate))
                            obr.ReceivedDate = rpt.ReceiveDate.DateTimeStamp();
                        obr.ProviderCode = rpt.PhysCode;
                        obr.ProviderLastName = rpt.PhysLastName;
                        obr.ProviderFirstName = rpt.PhysFirstName;
                        sDocName = rpt.PhysLastName;                        
                        obr.CallBackPhone = rpt.CallBackPhone;
                        bool isLenetix = false;
                        //if (!(settings.Lenetix_TestCodes == null))
                        //{
                        //    if (settings.Lenetix_TestCodes.Contains(rpt.TestCode))
                        //    {
                        //        isLenetix = true;
                        //    }
                        //}
                        //if (!(settings.TestCodes == null))
                        //{
                        //    if (settings.TestCodes.Contains(sTestCode))
                        //    {
                        //        containsTestCode = true;
                        //    }
                        //}

                        string panelLikeTest = "";
                        if (!isLenetix & string.IsNullOrEmpty(panelLikeTest))
                        {
                            if (containsTestCode)
                            {
                                obr.IsPanel = "SUPPRESS";
                            }
                            else
                            {
                                obr.IsPanel = "";
                            }
                        }
                        else
                        {
                            obr.IsPanel = "";
                        }

                        if (Information.IsDate(rpt.ResultDate))
                        {
                            obr.ResultDate = rpt.ResultDate.DateTimeStamp();
                        }
                        obr.DiagID = rpt.DiagnosticService;
                        obr.LabReportStatus = rpt.ReportStatus;

                        // OBR NTEs
                        List<LabComment> obrComments = new List<LabComment>();
                        if (rpt.NTEs.Count > 0)
                        {
                            obrComments = new List<LabComment>();
                            logger.LogInformation($"({accessionNumber}): Processing OBR NTE Count={rpt.NTEs.Count}");
                            var commentCount = 0;
                            foreach (NTE n in rpt.NTEs)
                            {
                                commentCount++;
                                logger.LogInformation($"({accessionNumber}): OBR NoteText='{n.Text}'");
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
                        logger.LogInformation($"({accessionNumber}): OBX");
                        List<Bioreference.ResultService.DI.Interface.Result> results = null;
                        if (rpt.AnalyteList.Count > 0)
                        {
                            results = new List<Bioreference.ResultService.DI.Interface.Result>();
                            logger.LogInformation($"({accessionNumber}): Processing OBX Count='{rpt.AnalyteList.Count}' TestCode='{rpt.TestCode}' OrderedCode='{rpt.OrderedCode}' ParentCode='{rpt.ParentCode}'");
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
                                logger.LogInformation($"({accessionNumber}): isProfileTNPed='{isProfileTNPed}'; isProfileATPed='{isProfileATPed}'");
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

                            logger.LogInformation($"({accessionNumber}): isProfileTNPed='{isProfileTNPed}'");

                            var obxs = new List<OBX>();
                            foreach (Bioreference.ResultService.DI.Interface.ReportAnalyte a in rpt.AnalyteList)
                            {
                                logger.LogInformation($"({accessionNumber}): Add code to message");

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

                                var obx = new OBX();
                                obx.Set_ID = count.ToString();
                                obx.ValueType = a.ValueType;
                                if (!(analyte == null) && analyte.InstrumentId != "N/A")
                                {
                                    obx.InstrumentId = analyte.InstrumentId; // analyte.PerformingFacility; // analyte.InstrumentId; //TODO
                                    obx.PerformLocation = analyte.PerformingFacility;
                                }
                                if (a.ValueType.Equals(AeStringValue))
                                {
                                    _aeList.Add(a.Code);
                                }

                                obx.ReportDate = a.ReportDate.DateTimeStamp();

                                if (!(analyte == null) && analyte.IsPresumptiveHold)
                                {
                                    obx.PresumptiveHold = "PH";
                                }
                                obx.Code = a.Code;
                                string panelLikeTest1 = "";
                                addSuppress = rpt.AddSuppress;

                                if (obx.ReferenceLabId != "0" && a.Code == rpt.TestCode)
                                {
                                    addSuppress = false;
                                }

                                obx.Description = a.Description;
                                obx.Value = a.Value;
                                obx.Units = a.Units;

                                string sRefRange = a.ReferenceRange.Replace(Convert.ToChar(150), '-');
                                obx.ReferenceRange = sRefRange;

                                if (a.Value != "TO FOLLOW")
                                {
                                    obx.Flag = a.Flag;
                                }
                                obx.Status = a.Status;
                                if (a.ReferenceLabNumber == "0")
                                {
                                    obx.PerformLocation = a.PerformingFacility;
                                    obx.ProducerName = "BIO";
                                    if (count == 1)
                                        obr.AccessionLocation = a.AccessioningFacility;
                                }
                                else
                                {
                                    if (a.RefLabPerformingFacilityId != 0)
                                    {
                                        obx.ReferenceLabId = a.RefLabPerformingFacilityId.ToString();
                                    }
                                    else
                                    {
                                        obx.ReferenceLabId = a.ReferenceLabNumber;
                                    }
                                    obx.ProducerName = "SENDOUT";
                                }                                
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
                                }

                                // Coc Data set here
                                if (blReport.IsCOC)
                                {
                                    obx.ReleaseUser = a.CocApproverEmpNumber;
                                    obx.TechUserFName = a.CocApproverFirstName;
                                    obx.TechUser =  a.CocApproverLastName;
                                    obx.TechUserMName = a.CocApproverMiddleName;
                                    obx.COCReleasedDate = a.CocApprovedDate.ToString().DateTimeStamp(); 
                                }
                                else
                                {   
                                    obx.TechUser = a.TechUser;
                                }

                                // OBX NTEs
                                //List<LabComment> obxComments = null;
                                //if (a.NTEs.Count > 0)
                                //{
                                //    obxComments = new List<LabComment>();
                                //    logger.LogInformation($"({accessionNumber}): Processing OBX NTEs Count='{a.NTEs.Count}'");
                                //    int lrcCount = 1;
                                //    foreach (NTE n in a.NTEs)
                                //    {
                                //        logger.LogInformation($"({accessionNumber}): OBX NoteText='{n.Text}'");
                                //        var resultComment = new LabComment()
                                //        {
                                //            NoteId = lrcCount.ToString(),
                                //            NoteText = n.Text.TrimEnd(),
                                //            CommentCode = n.ExternalId
                                //        };
                                //        obxComments.Add(resultComment);
                                //        lrcCount++;
                                //    }
                                //}

                                // Attachment by lines                            
                                logger.LogInformation($"({accessionNumber}): OBX Attachments");
                                List<OBX> attachments = null;
                                if (!(a.Attachment == null) && a.Attachment.Count > 0)
                                {
                                    logger.LogInformation($"({accessionNumber}): Processing OBX Attachments Count='{a.Attachment.Count}'");
                                    attachments = new List<OBX>();
                                    foreach (string s in a.Attachment)
                                    {
                                        count += 1;
                                        var attachment = new OBX();
                                        attachment.Set_ID = count.ToString();
                                        attachment.ValueType = "ED";
                                        attachment.Code = a.Code;
                                        attachment.Description = a.Description;
                                        attachment.Status = a.Status;
                                        attachment.AttachmentMultipart = "multipart";
                                        attachment.AttachmentType = a.AttachmentType;
                                        attachment.AttachmentBase = "Base64";
                                        attachment.Value = s; // attachment line s
                                        attachments.Add(attachment);
                                    }
                                }
                                obxs.Add(obx);
                                if (attachments != null) obxs.AddRange(attachments);                                
                            }
                            var oResult = new DI.Interface.Result()
                            {
                                Obx = obxs
                                //ResultComment = obxComments != null ? obxComments : new()
                            };
                            results.Add(oResult);
                            logger.LogInformation($"({accessionNumber}): AddSuppress='{addSuppress}'");
                            if (addSuppress)
                                obr.IsPanel = "SUPPRESS";
                        }

                        if (results != null && results.Count >0)
                        {
                            oMessage.LabReport.Add(new LabReport
                            {
                                Obr = obr,
                                LabReportComment = obrComments,
                                Result = results != null ? results : new()
                            });

                            obrCount++;
                        }
                        else
                        {
                            logger.LogInformation($" Skipping the OBR : ({obr.TestCode})'");
                        }
                    }
                }

                hl7Message = ToHL7(oMessage);
                logger.LogInformation($"({accessionNumber}): HL7='{hl7Message}'");
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "({Accession}): CreateMessage() failed.", accessionNumber);
            }

            pidCounter = pidCounter + 1;
            logger.LogInformation($"({accessionNumber}): GenerateMessage hl7='{hl7Message}'");
            return hl7Message;
        }
        public string ToHL7(ORUMessage message)
        {
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HL7Configuration", mappingFileName);
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Mapping JSON files not found.");
            string mapping = File.ReadAllText(jsonPath);
            var json = JsonConvert.SerializeObject(message);
            var hl7Builder = new HL7Builder();
            string hl7 = hl7Builder.CreateHl7FromObject(mapping, json);
            return hl7;
        }

        static string GetLoadTimeKey(object loadTimeObj)
        {
            if (loadTimeObj == null)
                return string.Empty;

            if (loadTimeObj is DateTime)
            {
                var dt = (DateTime)loadTimeObj;
                return dt.ToString("yyyyMMddHHmmss");
            }

            if (loadTimeObj is DateTime?)
            {
                var ndt = (DateTime?)loadTimeObj;
                return ndt.HasValue
                    ? ndt.Value.ToString("yyyyMMddHHmmss")
                    : string.Empty;
            }

            return (loadTimeObj.ToString() ?? string.Empty).Trim();
        }

        static string CleanHl7(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            raw = raw.Replace(@"\\", @"\");
            raw = raw.Replace(@"\r\n", Environment.NewLine);
            raw = raw.Replace(@"\r", Environment.NewLine);

            raw = raw.Replace("\r\n", Environment.NewLine)
                     .Replace("\r", Environment.NewLine);

            return raw;
        }

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

            interfaceResult.rPatient = new Bioreference.ResultService.DI.Interface.Patient(lisOrder.Patient.PatientId, lisReport.AccessionNbr, lisOrder.Patient.LastName, lisOrder.Patient.FirstName, lisOrder.Patient.MiddleName, lisOrder.Patient.DateOfBirth, UtilitiesSSU.GenderToSex(lisOrder.Patient.Gender), lisOrder.Patient.PrimaryAddress.StreetLine1, lisOrder.Patient.PrimaryAddress.StreetLine2, lisOrder.Patient.PrimaryAddress.City, lisOrder.Patient.PrimaryAddress.State, lisOrder.Patient.PrimaryAddress.ZipCode, lisOrder.Patient.HomePhoneNumber, lisOrder.Patient.WorkPhoneNumber, lisOrder.Patient.SocialSecurityNum, default, lisOrder.AccountNumber);

            interfaceResult.rPatient.UpdateTrackingId = lisOrder.Patient.UpdateTrackingId;

            interfaceResult.rPatientVisit = new Bioreference.ResultService.DI.Interface.PatientVisit(default, lisOrder.AccountNumber);

            bool hasAllergen = false;
            bool igeTestExists = false;
            // Loop through all the analytes and check TransmitStatus.
            var isAdded = default(bool);
            foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
            {
                //if (a.TransmitStatus == transmitStatusType.Released && a.Analyte.IsReportable == isReportable || a.ToFollowSent == false)
                //// 'OrElse (Not lisReport.HasReleased() AndAlso a.HasBeenReleased())) _
                //{
                isAdded = AddAnalyteResultToReport(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, a.Code, true);



                logger.LogInformation($"({accessionNumber}): isAdded='{isAdded}' Category='{a.Analyte.Category.ToLower()}'");
                if (isAdded && a.Analyte.Category.ToLower().Equals("allergen"))
                {
                    hasAllergen = true;
                    // NA 11/14/13
                    _hasAllergen = true;
                }

                logger.LogInformation($"({accessionNumber}): Code={a.Code}");

                //}
            }

            // And loop through all the panels (ie. URINALYSIS)
            var isAdded1 = default(bool);
            foreach (ReportAnalytePanel p in lisReport.AnalytePanels.List)
            {
                //if (p.TransmitStatus == transmitStatusType.Released && p.Panel.IsReportable == isReportable || p.ToFollowSent == false)
                //{
                foreach (string s in p.OrderingPanelCodes)
                {
                    if (AddPanelResultToReport(lisOrder, lisReport, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, s, true))
                    {
                        isAdded1 = true;
                    }
                }

                //}
            }
            // ' vcc: 4/4/2017
            // ' If IgE and Allergen exists, Loop through all results And resend allergens
            // '************************************************************************
            logger.LogInformation($"({accessionNumber}): igeTestExists='{igeTestExists}'");
            if (igeTestExists)
            {
                if (_rptIge == null)
                {
                    //_rptIge = CreateIge(lisReport, lisOrder);
                }

                foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
                {
                    logger.LogInformation($"({accessionNumber}): Foreach='{a.Code}'");
                    //if (a.Analyte.IsReportable == isReportable && a.Analyte.Category.ToLower().Equals("allergen")) // 'AndAlso IsNothing(interfaceResult.FindReportAnalyte(a.Code)) Then
                    //{
                    // 'bdiRpt = GetAnalyteResultFromObject(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, "", True)
                    logger.LogInformation($"({accessionNumber}): Inside Id='{a.Code}'");
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
                    //}
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

            logger.LogInformation($"({accessionNumber}): Adding... Code='{a.Code}' Reportable='{isReportable}'");

            Bioreference.ResultService.DI.Interface.Report bdiRpt;
            bdiRpt = GetAnalyteResultFromObject(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, orderedTestCode, processIfPanelLikeTest);

            if (bdiRpt == null)
            {
                logger.LogInformation($"({accessionNumber}): Not Added... Code='{a.Code}'");
                return false;
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

            string code = lisAnalyte.Analyte.Code;
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

                    bdiRpt = new Bioreference.ResultService.DI.Interface.Report(orderedTestCode, lisOrder.Comments, obrName, accessionNumber, UtilitiesSSU.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, lisAnalyte.ResultDate.ToString(), lisAnalyte.Analyte.Category, UtilitiesSSU.ConvertStatusToString(status), lisAnalyte.ParentTestCode);

                    // '********************************************************************************************
                    // 'If the ordered code is not an actual analyte
                    Bioreference.LIS.Report report = lisAnalyte.GetParentReport();
                    if (report.FindAnalyte(orderedTestCode, false) == null)
                    {
                        // 'For these specific type of accession, we add the suppress when the group status is final
                        Bioreference.LIS.resultStatusType orderedStatus = report.GetGroupStatusForReporting(orderedTestCode);

                        if (orderedStatus == LIS.resultStatusType.Final || orderedStatus == LIS.resultStatusType.Corrected)
                        {
                            bdiRpt.AddSuppress = true;
                        }
                    }
                }

            }

            if (!(FindAnalyte(bdiRpt, code) == null))
                return default; // 'Already exists in OBR, do not readd.

            // 'If status = LIS.resultStatusType.Pending OrElse bdiRpt.ReportStatus.Equals("") Then
            bdiRpt.ReportStatus = UtilitiesSSU.ConvertStatusToString(status);
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
                string resultValue = UtilitiesSSU.UpdateResultToFollow(lisAnalyte, ref reportDate); 

                // if the test code is in the List of Tests Needing to be sent as ST,
                bool sendAoeAsStType = false;

                var interfaceAnalyte = new Bioreference.ResultService.DI.Interface.ReportAnalyte(code, resultValue, reportDate.ToString(), default, UtilitiesSSU.ConvertResultTypeToString(lisAnalyte.Analyte.ResultType, lisAnalyte.CodeTypeId == 5 ? true : false, sendAoeAsStType), name, lisAnalyte.Analyte.Units, lisAnalyte.GetReferenceRange(), UtilitiesSSU.ConvertStatusToString(lisAnalyte.ResultStatus), lisAnalyte.Analyte.ReferenceLabId.ToString(), default, lisAnalyte.FlagValue, lisAnalyte.PerformingFacility, lisAnalyte.AccessioningFacility, lisAnalyte.RefLabPerformingFacilityId); // a.GetFlaggedValue

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

                    //var notes = UtilitiesSSU.CreateNoteFromAnalyte(lisAnalyte);
                    //foreach (string s in notes)
                    //    interfaceDiscreteAnalyte.AddNTE(new NTE(s));


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
            return false;
        }

        private bool CheckATPComment(string text)
        {
            return false;
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

                logger.LogInformation($"({accessionNumber}): ProcessAnalyteComments='{cmt.Text}'");
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

                    //if (settings.ResultOut_SuppressNteAsBlockRefLabIds.Contains(lisAnalyte.Analyte.ReferenceLabId.ToString()))
                    //{

                    //    bool foundFlag = false;
                    //    foreach (string nte in _oSuppress.StringList)
                    //    {
                    //        if ((Strings.RTrim(cmt.Text.Replace(Environment.NewLine, "")) ?? "") == (nte ?? ""))
                    //        {
                    //            foundFlag = true;
                    //            break;
                    //        }
                    //    }
                    //    if (!foundFlag)
                    //    {
                    //        foreach (string s in Strings.Split(cmt.Text, Environment.NewLine))
                    //            interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(s), externalid: cmt.ExternalId));
                    //    }
                    //}

                    //else if (Strings.InStr(cmt.Text, Environment.NewLine) > 0)
                    //{
                    //    string[] comments = Strings.Split(cmt.Text, Environment.NewLine);
                    //    foreach (string comment in comments)
                    //    {
                    //        if (settings.ResultOut_SuppressNTEs == true)
                    //        {
                    //            interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId), _oSuppress);
                    //        }
                    //        else
                    //        {
                    //            interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId));
                    //        }
                    //    }
                    //}
                    //else if (settings.ResultOut_SuppressNTEs == true)
                    //{
                    //    interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId), _oSuppress);
                    //}
                    //else
                    //{
                    //    interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId));

                    //}

                    aCount += 1;

                }

            }

            foreach (NTE nt in interfaceAnalyte.NTEs)
                logger.LogInformation($"({accessionNumber}): After Processing='{nt.Text}'");

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

                    //if (settings.ResultOut_SuppressNteAsBlockRefLabIds.Contains(lisAnalyte.Analyte.ReferenceLabId.ToString()))
                    //{

                    //    bool foundFlag = false;
                    //    foreach (string nte in _oSuppress.StringList)
                    //    {
                    //        if ((Strings.RTrim(cmt.Text.Replace(Environment.NewLine, "")) ?? "") == (nte ?? ""))
                    //        {
                    //            foundFlag = true;
                    //            break;
                    //        }
                    //    }
                    //    if (!foundFlag)
                    //    {
                    //        foreach (string s in Strings.Split(cmt.Text, Environment.NewLine))
                    //            interfaceAnalyte.AddNTE(new NTE(Strings.RTrim(s)));
                    //    }
                    //}


                    //else if (Strings.InStr(cmt.Text, Environment.NewLine) > 0)
                    //{
                    //    string[] comments = Strings.Split(cmt.Text, Environment.NewLine);
                    //    foreach (string comment in comments)
                    //    {
                    //        if (settings.ResultOut_SuppressNTEs == true)
                    //        {
                    //            interfaceReport.AddNTE(new NTE(Strings.RTrim(comment)), _oSuppress);
                    //        }
                    //        else
                    //        {
                    //            interfaceReport.AddNTE(new NTE(Strings.RTrim(comment)));
                    //        }
                    //    }
                    //}
                    //else if (settings.ResultOut_SuppressNTEs == true)
                    //{
                    //    interfaceReport.AddNTE(new NTE(Strings.RTrim(cmt.Text)), _oSuppress);
                    //}
                    //else
                    //{
                    //    interfaceReport.AddNTE(new NTE(Strings.RTrim(cmt.Text)));

                    //}

                    aCount += 1;
                }
            }

        }

        private bool AddPanelResultToReport(Bioreference.LIS.Order lisOrder, Bioreference.LIS.Report lisReport, Bioreference.LIS.ReportAnalytePanel p, Bioreference.ResultService.DI.Interface.DIResult interfaceResult, Bioreference.ResultService.DI.Interface.ReportAnalyte discreteAnalyte, bool tabularNTEHeadingCreated, bool isReportable, string orderedTestCode, bool processIfPanelLikeTest)
        {

            logger.LogInformation($"({accessionNumber}): Adding... Panel='{p.PanelCode}' Reportable='{p.Panel.IsReportable}' - '{isReportable}'");

            Bioreference.ResultService.DI.Interface.Report bdiRpt;
            bdiRpt = GetPanelResultFromObject(lisOrder, lisReport, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, orderedTestCode, processIfPanelLikeTest);

            if (bdiRpt == null)
            {
                logger.LogInformation($"({accessionNumber}): Not Added... '{p.PanelCode}'");
                return false;
            }
            else
            {
                logger.LogInformation($"({accessionNumber}): Added");
            }

            //// If this is an IGE report then add to the bottom
            //if (bdiRpt.TestCode == settings.ResultOut_IGECode)
            //{
            //    _rptIge = bdiRpt;
            //}
            //// Don't readd if already exists
            //else if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
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
            string code = string.Empty;
            string name = string.Empty;
            if (lisPanel != null)
            {
                if (lisPanel.OrderingPanelCodes != null && lisPanel.OrderingPanelCodes.Count() > 0)
                {
                    code = lisPanel.OrderingPanelCodes[0]?.ToString() ?? string.Empty;
                    name = lisPanel.Panel.Name;
                }
                else
                {
                    code = lisPanel.PanelCode ?? string.Empty;
                }
            }


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

                    bdiRpt = new Bioreference.ResultService.DI.Interface.Report(orderedTestCode, default, obrName, accessionNumber, UtilitiesSSU.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, lisPanel.GetLatestResultDate().ToString(), lisPanel.Panel.Category, UtilitiesSSU.ConvertStatusToString(status));

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
            bdiRpt.ReportStatus = UtilitiesSSU.ConvertStatusToString(status);
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
                generateObx = false;

                foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                {

                    // ''' Dim adi As New DI.Interface.ReportAnalyte(a.Code, a.ResultValue, DateTime.Now, "", "", "", "", "", "", "", "", "", a.PerformingFacility, a.AccessioningFacility)
                    if (a.TransmitStatus == transmitStatusType.Released && a.ResultValue != "" && a.Analyte.IsReportable && a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.Discrete) // && !IsBypassNoBillCode(a.ResultValue, lisPanel.PanelCode, a.Analyte.Code, a.PerformingFacility, bdiRpt, lisReport))
                    {
                        lastDiscreteAnalyteCode = a.Analyte.Code;
                    }

                    if (((RefAnalyte)a.Analyte).AltOutboundTestCode == lisPanel.PanelCode)
                    {
                        generateObx = false;
                    }

                }

                bool flagDiscrete = false; // 'If tabular text and analyte is flagged, use this to flag parent obx
                string resultValue = "";

                var reportDate = default(DateTime);
                foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                {
                    resultValue = UtilitiesSSU.UpdateResultToFollow(a, ref reportDate);

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
                        bool sendAoeAsStType = false; //settings.SendAsSTResultType.Contains(code);

                        var anl = new Bioreference.ResultService.DI.Interface.ReportAnalyte(analyteCode, resultValue, reportDate.ToString(), default, UtilitiesSSU.ConvertResultTypeToString(a.Analyte.ResultType, a.CodeTypeId == 5, sendAoeAsStType), analyteName, a.Analyte.Units, a.GetReferenceRange().Replace('\u0096', '-'), UtilitiesSSU.ConvertStatusToString(a.ResultStatus), a.Analyte.ReferenceLabId.ToString(), default, a.FlagValue, a.PerformingFacility, a.AccessioningFacility, a.RefLabPerformingFacilityId); // a.GetFlaggedValue

                        anl.CocApproverEmpNumber = a.CocApproverEmpNbr;
                        anl.CocApproverFirstName = a.CocApproverFirstName;
                        anl.CocApproverLastName = a.CocApproverLastName;
                        anl.CocApproverMiddleName = a.CocApproverMiddleName;
                        anl.CocApprovedDate = a.CocApprovedDate;

                        // [EL] Include PanelCode for NoBill processing
                        anl.PanelCode = lisPanel.PanelCode;
                        anl.PanelSPMStatus = (int)lisPanel.SPMStatus;

                        //if (!isAllergenPanel)
                        //{
                        //    ProcessAnalyteComments(a, anl);
                        //}
                        //else
                        //{
                        //    ProcessAllergenComments(a, anl, bdiRpt);
                        //}
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

                            ////var notes = UtilitiesSSU.CreateNoteFromAnalyte(a);

                            //if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                            //{
                            //    tabText.AddRange(notes);
                            //    //foreach (NTE nte in anl.NTEs)
                            //    //    tabText.Add(settings.TabularTextNTEPrefix + nte.Text);
                            //}
                            //else if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                            //{
                            //    tabTextNoHeaders.AddRange(notes);
                            //    //foreach (NTE nte in anl.NTEs)
                            //    //    tabTextNoHeaders.Add(settings.TabularTextNTEPrefix + nte.Text);
                            //}

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
                    string[] comments = Strings.Split(UtilitiesSSU.FormatFixedWidthText(analytesAsNotes, UtilitiesSSU.NOTEFIXEDWIDTH, ';'), Environment.NewLine);
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

                            //if (Strings.InStr(cmt.Text, Environment.NewLine) > 0)
                            //{
                            //    string[] comments = Strings.Split(cmt.Text, Environment.NewLine);
                            //    //foreach (string comment in comments)
                            //    //{
                            //    //    if (settings.ResultOut_SuppressNTEs == true)
                            //    //    {
                            //    //        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId), _oSuppress);
                            //    //    }
                            //    //    else
                            //    //    {
                            //    //        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(comment), externalid: cmt.ExternalId));
                            //    //    }
                            //    //}
                            //}
                            //else if (settings.ResultOut_SuppressNTEs == true)
                            //{
                            //    lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId), _oSuppress);
                            //}
                            //else
                            //{
                            //    lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text), externalid: cmt.ExternalId));
                            //}
                        }
                    }
                }
                // *****************************************************************

            }

            logger.LogInformation($"({accessionNumber}): bdiRpt.TestCode='{bdiRpt.TestCode}'");
            logger.LogInformation($"({accessionNumber}): bdiRpt.AnalyteList.Count='{bdiRpt.AnalyteList.Count}'");

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
                logger.LogInformation($"({accessionNumber}): CreateAllergenDIReport='{analyteCode}'");
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
                logger.LogInformation($"({accessionNumber}): CreateAllergenDIReport Analytefound='{analyte.Code}'");
                allergenRpt = new Bioreference.ResultService.DI.Interface.Report(analyteCode, lisOrder.Comments, ti.Name, accessionNumber, UtilitiesSSU.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, analyte.ResultDate.ToString(), analyte.Analyte.Category, UtilitiesSSU.ConvertStatusToString(analyte.ResultStatus));
                Bioreference.ResultService.DI.Interface.ReportAnalyte anl;
                foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
                {
                    logger.LogInformation($"({accessionNumber}): CreateAllergenDIReport Code='{a.Code}'");
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
                        anl = new Bioreference.ResultService.DI.Interface.ReportAnalyte(a.Code, a.ResultValue, a.ReleaseDate.ToString(), default, UtilitiesSSU.ConvertResultTypeToString(a.Analyte.ResultType), a.Analyte.Name, a.Analyte.Units, a.GetReferenceRange(), UtilitiesSSU.ConvertStatusToString(a.ResultStatus), a.Analyte.ReferenceLabId.ToString(), default, a.FlagValue, a.PerformingFacility, a.AccessioningFacility, a.RefLabPerformingFacilityId);
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

        //private Bioreference.ResultService.DI.Interface.Report CreateIge(Bioreference.LIS.Report lisReport, Bioreference.LIS.Order lisOrder)
        //{
        //    Bioreference.ResultService.DI.Interface.Report interfaceReportIge = default;
        //    // Create IGE Serum Analyte here
        //    Bioreference.LIS.ReportAnalyte analyteIge = lisReport.FindAnalyte(settings.ResultOut_IGECode, true);
        //    if (analyteIge is not null && analyteIge.TransmitStatus != transmitStatusType.Released)
        //    {
        //        string igeCode = Conversions.ToString(settings.ResultOut_ChannelId == 0 ? analyteIge.Analyte.Code : analyteIge.OrderingAnalyteCodes[0]);
        //        string resultValue = !analyteIge.HasBeenReleased() ? "Pending" : analyteIge.ResultValue;
        //        interfaceReportIge = new Bioreference.ResultService.DI.Interface.Report(igeCode, lisOrder.Comments, analyteIge.Analyte.Name, accessionNumber, UtilitiesSSU.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, analyteIge.ResultDate.ToString(), analyteIge.Analyte.Category, UtilitiesSSU.ConvertStatusToString(analyteIge.ResultStatus));
        //        var anlIge = new Bioreference.ResultService.DI.Interface.ReportAnalyte(igeCode, resultValue, analyteIge.ReleaseDate.ToString(), default, UtilitiesSSU.ConvertResultTypeToString(analyteIge.Analyte.ResultType), analyteIge.Analyte.Name, analyteIge.Analyte.Units, analyteIge.GetReferenceRange(), UtilitiesSSU.ConvertStatusToString(analyteIge.ResultStatus), analyteIge.Analyte.ReferenceLabId.ToString(), default, analyteIge.FlagValue, analyteIge.PerformingFacility, analyteIge.AccessioningFacility, analyteIge.RefLabPerformingFacilityId); // _analyte_IGE.GetFlaggedValue)
        //        ProcessAnalyteComments(analyteIge, anlIge);
        //        interfaceReportIge.AddAnalyte(anlIge);
        //    }
        //    return interfaceReportIge;
        //}

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

    internal static class UtilitiesORU
    {
        internal const int NOTEFIXEDWIDTH = 78;

        internal static string ConvertResultTypeToString(Bioreference.Common.Lab.resultType oResultType)
        {
            return ConvertResultTypeToString(oResultType, false, false);
        }

        internal static string ConvertResultTypeToString(Bioreference.Common.Lab.resultType oResultType, bool isAoe)
        {
            return ConvertResultTypeToString(oResultType, isAoe, false);
        }

        internal static string ConvertResultTypeToString(Bioreference.Common.Lab.resultType oResultType, bool isAoe, bool sendAoeAsStType)
        {
            if (isAoe)
            {
                if (sendAoeAsStType)
                    return "ST";
                else
                    return "AE";
            }
            else if (oResultType == Bioreference.Common.Lab.resultType.DecimalFormat)
                return "NM";
            else if (oResultType == Bioreference.Common.Lab.resultType.NumericFormat)
                return "NM";
            else if (oResultType == Bioreference.Common.Lab.resultType.PickListItem)
                return "ST";
            else
                return "ST";
        }

        internal static string ConvertStatusToString(Bioreference.LIS.resultStatusType oStatus)
        {
            // If oStatus = Bioreference.LIS.resultStatusType.Pending Then
            // Return "P"
            if (oStatus == Bioreference.LIS.resultStatusType.Final)
                return "F";
            else if (oStatus == Bioreference.LIS.resultStatusType.Corrected)
                return "C";
            else
                return "P";    // 'If Pending or Preliminary set to P
        }

        internal static string PriorityToString(Bioreference.LIS.OrderPriority oPriority)
        {
            if (oPriority == Bioreference.LIS.OrderPriority.Routine)
                return "R";
            else if (oPriority == Bioreference.LIS.OrderPriority.STAT)
                return "S";
            else
                return "";
        }

        internal static string GenderToSex(Gender gender)
        {
            if (gender == Gender.Male)
                return "M";
            else if (gender == Gender.Female)
                return "F";
            else
                return "U";
        }

        internal static string FormatFixedWidthText(string text, int maxCharWidth, char breakChar = ' ')
        {
            string formattedComment = "";
            var s = new List<string>();

            foreach (string line in text.Split(Environment.NewLine))
            {
                s.Clear();
                string currentLine = line;  //.Replace("\n", "");  // Create a modifiable copy of 'line'
                if (currentLine.Length > maxCharWidth)
                {
                    int i;
                    while (currentLine.Length > maxCharWidth)
                    {
                        i = maxCharWidth - 1;
                        while (i != 0)
                        {
                            if (currentLine.Substring(i, 1) == breakChar.ToString())
                            {
                                s.Add(string.Concat(currentLine.Substring(0, i), Environment.NewLine));  // Use "\r\n" instead of Constants.vbCrLf
                                currentLine = currentLine.Substring(i + 1);  // Update 'currentLine' instead of 'line'
                                break;
                            }
                            i -= 1;
                        }
                    }
                    s.Add(string.Concat(currentLine, Environment.NewLine));
                    foreach (string c in s)
                    {
                        formattedComment = string.Concat(formattedComment, c);
                    }
                    formattedComment = string.Concat(formattedComment, Environment.NewLine);
                }
                else
                {
                    formattedComment = string.Concat(formattedComment, currentLine, Environment.NewLine);
                }
            }
            return formattedComment;
        }

        //internal static List<string> CreateNoteFromAnalyte(Bioreference.LIS.ReportAnalyte a, int pos = 1)
        //{

        //    string testDescriptionText = "";
        //    string resultText = "";
        //    string flagText = "";
        //    string referenceRangeText = "";
        //    string unitsText = "";
        //    bool addlLine = false;
        //    int startPos;

        //    var list = new List<string>();

        //    startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")));
        //    if (a.Analyte.Name.Length > startPos)
        //        testDescriptionText = a.Analyte.Name.Substring(startPos);
        //    if (testDescriptionText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")))
        //    {
        //        testDescriptionText = testDescriptionText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")));
        //        addlLine = true;
        //    }
        //    testDescriptionText = testDescriptionText.PadRight((Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionTitle")).Length, ' ');

        //    startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")));
        //    if (a.ResultValue.Length > startPos)
        //        resultText = a.ResultValue.Substring(startPos);
        //    if (resultText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")))
        //    {
        //        resultText = resultText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")));
        //        addlLine = true;
        //    }
        //    resultText = resultText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultTitle").Length, ' ');

        //    startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")));
        //    if (a.FlagValue.Length > startPos)
        //        flagText = a.FlagValue.Substring(startPos);
        //    if (flagText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")))
        //    {
        //        flagText = flagText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")));
        //        addlLine = true;
        //    }
        //    flagText = flagText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagTitle").Length, ' ');

        //    startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")));
        //    if (a.GetReferenceRange().Length > startPos)
        //        referenceRangeText = a.GetReferenceRange().Substring(startPos);
        //    if (referenceRangeText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")))
        //    {
        //        referenceRangeText = referenceRangeText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")));
        //        addlLine = true;
        //    }
        //    referenceRangeText = referenceRangeText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeTitle").Length, ' ');

        //    startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")));
        //    if (a.Analyte.Units.Length > startPos)
        //        unitsText = a.Analyte.Units.Substring(startPos);
        //    if (unitsText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")))
        //    {
        //        unitsText = unitsText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")));
        //        addlLine = true;
        //    }

        //    unitsText = unitsText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsTitle").Length, ' ');

        //    list.Add(string.Concat(testDescriptionText, resultText, flagText, referenceRangeText, unitsText));

        //    //if (addlLine)
        //    //{
        //    //    list.AddRange(CreateNoteFromAnalyte(a, pos + 1));
        //    //}

        //    return list;

        //}

        internal static string UpdateResultToFollow(Bioreference.LIS.ReportAnalyte analyte, [Optional] ref DateTime reportDate)
        {
            string resultValue;

            reportDate = analyte.ReleaseDate; // ITBTC-81: Sending current release date, if already has been released
            if (!analyte.HasBeenReleased())
            {
                if ((analyte.ToFollowSent == false && analyte.Analyte.IsReportable) & analyte.Analyte.IsRequired)
                {
                    if (analyte.CodeTypeId == 5) // AOE
                    {
                        resultValue = analyte.ResultValue;
                    }
                    else
                    {
                        resultValue = "TO FOLLOW";
                        reportDate = DateTime.Now;
                    } // ITBTC-81: sending current date to each TO FOLLOW on the order message.
                }
                else
                {
                    resultValue = analyte.ResultValue;
                }     // resultValue = "Pending"
            }
            else
            {
                resultValue = analyte.ResultValue;
            }
            return resultValue;
        }

        public static string ArchiveStatus(string msg)
        {

            var conn = new SqlConnection(LIS.Configuration.AppSettings.GetString("ConnectionStrings:Bioreference.Iguana.HL7.B2.ConnString_ArchiveStatus"));
            SqlCommand cmd = null;
            string timestampId = "";

            try
            {
                conn.Open();
                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "StatusToVertex_Save";
                cmd.Parameters.Add("@Message", SqlDbType.Text).Value = msg;
                var dt = cmd.ExecuteReader();
                dt.Read();
                timestampId = Conversions.ToString(dt["TimeStampID"]);
            }
            catch (Exception ex)
            {
                timestampId = "";
                throw ex;
            }
            finally
            {
                if (!(conn == null))
                {
                    conn.Close();
                    conn.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
            return timestampId;
            // Dim ds As DataSet = _channelLogDB.ExecuteSP("StatusToVertex_Save", New String() {"@Message"}, New Object() {msg})
            // Return ds.Tables(0).Rows(0)("TimeStampID")
        }
    }
}
