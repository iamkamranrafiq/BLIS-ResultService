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

namespace Bioreference.ResultService.Application.Payload
{
    public class SSUPayloadService : ISSUPayloadService
    {
        private ILogger<SSUPayloadService> logger;
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
        private int pidCounter;
        private string accessionNumber;
        private string mappingFileName = "SSU_Mapping.json";

        public SSUPayloadService(ILogger<SSUPayloadService> logger, ISettingService settingsProvider, IOptions<AppSettingsReportOut> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;            
        }

        async Task<string[]> ISSUPayloadService.Message(string accessionNbr, DateTime dateServiced, bool? withOBX = true)
        {   
            string[] ssuMessages = Array.Empty<string>();
            bool isReportable = true;
            Bioreference.LIS.Report blReport = Bioreference.LIS.Report.Fetch(accessionNbr, dateServiced);
            if (!(blReport == null) && blReport.ID > 0)
            {
                try
                {
                    //Generate Message
                    ssuMessages = GenerateMessage(blReport, isReportable, withOBX);
                    logger.LogInformation("({Accession}): ISSUPayload_SSUMessage='{Payload}'",    accessionNumber, string.Join(Environment.NewLine + "---" + Environment.NewLine, ssuMessages?? Array.Empty<string>()));

                    if (ssuMessages == null || ssuMessages.Length == 0)
                    {
                        logger.LogWarning($"({accessionNumber}): Empty message generated");
                    }                    
                }
                catch (Exception ex)
                {
                    logger.LogError($"({accessionNumber}): SSUMessage Failed='{ex.Message}'");
                }
            }
            return ssuMessages;
        }

        public string[] GenerateMessage(Bioreference.LIS.Report blReport, bool isReportable, bool? withOBX = true)
        {
            string[] msg = Array.Empty<string>();
            logger.LogInformation($"({blReport.AccessionNbr}): GenerateMessage HasValidPrefix='{this.HasValidPrefixForAccessionStatus(blReport.AccessionNbr)}' HasReleased='{blReport.HasReleased()}' sendToFollow='{sendToFollow}'");
            Bioreference.ResultService.DI.Interface.DIResult rslt = GetResultFromObject(blReport, isReportable);

            
            if (!(rslt == null))
            {
                msg = CreateMessage(rslt, isReportable, blReport,withOBX);
            }

            logger.LogInformation($"({accessionNumber}): GenerateMessage: No message generated.");
            return msg;
        }

        /// <summary>
        /// Create a HL7 message from the Results and Report
        /// </summary>
        /// <param name="rResult"></param>
        /// <param name="isReportable"></param>
        /// <param name="blReport"></param>
        /// <returns></returns>        
        private string[] CreateMessage(Bioreference.ResultService.DI.Interface.DIResult result, bool isReportable, Bioreference.LIS.Report blReport, bool? withOBX = true)
        {
            var messages = new List<string>();
            var timestamp = blReport.DateServiced.ToString("yyyyMMddHHmmss");

            // ---------- Common MSH ----------
            var commonMsh = new List<MSH>
            {
                new MSH
                {
                    MessageType          = "SSU",
                    MessageEvent         = "U03",
                    SendingApplication   = "DI",
                    SendingFacilityCode  = "BRLI",
                    //SendingFacility      = "",
                    ReceivngApplication  = "B2",
                    ReceivingFacility    = "BRLI",
                    MessageTime          = timestamp,
                    MessageID            = timestamp,
                    ProcessingId         = "P", //result.rReports[0].LabReportStatus
                    VersionId            = Convert.ToString(2.4),
                    AcceptAckType        = "AL",
                    SequenceNumber       = Convert.ToString(blReport.OrderId)
                }
            };

            //// ---------- Common Specimen Detail ----------
            //var specimen = new SpecimenContainerDetail
            //{
            //    ExternalAccessionIdentifier = string.Empty,
            //    AccessionIdentifier = blReport.AccessionNbr, // .AccessionIdentifier,
            //    ContainerIdentifier = string.Empty,
            //    PrimaryContainerIdentifier = string.Empty,
            //    EquipmentContainerIdentifier = string.Empty,
            //    SpecimenSource = string.Empty,
            //    RegistrationDateTime = string.Empty,
            //    ContainerStatus = "I"
            //};

            try
            {
                logger.LogInformation("({Accession}): Start CreateMessage grouping by InstrumentId + InstrumentLoadTime", accessionNumber);

                // ---------- Collect all analytes ----------

                var allAnalytes = result?.rReports?
                    .Where(r => r?.AnalyteList != null && r.AnalyteList.Count > 0)
                    .SelectMany(rpt =>
                        rpt.AnalyteList.Select(a =>
                        {
                            var tiLite = Bioreference.Common.TestMaster.TestLite.Fetch(a.Code);
                            

                            // Find the ReportAnalyte using the sample logic
                            var la = tiLite.CodeTypeId == 3
                                ? blReport.FindAnalyteInPanel(tiLite.TestCode, a.Code)
                                : blReport.FindAnalyte(a.Code, true);

                            // Update Instrument / InstrumentId if blank
                            if (la != null && string.IsNullOrEmpty(la.InstrumentId))
                            {
                                var ti = Bioreference.Common.TestMaster.TestInstruments.Fetch(
                                    la.Code,
                                    1
                                );

                                if (ti != null && ti.TestInstrumentList.Count > 0)
                                {
                                    // Update the InstrumentId
                                    la.Instrument = Convert.ToString(ti.TestInstrumentList[0].InstrumentName);
                                    la.InstrumentId = Convert.ToString(ti.TestInstrumentList[0].InstrumentName);
                                    a.InstrumentId = Convert.ToString(ti.TestInstrumentList[0].InstrumentName);
                                }
                                else
                                {
                                    if (tiLite.CodeTypeId != 5)
                                    {
                                        la.Instrument = "LoadTest Instrument";
                                        la.InstrumentId = "LoadTest Instrument";
                                        a.InstrumentId = "LoadTest Instrument";
                                    }
                                }
                            }

                            return (A: a, LA: la);
                        }))
                    .ToList()
                    ?? new List<(Bioreference.ResultService.DI.Interface.ReportAnalyte A, Bioreference.LIS.ReportAnalyte LA)>();


                //// ---------- If no analytes, No OBX
                //if (allAnalytes.Count == 0)
                //    return new[] { BuildFallbackMessage(commonMsh, specimen, blReport) };

                // ---------- Group by valid InstrumentId + LoadTime ----------
                ////var groups = allAnalytes
                ////    .Where(p =>
                ////        p.LA != null &&
                ////        !string.IsNullOrWhiteSpace(p.LA.InstrumentId) &&
                ////        //!p.LA.InstrumentId.Equals("N/A", StringComparison.OrdinalIgnoreCase) &&
                ////        p.LA.InstrumentLoadTime != null)
                ////    .GroupBy(p => new
                ////    {
                ////        InstrumentId = p.LA.InstrumentId.Trim(),
                ////        LoadTimeKey = GetLoadTimeKey(p.LA.InstrumentLoadTime),
                ////        AccessioningFacility = p.LA.AccessioningFacility
                ////    })
                ////    .ToList();
                ///
                var groups =
                    allAnalytes
                        .Where(p =>
                            p.LA != null &&
                            !string.IsNullOrWhiteSpace(p.LA.InstrumentId) &&
                            p.LA.InstrumentLoadTime != null)
                        
                        .GroupBy(p => new
                        {
                            LoadTimeKey = GetLoadTimeKey(p.LA.InstrumentLoadTime),
                            AccessioningFacility = p.LA.AccessioningFacility
                        })
                        // Remore 'LoadTest Instrument' only if there are other instruments
                        .SelectMany(g =>
                        {
                            var hasOtherInstrument = g.Any(x =>
                                !x.LA.InstrumentId.Trim()
                                    .Equals("LoadTest Instrument", StringComparison.OrdinalIgnoreCase));

                            if (hasOtherInstrument)
                            {
                                return g.Where(x =>
                                    !x.LA.InstrumentId.Trim()
                                        .Equals("LoadTest Instrument", StringComparison.OrdinalIgnoreCase));
                            }
                            return g;
                        })
                        // 3) Final grouping
                        .GroupBy(p => new
                        {
                            InstrumentId = p.LA.InstrumentId.Trim(),
                            LoadTimeKey = GetLoadTimeKey(p.LA.InstrumentLoadTime),
                            AccessioningFacility = p.LA.AccessioningFacility
                        })
                        .ToList();



                //// ---------- If all invalid , No OBX
                //if (groups.Count == 0)
                //    return new[] { BuildFallbackMessage(commonMsh, specimen, blReport) };

                // ---------- Build one HL7 per group ----------
                foreach (var grp in groups)
                {
                    var obxes = grp.Select((pair, idx) =>
                    {
                        var a = pair.A;
                        var la = pair.LA;

                        var obx = new OBX
                        {
                            Set_ID = Convert.ToString(idx + 1),
                            SPMTNP = string.Empty,
                            Code = a.Code,                            
                            Status = "I" // a.Status
                            //InstrumentId = a.InstrumentId
                            //ValueType = a.ValueType,
                            //Description = a.Description,
                            //Value = a.Value,
                            //Units = a.Units,
                            //ReportDate = a.ReportDate.DateTimeStamp()
                        };

                        //var obx = new OBX
                        //{
                        //    ValueType = "1",
                        //    Code = "2",
                        //    Description = "3",
                        //    Value = "4",
                        //    Units = "5",
                        //    ReferenceRange = "6",
                        //    Flag = "7",
                        //    Status = "8",
                        //    ReportDate = "10",
                        //    ReferenceLabId = "11",
                        //    Set_ID = "12",
                        //    MachineSequence = "13",
                        //    Rack = "14",
                        //    RackPosition = "15",
                        //    InstrumentId = "16",
                        //    Comments_Reference = "17",
                        //    ReportingType = "18",
                        //    TechUser = "19",
                        //    ReleaseUser = "20",
                        //    PerformLocation = "21",
                        //    TechUserFName = "22",
                        //    TechUserMName = "23",
                        //    COCReleasedDate = "24",
                        //    AttachmentMultipart = "25",
                        //    AttachmentType = "26",
                        //    AttachmentBase = "27",
                        //    ProducerName = "28",
                        //    PresumptiveHold = "29",
                        //    SPMTNP = "30"
                        //};

                        //if (!string.IsNullOrEmpty(la?.InstrumentId) &&
                        //    !la.InstrumentId.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                        //    obx.InstrumentId = la.InstrumentId.Trim();

                        return obx;
                    }).ToList();


                    var oMessage = new Bioreference.ResultService.DI.Interface.SSUMessage
                    {
                        MSH = commonMsh,
                        EquipmentDetail = new EquipmentDetail
                        {
                            EquipmentInstanceIdentifier = grp.Key.InstrumentId,
                            EventDateTime = grp.Key.LoadTimeKey
                        },
                        // ---------- Common Specimen Detail ----------
                        SpecimenContainerDetail = new SpecimenContainerDetail
                        {
                            ExternalAccessionIdentifier = string.Empty,
                            AccessionIdentifier = blReport.AccessionNbr, // .AccessionIdentifier,
                            ContainerIdentifier = string.Empty,
                            PrimaryContainerIdentifier = grp.Key.InstrumentId,
                            EquipmentContainerIdentifier = string.Empty,
                            SpecimenSource = string.Empty,
                            RegistrationDateTime = string.Empty,
                            ContainerStatus = "I"
                        }                        
                    };
                    if (withOBX == true)
                    {
                        oMessage.Result = obxes;
                    }
                    else
                    {
                        oMessage.Result = new List<OBX>();
                    }

                    var hl7 = CleanHl7(ToHL7(oMessage, mappingFileName));
                    messages.Add(hl7);

                    logger.LogInformation("({Accession}): HL7 group [{Instrument}|{Time}] generated.",
                        accessionNumber, grp.Key.InstrumentId, grp.Key.LoadTimeKey);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "({Accession}): CreateMessage() failed.", accessionNumber);
            }

            logger.LogInformation("({Accession}): Generated {Count} HL7 message(s).", accessionNumber, messages.Count);
            return messages.ToArray();


            string BuildFallbackMessage(List<MSH> msh, SpecimenContainerDetail specimenDetail, Bioreference.LIS.Report report)
            {
                var oMessage = new Bioreference.ResultService.DI.Interface.SSUMessage
                {
                    MSH = msh,
                    EquipmentDetail = new EquipmentDetail
                    {
                        EquipmentInstanceIdentifier = "1001",
                        EventDateTime = report.DateServiced.ToString("yyyyMMddHHmmss")
                    },
                    SpecimenContainerDetail = specimenDetail,
                    Result = new List<OBX>()
                };

                return CleanHl7(ToHL7(oMessage, "SSU_Mapping.json"));
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
        }


        public string ToHL7(SSUMessage message, string mappingFile)
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
            logger.LogInformation($"({accessionNumber}): Archive to database...");
            string timeStamp = "";
            try
            {
                timeStamp = UtilitiesSSU.ArchiveStatus(msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"({accessionNumber}): SendNoBillMessageToVertex='{ex.Message}'");
            }
            if (string.IsNullOrEmpty(timeStamp))
            {
                timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //ErrorMessages.Add("Failed to archive vertex message to database.");
                logger.LogError($"({accessionNumber}): Failed to archive vertex message to database.");
            }
            logger.LogInformation($"({accessionNumber}): Archive to database done.");
            var obCHM = new OutboundMessageCHM(accessionNum);
            obCHM.Message = msg;
            obCHM.Save();
            logger.LogInformation($"({accessionNumber}): Writing to table 'OutboundMessageCHM' done, msg='{msg}' and CreateFile value {obCHM.CreateFile}");
            // Dim fileName As String
            // fileName = String.Concat(Settings.Outbound_StatusMessagePaths(outboundStatusMessagePathIndex), Settings.BIOFilePrefix, timeStamp, ".txt")

            // logger.LogInformation(String.Concat("Writing File:", fileName))
            // Dim sw As New System.IO.StreamWriter(fileName, True)
            // sw.WriteLine(msg)
            // sw.Close()
            // logger.LogInformation("Writing File done.")
            return msg;

        }

        public bool HasValidPrefixForAccessionStatus(object accessionNbr)
        {
            return true;
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

        private Bioreference.ResultService.DI.Interface.Report CreateIge(Bioreference.LIS.Report lisReport, Bioreference.LIS.Order lisOrder)
        {
            Bioreference.ResultService.DI.Interface.Report interfaceReportIge = default;
            // Create IGE Serum Analyte here
            Bioreference.LIS.ReportAnalyte analyteIge = lisReport.FindAnalyte(settings.ResultOut_IGECode, true);
            if (analyteIge is not null && analyteIge.TransmitStatus != transmitStatusType.Released)
            {
                string igeCode = Conversions.ToString(settings.ResultOut_ChannelId == 0 ? analyteIge.Analyte.Code : analyteIge.OrderingAnalyteCodes[0]);
                string resultValue = !analyteIge.HasBeenReleased() ? "Pending" : analyteIge.ResultValue;
                interfaceReportIge = new Bioreference.ResultService.DI.Interface.Report(igeCode, lisOrder.Comments, analyteIge.Analyte.Name, accessionNumber, UtilitiesSSU.PriorityToString(lisOrder.Priority), lisOrder.DateOfService.ToString(), lisOrder.DateOfCollection.ToString(), default, lisOrder.DateOfService.ToString(), lisOrder.AccountNumber, lisOrder.PrimaryPhysician.LastName, default, default, analyteIge.ResultDate.ToString(), analyteIge.Analyte.Category, UtilitiesSSU.ConvertStatusToString(analyteIge.ResultStatus));
                var anlIge = new Bioreference.ResultService.DI.Interface.ReportAnalyte(igeCode, resultValue, analyteIge.ReleaseDate.ToString(), default, UtilitiesSSU.ConvertResultTypeToString(analyteIge.Analyte.ResultType), analyteIge.Analyte.Name, analyteIge.Analyte.Units, analyteIge.GetReferenceRange(), UtilitiesSSU.ConvertStatusToString(analyteIge.ResultStatus), analyteIge.Analyte.ReferenceLabId.ToString(), default, analyteIge.FlagValue, analyteIge.PerformingFacility, analyteIge.AccessioningFacility, analyteIge.RefLabPerformingFacilityId); // _analyte_IGE.GetFlaggedValue)
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

    internal static class UtilitiesSSU
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

        internal static List<string> CreateNoteFromAnalyte(Bioreference.LIS.ReportAnalyte a, int pos = 1)
        {

            string testDescriptionText = "";
            string resultText = "";
            string flagText = "";
            string referenceRangeText = "";
            string unitsText = "";
            bool addlLine = false;
            int startPos;

            var list = new List<string>();

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")));
            if (a.Analyte.Name.Length > startPos)
                testDescriptionText = a.Analyte.Name.Substring(startPos);
            if (testDescriptionText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")))
            {
                testDescriptionText = testDescriptionText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")));
                addlLine = true;
            }
            testDescriptionText = testDescriptionText.PadRight((Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionTitle")).Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")));
            if (a.ResultValue.Length > startPos)
                resultText = a.ResultValue.Substring(startPos);
            if (resultText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")))
            {
                resultText = resultText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")));
                addlLine = true;
            }
            resultText = resultText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultTitle").Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")));
            if (a.FlagValue.Length > startPos)
                flagText = a.FlagValue.Substring(startPos);
            if (flagText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")))
            {
                flagText = flagText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")));
                addlLine = true;
            }
            flagText = flagText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagTitle").Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")));
            if (a.GetReferenceRange().Length > startPos)
                referenceRangeText = a.GetReferenceRange().Substring(startPos);
            if (referenceRangeText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")))
            {
                referenceRangeText = referenceRangeText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")));
                addlLine = true;
            }
            referenceRangeText = referenceRangeText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeTitle").Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")));
            if (a.Analyte.Units.Length > startPos)
                unitsText = a.Analyte.Units.Substring(startPos);
            if (unitsText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")))
            {
                unitsText = unitsText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")));
                addlLine = true;
            }

            unitsText = unitsText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsTitle").Length, ' ');

            list.Add(string.Concat(testDescriptionText, resultText, flagText, referenceRangeText, unitsText));

            //if (addlLine)
            //{
            //    list.AddRange(CreateNoteFromAnalyte(a, pos + 1));
            //}

            return list;

        }

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
