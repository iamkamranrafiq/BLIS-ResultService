using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.ReportOutNonCum;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using HL7Parser.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;


namespace Bioreference.ResultService.Application.Processor
{
    public class ReportOutNonCumProcessor : IReportOutNonCumProcessor
    {
        private ILogger<ReportOutNonCumProcessor> logger;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReportOutNonCum appSettings;
        private readonly IPayloadSenderApiClient payloadSenderApiClient;
        private ReportOutNonCumSettings Settings = null;

        // private processOddEvenType m_processIdType = Settings.ProcessOddEven;
        private int m_totalChannelCount;
        private int m_currentChannelIndex;
        private int m_allergenReportType;

        // private Reports.processDataType m_processDataType = Settings.ProcessDataType;
        private DI.Interface.ExcludedStrings m_oSuppress = null;
        private static bool m_sendOrderedCode;
        private List<string> m_testsNoRefRangeByPass = null;
        private bool m_canceled = false; // If this is set to true, we still need to process the Report, but not send a message to Reporting
        private bool m_hasAllergen = false;
        private static DateTime _settingsFetch;
        private string accessionNumber = string.Empty;
        private DI.Interface.Report m_rptIGE = null;

        public ReportOutNonCumProcessor(ILogger<ReportOutNonCumProcessor> logger, IPayloadSenderApiClient payloadSenderApiClient, ISettingService settingsProvider, IOptions<AppSettingsReportOutNonCum> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
            this.payloadSenderApiClient = payloadSenderApiClient;
        }

        public async Task InitializeAsync()
        {
            string connection = settingsProvider.GetConnectionString("Bioreference.LIS");
            Settings = await settingsProvider.FetchSetting<ReportOutNonCumSettings>(
                connection, "ResultOutEngine", "");

            m_totalChannelCount = Settings.TotalChannelCount;
            m_currentChannelIndex = Settings.CurrentChannelIndex;
            m_allergenReportType = appSettings.AllergenReportType;
            m_sendOrderedCode = Settings.ResultOut_SendOrderedCode;
            m_testsNoRefRangeByPass = Settings.ResultsOut_TestsNoRefBypass;
          
        }
        public async Task OnReportOutNonCumMessage(ReportOutNonCum reportOutNonCum)
        {      
            await InitializeAsync();
            bool isReportable = false;
            int reportId = 0;         

            try
            {
                ActivityHelper.SetAccessionLogKey(reportOutNonCum.AccessionNbr);
                logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    reportOutNonCum.AccessionNbr,
                    DateTime.UtcNow
                );

                logger.LogInformation($"({reportOutNonCum.AccessionNbr}): Received ReportOut Message");               
                    m_hasAllergen = false;
                    accessionNumber = reportOutNonCum.AccessionNbr;
                    isReportable = reportOutNonCum.IsReportable;
                    reportId = reportOutNonCum.ReportId;
                if (Settings.ResultOut_SuppressNTEs)
                {
                    m_oSuppress = new Bioreference.ResultService.DI.Interface.ExcludedStrings();
                    string success = m_oSuppress.CreateList("SuppressNtes");
                    if (success != "SUCCESS")
                        logger.LogError($"({reportOutNonCum.AccessionNbr}): Could not create SuppressedNtes list='{success}'");
                }
                logger.LogDebug($"Processing accession {accessionNumber} isReportable: {isReportable} ReportId: {reportId}");

                    var blReport = Bioreference.LIS.Report.Fetch(reportId);

                    if (blReport != null && blReport.ID > 0)
                    {
                        try
                        {
                            string message = this.GenerateMessage(blReport, isReportable, false);

                            logger.LogDebug($"The Generated Message: {message}");

                            if (m_canceled || string.IsNullOrEmpty(message))
                            {
                                logger.LogWarning("Outbound results consists of just cancelled tests. No Non-Cum message is sent to Reporting.");
                            }

                            if (!await payloadSenderApiClient.SendPayloadAsync(message, "REPORTING"))
                                throw new Exception("Error sending message to Iguana");

                        logger.LogInformation($"({reportOutNonCum.AccessionNbr}): Message sent to Iguana");
                    }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"({reportOutNonCum.AccessionNbr}): OnReportOutMessage(Inner)='{ex.Message}'");
                        }
                        logger.LogInformation($"({reportOutNonCum.AccessionNbr}): Processed ReportOut Message");
                }                

                logger.LogDebug("Done processing.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"({reportOutNonCum.AccessionNbr}): OnReportOutMessage(Outer)='{ex.Message}'");
                throw;
            }
        }
        private string GenerateMessage(Bioreference.LIS.Report blReport, bool isReportable, bool processAllergen)
        {
            // ONLY for specified accessions (CanSendAccessionNotes).
            // If does not have any tests to release, DO NOT generate message -
            // this will only generate specimen/accession notes and status (due to add-on).
            if (this.HasValidPrefixForAccessionStatus(blReport.AccessionNbr) || blReport.HasReleased())
            {
                // NA 11/14/13
                // Dim rslt As Bioreference.DI.Interface.Result = GetResultFromObject(blReport, isReportable)
                DI.Interface.DIResult rslt =
                    GetResultFromObject(blReport, isReportable, processAllergen);

                if (rslt != null)
                {
                   return CreateMessage(rslt, isReportable, blReport);
                }
            }
            logger.LogInformation($"({accessionNumber}): GenerateMessage: No message generated.");

            return string.Empty;
        }
        private string CreateMessage(DIResult result, bool isReportable, LIS.Report blReport)
        {
            string hl7Message = string.Empty;
            var oMessage = new ORUMessage(); 
            oMessage.PatientInfoComments = new List<PatientComment>();
            oMessage.LabReport = new List<LabReport>();
            string sLastName = string.Empty;
            string sFirstName = string.Empty;
            string sSex = string.Empty;
            string sOrderdate = string.Empty;
            string sCollectDate = string.Empty;
            string sDocName = string.Empty;
            string sTestCode = string.Empty;
            int PID_Counter = string.IsNullOrEmpty(Settings.ResultOut_FetchCount.ToString()) ? 1 : Settings.ResultOut_FetchCount - (Settings.ResultOut_FetchCount - 1);

            if (PID_Counter >= Settings.ResultOut_FetchCount)
            {
                PID_Counter = 1;
            }
            try
            {
                // MSH
                logger.LogInformation($"({accessionNumber}): MSH");
                oMessage.MSH = new MSH()
                {
                    SendingApplication = Settings.MSH_SendingApp,
                    SendingFacilityCode = Settings.MSH_SendingFacilityCode,
                    ReceivngApplication = Settings.MSH_ReceivingApp,
                    ReceivingFacility = Settings.MSH_ReceivingFacility,

                    MessageTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    MessageEvent = Settings.MSH_MessageEvent,
                    MessageID = DateTime.Now.ToString("yyyyMMddHHmmssfff"),

                    ProcessingId = Settings.MSH_ProcessingId,
                    VersionId = Settings.MSH_VersionId,
                    AcceptAckType = Settings.MSH_AcceptAckType,

                    MessageType = isReportable ? Settings.MSH_MessageType : "STA"
                   

                };

                // PID
                logger.LogInformation($"({accessionNumber}): PID");
                oMessage.PatientInfo = new PatientInfo()
                {
                    Set_ID = PID_Counter.ToString(),
                    ClientMRN = result.rPatient.Id,
                    PatientIdentifierList = result.rPatient.IdentifierList,
                    LastName = result.rPatient.LastName,
                    FirstName = result.rPatient.FirstName,
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

                // 'FOR UPDATE
                // ''ORC notes
                // 'This goes here for now since reporting ignores ORC recs
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

                // PV1
                //oMessage.PatientVisit().AddRow();
                logger.LogInformation($"({accessionNumber}): PV1");
                oMessage.PatientVisit = new PatientVisit()
                {
                    Set_ID = "1",
                    PatientClass = result.rPatientVisit.PatientClass,
                    PatientLocation = result.rPatientVisit.PatientLocation
                };

                // 'FOR UPDATE
                // 'ORC
                // If rResult.OrderStatus <> "" Then
                logger.LogInformation($"({accessionNumber}): ORC");
               
                oMessage.Order = new Bioreference.ResultService.DI.Interface.Order()
                {                    
                    AccessionNumber = result.rPatient.IdentifierList,                   
                    OrderStatus = result.OrderStatus,                    
                    OrderDate = result.ServiceDate.DateTimeStamp(),                   
                    ConfidentialityCode = blReport.IsCOC ? "R" : string.Empty
                  
                };
                // End If

                // 'For those accessions, set ORC-25 to COMM and attach notes.
                if (CanSendAccessionNotes(result.rPatient.IdentifierList))
                {                    
                    oMessage.Order.HasComments = "COMM";
                    oMessage.OrderComment = new List<Bioreference.ResultService.DI.Interface.OrderComment>();
                    for (int i = 0, loopTo1 = result.OrderComments.Count - 1; i <= loopTo1; i++)
                    {
                        oMessage.OrderComment.Add(new Bioreference.ResultService.DI.Interface.OrderComment() { NoteText = result.OrderComments[i].TrimEnd() });
                        logger.LogInformation($"({accessionNumber}): OrderComments='{result.OrderComments[i]}");
                    }
                }

                // OBR
                logger.LogInformation($"({accessionNumber}): OBR");
                
                if (result.rReports.Count > 0)
                {
                    logger.LogDebug($"({accessionNumber}): Processing OBR Count='{result.rReports.Count}'");
                    var containsTestCode = default(bool);
                    int obrCount = 0;
                    foreach (DI.Interface.Report rpt in result.rReports)
                    { 
                        var obr = new OBR();                       
                        obr.Set_ID = (obrCount + 1).ToString();                     
                        obr.AccessionNum = rpt.AccessionNum;
                        sTestCode = rpt.TestCode;
                        obr.TestCode = rpt.TestCode;
                        obr.TestDescription = rpt.TestDescription;
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
                        sDocName = rpt.PhysLastName;                        
                        obr.ProviderLastName = rpt.PhysLastName;                      
                        obr.ProviderFirstName = rpt.PhysFirstName;                       
                        obr.CallBackPhone = rpt.CallBackPhone;

                        // mzia (only for production)
                        // 'Do not supress Lenetix testcodes
                        bool isLenetix = false;
                        if (!(Settings.Lenetix_TestCodes == null))
                        {
                            if (Settings.Lenetix_TestCodes.Contains(rpt.TestCode))
                            {
                                isLenetix = true;
                            }
                        }
                        if (!(Settings.TestCodes == null))
                        {
                            if (Settings.TestCodes.Contains(sTestCode))
                            {
                                containsTestCode = true;
                            }
                        }

                        string panelLikeTest = GetPanelLikeTest(rpt.TestDescription);

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
                            bool forceNoSuppress = false;
                            
                            Bioreference.ResultService.DI.Interface.ReportAnalyte lastAnalyte = default;
                            LIS.ReportAnalyte lastAnalyteDetails = default;

                            foreach (Bioreference.ResultService.DI.Interface.ReportAnalyte a in rpt.AnalyteList)
                            {                               

                                // NA 09/04/2014
                                // Add Code here to bypass certain TestCodes.  Do not send to reporting
                                if (Settings.ResultOut_TestCodesToNotReport.Contains(a.Code))
                                {
                                    continue;
                                }
                              

                                logger.LogInformation($"({accessionNumber}): Add code to message");                             
                        

                                count += 1;                              

                                var obx = new OBX();
                                LIS.ReportAnalyte analyteInstrument = blReport.FindAnalyte(a.Code, true);

                                obx.Set_ID = count.ToString(); //analyteSetId.ToString();                               
                                obx.ValueType = a.ValueType;
                                
                                if (analyteInstrument != null)
                                {
                                    obx.InstrumentId = analyteInstrument.InstrumentId;
                                }
                                obx.Code = a.Code;
                           
                                // AG - if the FIRST component code(analyte) is not the same as the obr code, then we suppress.
                                // mzia: do not suppress if its an allergen obr
                                string panelLikeTest1 = GetPanelLikeTest(rpt.TestDescription);

                                // '******************************************************************
                                // 'Do not supress Lenetix testcodes
                                if (!Settings.Lenetix_TestCodes.Contains(rpt.TestCode) && !string.IsNullOrEmpty(panelLikeTest1))
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

                                // changed only for NON Cum channel.
                                // 2/14/2014
                                // if (this.ReferenceLabId != "0" && a.Code == rpt.TestCode)
                                if (a.Code == rpt.TestCode)
                                {
                                    forceNoSuppress = true;
                                }

                                // '******************************************************************
                                obx.Description = a.Description;
                                

                                // 'Temporary Rounding for some calculations
                                // 'REMOVED - these should be set in TESTMASTER
                                // .SetValue(MyBase.TemporaryRoundingFunction(a.Code, a.Value))
                                obx.Value = a.Value;
                                

                                // If a.Value = "TNP" Then oRptStat.TNP = True
                                obx.Units = a.Units;
                                
                                string sRefRange = a.ReferenceRange.Replace(Convert.ToChar(150), '-');
                                obx.ReferenceRange = sRefRange;
                                obx.Flag = a.Flag;                                
                                obx.Status = a.Status;
                              

                                // '***********************************************

                                // N.A. : 10/1/16
                                // commented out due to production problem

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
                                // '***********************************************

                                obx.ReportDate = a.ReportDate.DateTimeStamp();
                                

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
                                }

                                bool isAnalyteTNP = SharedFunctions.IsTNP(a.Value);
                                bool isAnalyteATP = SharedFunctions.IsATP(a.Value);
                                // OBX NTEs
                                List<LabComment> obxComments = null;
                                if (a.NTEs.Count > 0)
                                {
                                    obxComments = new List<LabComment>();
                                    logger.LogInformation($"({accessionNumber}): Processing OBX NTEs Count='{a.NTEs.Count}'");
                                    int lrcCount = 1;
                                    foreach (NTE n in a.NTEs)
                                    {
                                        bool suppressTNP = isAnalyteTNP && !CheckTNPComment(n.Text);
                                        bool suppressATP = isAnalyteATP && !CheckATPComment(n.Text);
                                        bool suppress = suppressTNP || suppressATP;
                                        logger.LogInformation($"({accessionNumber}): OBX NoteText='{n.Text}'");
                                        if (!suppress)
                                        {
                                            var resultComment = new LabComment()
                                            {
                                                NoteId = lrcCount.ToString(),
                                                NoteText = n.Text.TrimEnd()
                                            };
                                            obxComments.Add(resultComment);
                                            lrcCount++;
                                        }
                                    }
                                }

                         

                                var obxs = new List<OBX>() { obx };                               
                                var oResult = new DI.Interface.Result()
                                {
                                    Obx = obxs,
                                    ResultComment = obxComments != null ? obxComments : new()
                                };
                                results.Add(oResult);
                            }
                            logger.LogInformation($"({accessionNumber}): AddSuppress='{addSuppress}'");
                            if (forceNoSuppress)
                            {
                                obr.IsPanel = string.Empty;
                            }
                            else if (addSuppress)
                            {
                                obr.IsPanel = "SUPPRESS";
                            }
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
                hl7Message = ToHL7(oMessage, "ORU_Mapping_Outbound.json");
                logger.LogInformation($"({accessionNumber}): HL7='{hl7Message}'");
            }            
            catch (Exception ex)
            {
                logger.LogError($"({accessionNumber}): CreateMessage() failed='{ex.Message}'");
            }

            // mzia 3/2/2010: Incremental PIDs
            PID_Counter = PID_Counter + 1;
            logger.LogInformation($"({accessionNumber}): GenerateMessage hl7='{hl7Message}'");
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

        public bool HasValidPrefixForAccessionStatus(object accessionNbr)
        {
            if (Settings.ResultOut_AccStatus_ValidPrefixes.Count == 0)
                return true;

            return (accessionNbr.ToString().Length == 9 && Settings.ResultOut_AccStatus_ValidPrefixes.Contains(accessionNbr.ToString().Substring(0, 2)));
        }
        private DIResult GetResultFromObject(LIS.Report lisReport, bool isReportable, bool processAllergen)
        {
            DIResult interfaceResult;
            DI.Interface.Report bdiRpt;
            // Dim oSuppress As Bioreference.DI.Interface.ExcludedStrings = Nothing

            DI.Interface.ReportAnalyte discreteAnalyte = null;
            bool tabularNTEHeadingCreated = false;

            // Dim orderableTests As List(Of String) = New List(Of String)()

            // mzia: A released IGE Serum should be added automatically if there are Allergens in report
            m_rptIGE = null;        

                long lisOrderId = lisReport.OrderId;
                Bioreference.LIS.Order lisOrder = OrderManager.FetchOrder(lisOrderId);
            interfaceResult = new DIResult();
            interfaceResult.ServiceDate = lisOrder.DateOfService.ToString();
            interfaceResult.rPatient = new Patient(
                    lisOrder.Patient.PatientId,
                    lisReport.AccessionNbr,
                    lisOrder.Patient.LastName,
                    lisOrder.Patient.FirstName,
                    lisOrder.Patient.MiddleName,
                    lisOrder.Patient.DateOfBirth,
                    ResultService.Common.Utilities.GenderToSex(lisOrder.Patient.Gender),
                    lisOrder.Patient.PrimaryAddress.StreetLine1,
                    lisOrder.Patient.PrimaryAddress.StreetLine2,
                    lisOrder.Patient.PrimaryAddress.City,
                    lisOrder.Patient.PrimaryAddress.State,
                    lisOrder.Patient.PrimaryAddress.ZipCode,
                    lisOrder.Patient.HomePhoneNumber,
                    lisOrder.Patient.WorkPhoneNumber,
                    lisOrder.Patient.SocialSecurityNum,
                    null,
                    lisOrder.AccountNumber
                );

                interfaceResult.rPatient.UpdateTrackingId = lisOrder.Patient.UpdateTrackingId;

                interfaceResult.rPatientVisit = new PatientVisit(null, lisOrder.AccountNumber);

                // FOR UPDATE
                // ****************************************************
                // Set Order Status
                if (Settings.ResultOut_AccStatus_Enable)
                {
                    if (this.HasValidPrefixForAccessionStatus(lisReport.AccessionNbr))
                    {
                        interfaceResult.OrderStatus =
                            lisReport.IsFinal() ? "CM" : "A";
                    }
                }

                // AccessionComments
                if (this.CanSendAccessionNotes(lisOrder.AccessionNbr))
                {
                    string sComments;

                    foreach (LIS.OrderComment cmt in lisOrder.OrderComments.List)
                    {
                        sComments = ResultService.Common.Utilities.FormatFixedWidthText(cmt.Text, ResultService.Common.Utilities.NOTEFIXEDWIDTH);

                        foreach (string s in sComments.Split(new[] { "\r\n" }, StringSplitOptions.None))
                        {
                            interfaceResult.OrderComments.Add(s.Replace(((char)10).ToString(), ""));
                        }
                    }
                }

                // Order/Specimen comments
                if (lisReport.SpecimenComment != "")
                {
                    interfaceResult.Comments = lisReport.SpecimenComment;
                }
                // ****************************************************

                bool hasAllergen = false;

                // Loop through analytes
                foreach (LIS.ReportAnalyte a in lisReport.Analytes.List)
                {
                    if ((a.TransmitStatus == transmitStatusType.StatusSentToVertex ||
                         a.TransmitStatus == transmitStatusType.SentToReporting)
                        && a.Analyte.IsReportable == isReportable)
                    {
                        bool isAdded;

                        if (!m_sendOrderedCode)
                        {
                            isAdded = AddAnalyteResultToReport(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, a.Code, true, processAllergen);
                        }
                        else
                        {
                            isAdded = false;

                            foreach (string s in a.OrderingAnalyteCodes)
                            {
                                if (AddAnalyteResultToReport(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, s, true, processAllergen))
                                {
                                    isAdded = true;
                                }
                            }
                        }

                        if (isAdded && a.Analyte.Category.ToLower().Equals("allergen"))
                        {
                            hasAllergen = true;
                            m_hasAllergen = true;
                        }
                    }
                }

                // Loop through panels
                foreach (ReportAnalytePanel p in lisReport.AnalytePanels.List)
                {
                    if ((p.TransmitStatus == transmitStatusType.StatusSentToVertex ||
                         p.TransmitStatus == transmitStatusType.SentToReporting)
                        && p.Panel.IsReportable == isReportable)
                    {
                        bool isAdded;

                        if (!m_sendOrderedCode)
                        {
                            isAdded = this.AddPanelResultToReport(lisOrder, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, p.PanelCode, true, processAllergen);
                        }
                        else
                        {
                            isAdded = false;

                            foreach (string s in p.OrderingPanelCodes)
                            {
                                if (this.AddPanelResultToReport(lisOrder, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, isReportable, s, true, processAllergen))
                                {
                                    isAdded = true;
                                }
                            }
                        }

                        if (isAdded && p.Panel.Category.ToLower().Equals("allergen"))
                        {
                            hasAllergen = true;
                            m_hasAllergen = true;
                        }
                    }
                }

                // Re-send allergens if needed
                if (hasAllergen && processAllergen)
                {
                    if (m_rptIGE == null)
                    {
                        m_rptIGE = this.CreateIGE(lisReport, lisOrder);
                    }

                    foreach (Bioreference.LIS.ReportAnalyte a in lisReport.Analytes.List)
                    {
                        if (a.Analyte.IsReportable == isReportable &&  a.Analyte.Category.ToLower().Equals("allergen"))
                        {
                            bdiRpt = GetAnalyteResultFromObject(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, "", true, processAllergen);

                            if (bdiRpt != null)
                            {
                                if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                                {
                                    interfaceResult.rReports.Add(bdiRpt);
                                }
                            }
                        }
                    }

                    foreach (Bioreference.LIS.ReportAnalytePanel p in lisReport.AnalytePanels.List)
                    {
                        if (p.Panel.IsReportable == isReportable &&
                            p.Panel.Category.ToLower().Equals("allergen"))
                        {
                            bdiRpt = GetPanelResultFromObject(lisOrder, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, "", true, processAllergen);

                            if (bdiRpt != null)
                            {
                                if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                                {
                                    interfaceResult.rReports.Add(bdiRpt);
                                }
                            }
                        }
                    }
                }

                // mzia: Add IGE Serum to report
                if (m_rptIGE != null && processAllergen)
                {
                    interfaceResult.rReports.Add(m_rptIGE);
                }
            

            // If interfaceResult.rReports.Count > 0 Then Return interfaceResult Else Return Nothing
            return interfaceResult;
        }
        public bool HasCanceledAllergen(ReportAnalytePanel panel)
        {
            foreach (Bioreference.LIS.ReportAnalyte a in panel.Analytes.List)
            {
                if (IsCanceledAllergen(a))
                    return true;
            }

            return false;
        }

        private DI.Interface.Report GetPanelResultFromObject(LIS.Order lisOrder, LIS.ReportAnalytePanel lisPanel, DIResult bdiRslt, DI.Interface.ReportAnalyte interfaceDiscreteAnalyte, bool tabularNTEHeadingCreated, string orderedTestCode, bool processIfPanelLikeTest, bool processAllergen)
        {
            // If a canceled allergen, do not send to reporting.
            if (this.HasCanceledAllergen(lisPanel))
            {
                m_canceled = true;
                return null;
            }

            // For Tabular Text
            List<string> tabText = new List<string>();
            List<string> tabTextNoHeaders = new List<string>();
            tabularNTEHeadingCreated = false;
            interfaceDiscreteAnalyte = null;

            // If OutboundChannelId is set, then we pass back the ordering code for the panel.
            string code = (Settings.ResultOut_ChannelId == 0 ? lisPanel.PanelCode : lisPanel.OrderingPanelCodes[0]);
            string name = lisPanel.Panel.Name;

            // Alternate Outbound
            if (lisPanel.Panel.GetType() == typeof(RefAnalytePanel))
            {
                RefAnalytePanel refa = (RefAnalytePanel)lisPanel.Panel;
                if (!refa.AltOutboundTestCode.Equals("")) code = refa.AltOutboundTestCode;
                if (!refa.AltOutboundDescription.Equals("")) name = refa.AltOutboundDescription;
            }

            string[] testInfo;

            // mzia: look for panelLike tests e.g. Allergens
            string panelLikeTest = GetPanelLikeTest(lisPanel.Panel.Category);
            DI.Interface.Report bdiRpt = null;
            bool isPanelLikeTest = false;

            // NA 11/14/13
            if (panelLikeTest.Contains("^") && processIfPanelLikeTest && processAllergen)
            {
                testInfo = panelLikeTest.Split('^');
                bdiRpt = bdiRslt.FindReport(testInfo[0]);

                name = testInfo[1];
                orderedTestCode = testInfo[0];
                isPanelLikeTest = true;
            }

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
                if (!string.IsNullOrEmpty(orderedTestCode)) bdiRpt = bdiRslt.FindReport(orderedTestCode);
                if (bdiRpt == null)
                {
                    Bioreference.LIS.OrderTest ot = lisOrder.Tests.Find(code, orderedTestCode);
                    string obrName = "";
                    if (ot != null)
                    {
                        obrName = ot.OrderedTestName;
                    }
                    else
                    {
                        // If created from a reflex will not have an OrderTest - need to get test name from another.
                        obrName = lisOrder.Tests.FindOrderedTestName(orderedTestCode);
                    }
                    if (obrName.Equals("")) obrName = name;

                    bdiRpt = new DI.Interface.Report(
                        orderedTestCode,
                        null,
                        obrName,
                        accessionNumber,
                        ResultService.Common.Utilities.PriorityToString(lisOrder.Priority),
                        lisOrder.DateOfService.ToString(),
                        lisOrder.DateOfCollection.ToString(),
                        null,
                        lisOrder.DateOfService.ToString(),
                        lisOrder.AccountNumber,
                        lisOrder.PrimaryPhysician.LastName,
                        null,
                        null,
                        lisPanel.GetLatestResultDate().ToString(),
                        lisPanel.Panel.Category,
                        ResultService.Common.Utilities.ConvertStatusToString(status)
                    );

                    // If the ordered code is not an actual analyte, then add suppress
                    if (lisPanel.Parent.FindAnalyte(orderedTestCode, false) == null)
                    {
                        bdiRpt.AddSuppress = true;
                    }
                }
            }

            if (this.FindAnalyte(bdiRpt, code) != null) return null; // Already exists in OBR, do not readd.

            // If status is blank or current status is pending, then set.
            bdiRpt.ReportStatus = ResultService.Common.Utilities.ConvertStatusToString(status);

            // For Terse analytes - we add them as a comment on the last Discrete Analyte.
            string analytesAsNotes = "";
            string lastDiscreteAnalyteCode = "";
            DI.Interface.ReportAnalyte lastDiscreteAnalyte = null;

            if (!lisPanel.Panel.IsReportable)
            {
                // bdiRslt.rReports.Add(bdiRpt)  ''OBR with no OBXs
                return bdiRpt;
            }
            else
            {
                bool generateObx = true; // If Lenetix = true and all Analyte outbound test code <> panelcode, then keep true.
                bool isAllergenPanel = lisPanel.Panel.Category.ToLower().Equals("allergen");

                if (!Settings.Lenetix_TestCodes.Contains(lisPanel.PanelCode))
                {
                    generateObx = false;
                }

                // We first need to loop through all analytes and find the last Discrete analyte to add Terse comments to
                // We also use it to add panel comments to. Also, use altoutboundCode to determine if we need to generate a dummy obx record.
                // 3/27/2013 - see if all results are the same - if so, send out one OBX
                // 4/3/2013 - REMOVED ResultsSame functionality
                bool areResultsSame = false;
                string resultSameValue = "";

                foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                {
                    if (a.HasBeenReleased() &&
                        a.ResultValue != "" &&
                        a.Analyte.IsReportable &&
                        a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.Discrete)
                    {
                        lastDiscreteAnalyteCode = a.Analyte.Code;
                    }

                    if (((RefAnalyte)a.Analyte).AltOutboundTestCode == lisPanel.PanelCode)
                    {
                        generateObx = false;
                    }
                }

                bool flagDiscrete = false; // If tabular text and analyte is flagged, use this to flag parent obx
                string resultValue = "";

                if (areResultsSame)
                {
                    // Set it to the last discrete analyte so that comments, etc. can be added to it
                    lastDiscreteAnalyte = new DI.Interface.ReportAnalyte(
                        lisPanel.PanelCode,
                        resultSameValue,
                        lisPanel.ReleaseDate.ToString(),
                        "",
                        "ST",
                        lisPanel.PanelName,
                        "",
                        "",
                        "F",
                        lisPanel.Panel.ReferenceLabId.ToString(),
                        "",
                        ""
                    );

                    foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                    {
                        if (a.HasBeenReleased() && a.Comments.List.Count > 0)
                        {
                            // Dont handle Non Cum comments like regular Allergen Comments.
                            ProcessAnalyteComments(a, lastDiscreteAnalyte);
                        }
                    }

                    bdiRpt.AddAnalyte(lastDiscreteAnalyte);
                }
                else
                {
                    foreach (Bioreference.LIS.ReportAnalyte a in lisPanel.Analytes.List)
                    {
                        resultValue = (a.HasBeenReleased() ? a.ResultValue : "Pending");

                        // AG 9/17/2008 - Added this since Analytes can now be optional (DisplayByDefault = false)
                        // We don't want to sent the analyte if it was not assigned a value thus not released.
                        // Also add ResultValue <> "" condition, should never send blank results.
                        // AG 10/20/2008 Also must be Reportable
                        if ((a.HasBeenReleased() || a.Analyte.Category.ToLower().Equals("allergen")) &&
                            resultValue != "" &&
                            (a.Analyte.IsReportable ||
                             (!processIfPanelLikeTest && a.Analyte.Category.ToLower().Equals("allergen"))))
                        {
                            // If type is Terse then we add it to our string of Terse analytes to then add as a comment.
                            if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.Terse)
                            {
                                analytesAsNotes += string.Concat(a.Analyte.Name, " ", resultValue, "; ");
                                continue;
                            }

                            // This is to replace an analyte code in a one component panel with the panelcode, if setting permits.
                            string analyteCode = "";
                            if (a.Analyte.ReferenceLabId != 0 && lisPanel.Analytes.List.Count == 1)
                            {
                                Bioreference.Common.Lab.ReferenceLab refLab = Bioreference.Common.Lab.ReferenceLab.Fetch(a.Analyte.ReferenceLabId);
                                if (refLab != null)
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

                            string analyteName = a.Analyte.Name;

                            // Alternate Outbound
                            if (a.Analyte.GetType() == typeof(RefAnalyte))
                            {
                                RefAnalyte refa = (RefAnalyte)a.Analyte;
                                if (!refa.AltOutboundTestCode.Equals("")) analyteCode = refa.AltOutboundTestCode;
                                if (!refa.AltOutboundDescription.Equals("")) analyteName = refa.AltOutboundDescription;
                            }

                            DI.Interface.ReportAnalyte anl = new DI.Interface.ReportAnalyte(
                                analyteCode,
                                resultValue,
                                a.ReleaseDate.ToString(),
                                null,
                                ResultService.Common.Utilities.ConvertResultTypeToString(a.Analyte.ResultType),
                                analyteName,
                                a.Analyte.Units,
                                a.GetReferenceRange().Replace(((char)150).ToString(), "-"),
                                ResultService.Common.Utilities.ConvertStatusToString(a.ResultStatus),
                                a.Analyte.ReferenceLabId.ToString(),
                                null,
                                a.FlagValue,
                                a.PerformingFacility,
                                a.AccessioningFacility
                            );

                            // don't handle non cum comments as allergen comments.
                            ProcessAnalyteComments(a, anl);

                            if (a.Code == lastDiscreteAnalyteCode)
                            {
                                lastDiscreteAnalyte = anl;
                            }

                            // if this analyte's reporting type is TabularText
                            if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText ||
                                a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                            {
                                if ((generateObx || lastDiscreteAnalyteCode == ""))
                                {
                                    interfaceDiscreteAnalyte = new DI.Interface.ReportAnalyte(
                                        code,
                                        "SEE BELOW",
                                        a.ReleaseDate.ToString(),
                                        "",
                                        "ST",
                                        name,
                                        "",
                                        "",
                                        "F",
                                        anl.ReferenceLabNumber,
                                        "",
                                        ""
                                    );
                                }
                                if (!tabularNTEHeadingCreated && a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                                {
                                    tabText.Add(string.Concat(
                                        Settings.TestDescriptionTitle,
                                        Settings.ResultTitle,
                                        Settings.FlagTitle,
                                        Settings.ReferenceRangeTitle,
                                        Settings.UnitsTitle
                                    ));
                                    tabularNTEHeadingCreated = true;
                                }

                                List<string> notes = ResultService.Common.Utilities.CreateNoteFromAnalyte(a);

                                if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                                {
                                    tabText.AddRange(notes);
                                    foreach (NTE nte in anl.NTEs)
                                    {
                                        tabText.Add(Settings.TabularTextNTEPrefix + nte.Text);
                                    }
                                }
                                else if (a.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                                {
                                    tabTextNoHeaders.AddRange(notes);
                                    foreach (NTE nte in anl.NTEs)
                                    {
                                        tabTextNoHeaders.Add(Settings.TabularTextNTEPrefix + nte.Text);
                                    }
                                }

                                if (a.FlagValue != "") flagDiscrete = true;
                            }
                            else
                            {
                                bdiRpt.AddAnalyte(anl);
                            }
                        }
                    }
                }

                if (interfaceDiscreteAnalyte != null)
                {
                    if (flagDiscrete) interfaceDiscreteAnalyte.AddFlag("*");
                    bdiRpt.AddAnalyte(interfaceDiscreteAnalyte);
                    lastDiscreteAnalyte = interfaceDiscreteAnalyte;
                }
                if (lastDiscreteAnalyte != null)
                {
                    if (tabTextNoHeaders.Count > 0)
                    {
                        foreach (string s in tabTextNoHeaders)
                        {
                            lastDiscreteAnalyte.AddNTE(new NTE(s));
                        }
                    }
                }

                if (tabText.Count > 0)
                {
                    foreach (string s in tabText)
                    {
                        lastDiscreteAnalyte.AddNTE(new NTE(s));
                    }
                }

                // Once we finish looping through all the analytes, we attach all Terse analytes to the
                // last analyte in the panel.
                if (lastDiscreteAnalyte != null && analytesAsNotes.Length > 2)
                {
                    analytesAsNotes = analytesAsNotes.Substring(0, analytesAsNotes.Length - 2); // Remove last " ;"
                    string[] comments = ResultService.Common.Utilities.FormatFixedWidthText(analytesAsNotes, 78, ';').Split(new[] { "\r\n" }, StringSplitOptions.None);
                    foreach (string s in comments)
                    {
                        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(s.Trim(' '))));
                    }
                }

                bool IsTNPAutoComment = false;
                bool IsATPAutoComment = false;

                // We add the panel comments to the last obx record for reporting.
                if (lastDiscreteAnalyte != null)
                {
                    foreach (ReportComment cmt in lisPanel.Comments.List)
                    {
                        IsTNPAutoComment = CheckTNPComment(cmt.Text);
                        IsATPAutoComment = CheckATPComment(cmt.Text);
                        if (SharedFunctions.IsTNP(lastDiscreteAnalyte.Value) && cmt.IsAutoAdded && !IsTNPAutoComment)
                        {
                            continue;
                        }
                        if (SharedFunctions.IsATP(lastDiscreteAnalyte.Value) && cmt.IsAutoAdded && !IsATPAutoComment)
                        {
                            continue;
                        }

                        // Panel Comments
                        // if cmt.ExternalCommentCode is not empty then add to comment code to OBX otherwise add NTEs
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

                            if (cmt.Text.Contains("\r\n"))
                            {
                                string[] comments = cmt.Text.Split(new[] { "\r\n" }, StringSplitOptions.None);
                                foreach (string comment in comments)
                                {
                                    if (Settings.ResultOut_SuppressNTEs)
                                    {
                                        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(comment)), m_oSuppress);
                                    }
                                    else
                                    {
                                        lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(comment)));
                                    }
                                }
                            }
                            else
                            {
                                if (Settings.ResultOut_SuppressNTEs)
                                {
                                    lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text)), m_oSuppress);
                                }
                                else
                                {
                                    lastDiscreteAnalyte.AddNTE(new NTE(Strings.RTrim(cmt.Text)));
                                }
                            }
                        }
                    }
                }
            }

            logger.LogDebug(bdiRpt.TestCode);
            logger.LogDebug(string.Concat("Analyte count:", bdiRpt.AnalyteList.Count));

            // TabularText reports does not have analytes/OBXs they have rather NTEs
            if (!lisPanel.Panel.IsReportable || bdiRpt.AnalyteList.Count > 0 || bdiRpt.NTEs.Count > 0)
            {
                return bdiRpt;
            }
            else
            {
                if (!m_canceled)
                {
                    logger.LogError(string.Format("{1} - '{0}' panel does not have any released analytes.  Panel may be marked as Released in error.", bdiRpt.TestCode, bdiRpt.AccessionNum));
                }
                return null;
            }
        }

        public bool IsCanceledAllergen(Bioreference.LIS.ReportAnalyte analyte)
        {
            if (analyte.ResultValue.ToLower().StartsWith("cancel") &&
                analyte.Analyte.Category.ToLower().Equals("allergen"))
            {
                return true;
            }
            return false;
        }

        private DI.Interface.Report GetAnalyteResultFromObject(LIS.Order lisOrder, LIS.ReportAnalyte lisAnalyte, DIResult bdiRslt, DI.Interface.ReportAnalyte interfaceDiscreteAnalyte, bool tabularNTEHeadingCreated, string orderedTestCode, bool processIfPanelLikeTest, bool processAllergen)
        {
            interfaceDiscreteAnalyte = null;
            tabularNTEHeadingCreated = false;

            // If a canceled allergen, do not send to reporting.
            if (this.IsCanceledAllergen(lisAnalyte))
            {
                m_canceled = true;
                return null;
            }

            // If OutboundChannelId is set, then we pass back the ordering code for the analyte.
            // Change: Ordering always goes out.
            string code = (Settings.ResultOut_ChannelId == 0 ? lisAnalyte.Analyte.Code : lisAnalyte.OrderingAnalyteCodes[0]);
            string name = lisAnalyte.Analyte.Name;

            // Alternate Outbound
            if (lisAnalyte.Analyte.GetType() == typeof(RefAnalyte))
            {
                RefAnalyte refa = (RefAnalyte)lisAnalyte.Analyte;
                if (!refa.AltOutboundTestCode.Equals("")) code = refa.AltOutboundTestCode;
                if (!refa.AltOutboundDescription.Equals("")) name = refa.AltOutboundDescription;
            }

            string[] testInfo;
            string panelLikeTest = GetPanelLikeTest(lisAnalyte.Analyte.Category);
            DI.Interface.Report bdiRpt = null;

            string obrName = "";
            bool isPanelLikeTest = false;

            // NA 11/14/13
            if (panelLikeTest.Contains("^") && processIfPanelLikeTest && processAllergen)
            {
                testInfo = panelLikeTest.Split('^');
                bdiRpt = bdiRslt.FindReport(testInfo[0]);

                obrName = testInfo[1];
                orderedTestCode = testInfo[0];
                isPanelLikeTest = true;
            }

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
                if (!string.IsNullOrEmpty(orderedTestCode)) bdiRpt = bdiRslt.FindReport(orderedTestCode);
                if (bdiRpt == null)
                {
                    if (!isPanelLikeTest)
                    {
                        Bioreference.LIS.OrderTest ot = lisOrder.Tests.Find(code, orderedTestCode);
                        if (ot != null)
                        {
                            obrName = ot.OrderedTestName;
                        }
                        else
                        {
                            obrName = lisOrder.Tests.FindOrderedTestName(orderedTestCode);
                        }
                        if (obrName.Equals("")) obrName = name;
                    }

                    bdiRpt = new DI.Interface.Report(
                        orderedTestCode,
                        lisOrder.Comments,
                        obrName,
                        accessionNumber,
                        ResultService.Common.Utilities.PriorityToString(lisOrder.Priority),
                        lisOrder.DateOfService.ToString(),
                        lisOrder.DateOfCollection.ToString(),
                        null,
                        lisOrder.DateOfService.ToString(),
                        lisOrder.AccountNumber,
                        lisOrder.PrimaryPhysician.LastName,
                        null,
                        null,
                        lisAnalyte.ResultDate.ToString(),
                        lisAnalyte.Analyte.Category,
                        ResultService.Common.Utilities.ConvertStatusToString(status)
                    );

                    // If the ordered code is not an actual analyte
                    Bioreference.LIS.Report report = lisAnalyte.GetParentReport();
                    if (report.FindAnalyte(orderedTestCode, false) == null)
                    {
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
                }
            }

            if (this.FindAnalyte(bdiRpt, code) != null) return null; // Already exists

            bdiRpt.ReportStatus = ResultService.Common.Utilities.ConvertStatusToString(status);

            if (!lisAnalyte.Analyte.IsReportable)
            {
                return bdiRpt;
            }
            else
            {
                string resultValue = ResultService.Common.Utilities.UpdateResultToFollow(lisAnalyte);
                bool sendAoeAsStType = Settings.SendAsSTResultType.Contains(code);

                DI.Interface.ReportAnalyte interfaceAnalyte =
                    new DI.Interface.ReportAnalyte(
                        code,
                        resultValue,
                        lisAnalyte.ReleaseDate.ToString(),
                        null,
                        ResultService.Common.Utilities.ConvertResultTypeToString(lisAnalyte.Analyte.ResultType, lisAnalyte.CodeTypeId == 5, sendAoeAsStType),
                        name,
                        lisAnalyte.Analyte.Units,
                        lisAnalyte.GetReferenceRange(),
                        ResultService.Common.Utilities.ConvertStatusToString(lisAnalyte.ResultStatus),
                        lisAnalyte.Analyte.ReferenceLabId.ToString(),
                        null,
                        lisAnalyte.FlagValue,
                        lisAnalyte.PerformingFacility,
                        lisAnalyte.AccessioningFacility
                    );

                ProcessAnalyteComments(lisAnalyte, interfaceAnalyte);

                if (lisAnalyte.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText ||
                    lisAnalyte.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularTextWithOutHeaders)
                {
                    if (interfaceDiscreteAnalyte == null)
                    {
                        interfaceDiscreteAnalyte = new DI.Interface.ReportAnalyte(
                            bdiRpt.TestCode,
                            "SEE BELOW",
                            lisAnalyte.ReleaseDate.ToString(),
                            "",
                            "ST",
                            bdiRpt.TestDescription,
                            "",
                            "",
                            "F",
                            interfaceAnalyte.ReferenceLabNumber,
                            "",
                            ""
                        );
                    }

                    if (!tabularNTEHeadingCreated && lisAnalyte.Analyte.ReportingType == Bioreference.Common.Lab.reportingType.TabularText)
                    {
                        interfaceDiscreteAnalyte.AddNTE(new NTE(
                            Settings.TestDescriptionTitle +
                            Settings.ResultTitle +
                            Settings.FlagTitle +
                            Settings.ReferenceRangeTitle +
                            Settings.UnitsTitle
                        ));
                        tabularNTEHeadingCreated = true;
                    }

                    List<string> notes = ResultService.Common.Utilities.CreateNoteFromAnalyte(lisAnalyte);
                    foreach (string s in notes)
                    {
                        interfaceDiscreteAnalyte.AddNTE(new NTE(s));
                    }

                    foreach (NTE nte in interfaceAnalyte.NTEs)
                    {
                        nte.Text = Settings.TabularTextNTEPrefix + nte.Text;
                        interfaceDiscreteAnalyte.AddNTE(nte);
                    }

                    bool analyteExists = false;
                    foreach (DI.Interface.ReportAnalyte _analyte in bdiRpt.AnalyteList)
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

                    if (bdiRpt.AnalyteList.Count > 0)
                    {
                        return bdiRpt;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return bdiRpt;
        }
        private DI.Interface.ReportAnalyte FindAnalyte(DI.Interface.Report rpt, string code)
        {
            foreach (DI.Interface.ReportAnalyte r in rpt.AnalyteList)
            {
                if (r.Code == code) return r;
            }
            return null;
        }

        private string GetPanelLikeTest(string testDescription)
        {
            // For Each panelLikeTest As String In Settings.ResultOut_AllergensPanelLikeTests.Split(",")
            //     Dim testInfo() As String = panelLikeTest.Trim.Split("^")
            //     If testInfo(1) = testDescription Then
            //         Return panelLikeTest.Trim
            //     End If
            // Next
            return "";
        }


        private DI.Interface.Report CreateIGE(LIS.Report lisReport, LIS.Order lisOrder)
        {
            DI.Interface.Report interfaceReportIGE = null;

            // Create IGE Serum Analyte here
            Bioreference.LIS.ReportAnalyte _analyte_IGE =
                lisReport.FindAnalyte(Settings.ResultOut_IGECode, true);

            if (_analyte_IGE != null
                && _analyte_IGE.TransmitStatus != transmitStatusType.Released)
            {
                string IGECode = (Settings.ResultOut_ChannelId == 0
                    ? _analyte_IGE.Analyte.Code
                    : _analyte_IGE.OrderingAnalyteCodes[0]);

                string resultValue = (!_analyte_IGE.HasBeenReleased()
                    ? "Pending"
                    : _analyte_IGE.ResultValue);

                interfaceReportIGE = new DI.Interface.Report(
                    IGECode,
                    lisOrder.Comments,
                    _analyte_IGE.Analyte.Name,
                    accessionNumber,
                    ResultService.Common.Utilities.PriorityToString(lisOrder.Priority),
                    lisOrder.DateOfService.ToString(),
                    lisOrder.DateOfCollection.ToString(),
                    null,
                    lisOrder.DateOfService.ToString(),
                    lisOrder.AccountNumber,
                    lisOrder.PrimaryPhysician.LastName,
                    null,
                    null,
                    _analyte_IGE.ResultDate.ToString(),
                    _analyte_IGE.Analyte.Category,
                    ResultService.Common.Utilities.ConvertStatusToString(_analyte_IGE.ResultStatus)
                );

                DI.Interface.ReportAnalyte anlIGE =
                    new DI.Interface.ReportAnalyte(
                        IGECode,
                        resultValue,
                        _analyte_IGE.ReleaseDate.ToString(),
                        null,
                        ResultService.Common.Utilities.ConvertResultTypeToString(_analyte_IGE.Analyte.ResultType),
                        _analyte_IGE.Analyte.Name,
                        _analyte_IGE.Analyte.Units,
                        _analyte_IGE.GetReferenceRange(),
                        ResultService.Common.Utilities.ConvertStatusToString(_analyte_IGE.ResultStatus),
                        _analyte_IGE.Analyte.ReferenceLabId.ToString(),
                        null,
                        _analyte_IGE.FlagValue,
                        _analyte_IGE.PerformingFacility,
                        _analyte_IGE.AccessioningFacility
                    );

                ProcessAnalyteComments(_analyte_IGE, anlIGE);

                interfaceReportIGE.AddAnalyte(anlIGE);
            }

            return interfaceReportIGE;
        }
        private bool CheckTNPComment(string text)
        {
            // Return (From a In Settings.TNPCommentsToReport
            //        Where text.ToLower().Contains(a.ToString().ToLower())
            //        Select a).Any()
            foreach (string a in Settings.TNPCommentsToReport)
            {
                if (text.ToLower().Contains(a.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
        private bool CheckATPComment(string text)
        {
            // Return (From a In Settings.ATPCommentsToReport
            //        Where text.ToLower().Contains(a.ToString().ToLower())
            //        Select a).Any()
            foreach (string a in Settings.ATPCommentsToReport)
            {
                if (text.ToLower().Contains(a.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        private void ProcessAnalyteComments(LIS.ReportAnalyte lisAnalyte, DI.Interface.ReportAnalyte interfaceAnalyte)
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

                // mzia: Individual Analyte Comments
                // mzia: if cmt.ExternalCommentCode is not empty then add to comment code to OBX otherwise add NTEs
                if (cmt.ExternalCommentCode != "")
                {
                    interfaceAnalyte.CommentIdMappings.Add(
                        cmt.ExternalCommentCode + "^" + ((int)cmt.ExternalCommentType).ToString()
                    );
                }
                else
                {
                    // If not the first note, then put a blank line
                    if (aCount > 1)
                    {
                        interfaceAnalyte.AddNTE(new NTE(""));
                    }

                    if (Settings.ResultOut_SuppressNteAsBlockRefLabIds.Contains(lisAnalyte.Analyte.ReferenceLabId.ToString()))
                    {
                        bool foundFlag = false;
                        foreach (string nte in m_oSuppress.StringList)
                        {
                            if (cmt.Text.TrimEnd().Replace("\r\n", "") == nte)
                            {
                                foundFlag = true;
                                break;
                            }
                        }
                        if (!foundFlag)
                        {
                            foreach (string s in cmt.Text.Split(new[] { "\r\n" }, StringSplitOptions.None))
                            {
                                interfaceAnalyte.AddNTE(new NTE(s.TrimEnd()));
                            }
                        }
                    }
                    else
                    {
                        if (cmt.Text.Contains("\r\n"))
                        {
                            string[] comments = cmt.Text.Split(new[] { "\r\n" }, StringSplitOptions.None);
                            foreach (string comment in comments)
                            {
                                if (Settings.ResultOut_SuppressNTEs)
                                {
                                    interfaceAnalyte.AddNTE(new NTE(comment.TrimEnd()), m_oSuppress);
                                }
                                else
                                {
                                    interfaceAnalyte.AddNTE(new NTE(comment.TrimEnd()));
                                }
                            }
                        }
                        else
                        {
                            if (Settings.ResultOut_SuppressNTEs)
                            {
                                interfaceAnalyte.AddNTE(new NTE(cmt.Text.TrimEnd()), m_oSuppress);
                            }
                            else
                            {
                                interfaceAnalyte.AddNTE(new NTE(cmt.Text.TrimEnd()));
                            }
                        }
                    }

                    aCount += 1;
                }
            }
        }


        private bool CanSendAccessionNotes(string accessionNbr)
        {
            // If none exist, always send comments
            if (Settings.ResultOut_SendCommentsAccStartsWith.Count == 0)
                return true;

            return (accessionNbr.Length == 9 &&
                    Settings.ResultOut_SendCommentsAccStartsWith.Contains(accessionNbr.Substring(0, 2)));
        }
        private bool AddAnalyteResultToReport(LIS.Order lisOrder,LIS.ReportAnalyte a, DIResult interfaceResult, DI.Interface.ReportAnalyte discreteAnalyte, bool tabularNTEHeadingCreated, bool isReportable, string orderedTestCode, bool processIfPanelLikeTest, bool processAllergen)
        {
            logger.LogDebug(string.Concat("Adding...", a.Code, " Reportable: ", a.Analyte.IsReportable, " -", isReportable));

            DI.Interface.Report bdiRpt;

            // NA 11/14/13
            // bdiRpt = GetAnalyteResultFromObject(lisOrder, a, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, orderedTestCode, processIfPanelLikeTest)
            bdiRpt = GetAnalyteResultFromObject(
                lisOrder,
                a,
                interfaceResult,
                discreteAnalyte,
                tabularNTEHeadingCreated,
                orderedTestCode,
                processIfPanelLikeTest,
                processAllergen);

            if (bdiRpt == null)
            {
                logger.LogDebug(string.Concat("Not Added...", a.Code));
                return false;
            }

            // If this is an IGE report then add to the bottom
            // NA 11/14/13
            // If a.Code = Settings.ResultOut_IGECode Then
            if (a.Code == Settings.ResultOut_IGECode && processAllergen)
            {
                m_rptIGE = bdiRpt;
            }
            else
            {
                // Don't re-add if already exists
                if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                {
                    interfaceResult.rReports.Add(bdiRpt);
                }
            }

            return true;
        }

        private bool AddPanelResultToReport(LIS.Order lisOrder, LIS.ReportAnalytePanel p, DIResult interfaceResult, DI.Interface.ReportAnalyte discreteAnalyte, bool tabularNTEHeadingCreated, bool isReportable, string orderedTestCode, bool processIfPanelLikeTest, bool processAllergen)
        {
            logger.LogDebug(string.Concat("Adding...", p.PanelCode, " Reportable: ", p.Panel.IsReportable, " -", isReportable));

            DI.Interface.Report bdiRpt;

            // NA 11/14/13
            // bdiRpt = GetPanelResultFromObject(lisOrder, p, interfaceResult, discreteAnalyte, tabularNTEHeadingCreated, orderedTestCode, processIfPanelLikeTest)
            bdiRpt = GetPanelResultFromObject(
                lisOrder,
                p,
                interfaceResult,
                discreteAnalyte,
                tabularNTEHeadingCreated,
                orderedTestCode,
                processIfPanelLikeTest,
                processAllergen);

            if (bdiRpt == null)
            {
                logger.LogDebug(string.Concat("Not Added...", p.PanelCode));
                return false;
            }
            else
            {
                logger.LogDebug("Added");
            }

            // If this is an IGE report then add to the bottom
            // NA 11/14/13
            // If bdiRpt.TestCode = Settings.ResultOut_IGECode Then
            if (bdiRpt.TestCode == Settings.ResultOut_IGECode && processAllergen)
            {
                m_rptIGE = bdiRpt;
            }
            else
            {
                // Don't re-add if already exists
                if (interfaceResult.FindReport(bdiRpt.TestCode) == null)
                {
                    interfaceResult.rReports.Add(bdiRpt);
                }
            }

            return true;
        }



    }
}

