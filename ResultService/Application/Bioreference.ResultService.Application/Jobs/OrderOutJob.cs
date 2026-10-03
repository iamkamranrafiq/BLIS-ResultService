using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.OrderOut;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using HL7Parser.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;


namespace Bioreference.ResultService.Application.Jobs
{
    public class OrderOutJob : IBLISJob
    {
        private ILogger<OrderOutJob> _logger;
        private readonly ISettingService _settingsProvider;
        private readonly AppSettingsOrderEngine _appSettings;
        private readonly IPayloadSenderApiClient _payloadSenderApiClient;
        private string connectionString = string.Empty;

        public OrderOutJob(ILogger<OrderOutJob> logger, ISettingService settingsProvider, IOptions<AppSettingsOrderEngine> options, IPayloadSenderApiClient payloadSenderApiClient)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _appSettings = options.Value;
            _payloadSenderApiClient = payloadSenderApiClient;

        }

        public async Task<bool> Execute()
        {
            Bioreference.LIS.Order order = default;
            bool isSuccess = false;
            string sMessage = "";
            string responseMessage = "";
            int refLabId = _appSettings.ReferenceLabId;
            string fileName = $"OutboundOrder_{DateTime.Now:yyyy-MM-dd}.txt";

            _logger.LogDebug(
                "Entity: {Entity}; Event: {Event}; RefLabId: {RefLabId}",
                "OrderOut",
                "Start",
                refLabId
            );

            connectionString = _settingsProvider.GetConnectionString("Bioreference.LIS");

            try
            {
                // Stopwatch for fetching next order
                var fetchStopwatch = Stopwatch.StartNew();
                Bioreference.LIS.Order o =
                    await Task.Run(() => Bioreference.LIS.Order.FetchNextToSend(refLabId));
                fetchStopwatch.Stop();

                ActivityHelper.SetAccessionLogKey(o.AccessionNbr);
                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; RefLabId: {RefLabId}; ElapsedTime: {ElapsedTime} ms",
                    "OrderOut",
                    "FetchNextToSend",
                    refLabId,
                    fetchStopwatch.ElapsedMilliseconds
                );

                if (o != null)
                {
                    try
                    {
                        order = o;
                        var tests = new List<TestInfo>();

                        foreach (LIS.OrderTest t in order.Tests.List)
                        {
                            if (!t.IsSentOut)
                            {
                                if (_appSettings.AddTestFromTestMaster)
                                {
                                    var testStopwatch = Stopwatch.StartNew();
                                    var ti = await Task.Run(() =>
                                        TestMasterWrapper.Fetch(t.TestCode, order: ref o));
                                    testStopwatch.Stop();

                                    _logger.LogInformation(
                                        "Entity: {Entity}; Event: {Event}; TestCode: {TestCode}; ElapsedTime: {ElapsedTime} ms",
                                        "OrderOut",
                                        "FetchTestMaster",
                                        t.TestCode,
                                        testStopwatch.ElapsedMilliseconds
                                    );

                                    if (ti != null && Equals(ti.ReferenceLabId, refLabId))
                                    {
                                        tests.Add(new TestInfo(
                                            t.TestCode,
                                            ti.ReferenceLabeAnalyteCode,
                                            ti.Name,
                                            t.IsSentOut,
                                            ti.ReferenceLabOrderingAnalyteCode));

                                        t.IsSentOut = true;
                                    }
                                    else
                                    {
                                        _logger.LogError(
                                            "Entity: {Entity}; Event: {Event}; AccessionNbr: {AccessionNbr}; TestCode: {TestCode}",
                                            "OrderOut",
                                            "TestNotFound_TestMaster",
                                            o.AccessionNbr,
                                            t.TestCode
                                        );

                                        t.IsSentOut = true;
                                    }
                                }
                                else
                                {
                                    var testStopwatch = Stopwatch.StartNew();
                                    var ti = await Task.Run(() =>
                                        TestDetail.Fetch(t.TestCode, 0));
                                    testStopwatch.Stop();

                                    _logger.LogInformation(
                                        "Entity: {Entity}; Event: {Event}; TestCode: {TestCode}; ElapsedTime: {ElapsedTime} ms",
                                        "OrderOut",
                                        "FetchTestDetail",
                                        t.TestCode,
                                        testStopwatch.ElapsedMilliseconds
                                    );

                                    if (ti != null && Equals(ti.GetReferenceLabId(), refLabId))
                                    {
                                        tests.Add(new TestInfo(
                                            t.TestCode,
                                            ti.GetReferenceLabAnalyteCode(),
                                            ti.Analyte.Name,
                                            t.IsSentOut,
                                            ti.GetReferenceLabOrderingAnalyteCode()));

                                        t.IsSentOut = true;
                                    }
                                    else
                                    {
                                        _logger.LogError(
                                            "Entity: {Entity}; Event: {Event}; AccessionNbr: {AccessionNbr}; TestCode: {TestCode}",
                                            "OrderOut",
                                            "TestNotFound_B2",
                                            o.AccessionNbr,
                                            t.TestCode
                                        );

                                        t.IsSentOut = true;
                                    }
                                }
                            }
                        }

                        if (tests.Count > 0)
                        {
                            sMessage = GenerateMessage(refLabId, order, tests);

                            _logger.LogInformation(
                                "Entity: {Entity}; Event: {Event}; AccessionNbr: {AccessionNbr}; TestCount: {TestCount}",
                                "OrderOut",
                                "MessageGenerated",
                                order.AccessionNbr,
                                tests.Count
                            );

                            await _payloadSenderApiClient.SendPayloadAsync(sMessage, _appSettings.LabName);

                            _logger.LogInformation(
                                "Entity: {Entity}; Event: {Event}; AccessionNbr: {AccessionNbr}",
                                "OrderOut",
                                "PayloadSent",
                                order.AccessionNbr
                            );
                        }

                        if (order.IsValid)
                        {
                            order.Save();

                            _logger.LogDebug(
                                "Entity: {Entity}; Event: {Event}; AccessionNbr: {AccessionNbr}",
                                "OrderOut",
                                "OrderSaved",
                                order.AccessionNbr
                            );
                        }
                        else
                        {
                            responseMessage =
                                $"RefLabId - {refLabId}, AccessionNbr - {order.AccessionNbr}, Invalid: {order.GetCompleteRules()}";

                            _logger.LogDebug(
                                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                                "OrderOut",
                                "InvalidOrder",
                                responseMessage
                            );

                            AppendMessage(_appSettings.OrderOut_StatusMsgPath, fileName, responseMessage);
                            return false;
                        }

                        _logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; AccessionNbr: {AccessionNbr}",
                            "OrderOut",
                            "Processed",
                            order.AccessionNbr
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Entity: {Entity}; Event: {Event}; RefLabId: {RefLabId}; OrderId: {OrderId}",
                            "OrderOut",
                            "ProcessingError",
                            refLabId,
                            o.ID
                        );

                        return false;
                    }

                    AppendMessage(_appSettings.OrderOut_ArchiveMsgPath, fileName, sMessage);
                    isSuccess = true;
                }
                else
                {
                    responseMessage = $"Outbound Order Save: RefLabId - {refLabId}, AccessionNbr - #{order?.AccessionNbr}, Invalid: {order?.GetCompleteRules()}";
                    _logger.LogDebug(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "OrderOut",
                        "Failed",
                        responseMessage
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "OrderOut",
                    "ExecutionFailed",
                    "Outbound order execution failed."
                );

                return false;
            }

            _logger.LogDebug(
                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                "OrderOut",
                "Completed",
                "Job completed successfully."
            );

            return isSuccess;
        }

        private void AppendMessage(string folder, string fileName, string message)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, fileName), message + Constants.vbCrLf);
        }

        public bool SuppressTestCode(string testCode, string acctNbr)
        {
            if (_appSettings.SupressTestCodes)
            {
                if (_appSettings.SupressTestCodes_AcctNbrByPass.Contains(acctNbr))
                    return false;

                testCode = testCode.TrimStart('0').Trim();

                if (_appSettings.SupressTestCodes_AllowCodes.Contains(testCode))
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


        public string GenerateMessage(int refLabId, Bioreference.LIS.Order order, List<TestInfo> tests)
        {
            var message = new ORMMessage();

            // MSH Segment
            message.MSH = new MSH();
            message.MSH.SendingApplication = _appSettings.MSH_SendingApp;
            message.MSH.SendingFacilityCode = _appSettings.MSH_SendingFacilityCode;
            message.MSH.ReceivngApplication = _appSettings.MSH_ReceivingApp;
            message.MSH.ReceivingFacility = _appSettings.MSH_ReceivingFacility;
            message.MSH.MessageTime = DateTime.Now.ToString();
            message.MSH.MessageType = "ORM";
            message.MSH.MessageEvent = "R01";
            message.MSH.MessageID = DateTime.Now.ToString("yyyyMMddhhmmssfff");
            message.MSH.ProcessingId = "P";
            message.MSH.VersionId = Convert.ToString(2.3);
            message.MSH.AcceptAckType = "AL";

            message.PatientVisit = new PatientVisit();
            message.PatientVisit.Set_ID = "1";

            // Patient Segment
            message.PatientInfo = new PatientInfo();
            message.PatientInfo.PatientIdentifierList = order.AccessionNbr;
            message.PatientInfo.Set_ID = "1"; // Only one patient per message
            message.PatientInfo.LastName = order.Patient.LastName;
            message.PatientInfo.FirstName = order.Patient.FirstName;
            message.PatientInfo.Sex = Bioreference.ResultService.Common.Utilities.GenderToSex(order.Patient.Gender);
            message.PatientInfo.DOB = order.Patient.DateOfBirth;

            var newOrder = new DI.Interface.Order
            {
                OrderControlId = "NW",
                AccessionNumber = order.AccessionNbr,
                ReferenceLabNumber = refLabId.ToString(),
                ClientAccountNumber = order.AccountNumber,
                OrderingDoctorName = order.PrimaryPhysician.FullName,
                Priority = order.Priority.ToString(),
                OrderDate = order.DateOfService.ToString(),
                CollectDate = order.DateOfCollection.ToString()
            };

            message.Order = new List<DI.Interface.Order> { newOrder };


            // Order Comments
            if (order.Comments.Length > 0)
            {
                for (int i = 0; i < order.Comments.Length; i++)
                {
                    message.OrderComment = new List<LabComment>();
                    message.OrderComment[i].NoteText = order.Comments[i].ToString();
                }
            }
            else
            {
                message.OrderComment = new List<LabComment>();
                LabComment note = new LabComment();
                note.NoteText = string.Empty;
                message.OrderComment.Add(note);
            }

            // Order Tests
            int x = 0;
            message.GrpORMOrder = new List<GrpORMOrder>();

            foreach (var t in tests)
            {
                if (SuppressTestCode(t.ReferenceCode, order.AccountNumber))
                {
                    //WarningMessages.Add(string.Format(
                    //    "Outbound test order suppressed for accession# {0} TestCode {1} (Ref Lab {2}).",
                    // order.AccessionNbr, t.TestCode, t.ReferenceCode));
                }
                else
                {
                    var tx = new Tx
                    {
                        SetId = (x + 1).ToString(),
                        Priority = Bioreference.ResultService.Common.Utilities.PriorityToString(order.Priority),
                        TestCode = t.ReferenceOrderingCode,
                        TestDescription = t.Name?.Trim(),
                        CollectDatetime = order.DateOfCollection,
                        OrderDateTime = order.DateOfService,
                        PlacerOrderNumber = order.AccessionNbr
                    };

                    var orderGroup = new GrpORMOrder
                    {
                        Tx = new List<Tx> { tx }
                    };

                    message.GrpORMOrder.Add(orderGroup);

                    x++;
                }
            }

            return ProcessInbound(message);


        }

        public string ProcessInbound(ORMMessage message)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string localDependencyPath = Path.Combine(baseDirectory, "HL7Configuration");
            var objectBuilder = new HL7Builder();

            string jsonORMFilePath = Path.Combine(localDependencyPath, "ORM_Mapping.json");

            if (!File.Exists(jsonORMFilePath))
            {
                throw new FileNotFoundException("Mapping JSON files not found.");
            }

            string mappingORMJson = File.Exists(jsonORMFilePath) ? File.ReadAllText(jsonORMFilePath) : null;
            var objJson = JsonConvert.SerializeObject(message);

            string hl7 = objectBuilder.CreateHl7FromObject(mappingORMJson, objJson);

            return hl7;
        }
    }
}
