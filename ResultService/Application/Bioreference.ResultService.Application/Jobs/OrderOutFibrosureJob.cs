using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.OrderOutFibrosure;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using HL7Parser.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;
using OBX = Bioreference.ResultService.DI.Interface.OBX;


namespace Bioreference.ResultService.Application.Jobs
{
    public class OrderOutFibrosureJob : IBLISJob
    {
        private ILogger<OrderOutFibrosureJob> _logger;
        private readonly ISettingService _settingsProvider;
        private readonly AppSettingsOrderFibrosureEngine _appSettings;
        private readonly IPayloadSenderApiClient _payloadSenderApiClient;
        private string connectionString = string.Empty;

        public OrderOutFibrosureJob(ILogger<OrderOutFibrosureJob> logger, ISettingService settingsProvider, IOptions<AppSettingsOrderFibrosureEngine> options, IPayloadSenderApiClient payloadSenderApiClient)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _appSettings = options.Value;
            _payloadSenderApiClient = payloadSenderApiClient;

        }

        public async Task<bool> Execute()
        {
            bool isSuccess = false;

            try
            {
                connectionString = _settingsProvider.GetConnectionString("Bioreference.LIS");

                int labId = _appSettings.LabId;
                string[] calculationsTestName = _appSettings.CalculationsTestName.Split('|');
                ExternalCalcLabQueue objExternalCalcLabQueue;

                var analytes = new List<Bioreference.LIS.ReportAnalyte>();

                foreach (string calcName in calculationsTestName)
                {
                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; LabId: {LabId}; CalcName: {CalcName}",
                        "ExternalCalcQueue",
                        "FetchStart",
                        labId,
                        calcName
                    );

                    // Stopwatch for ExternalCalcLabQueue.Fetch
                    var fetchQueueStopwatch = Stopwatch.StartNew();
                    objExternalCalcLabQueue =
                        await Task.Run(() => ExternalCalcLabQueue.Fetch(labId, calcName));
                    fetchQueueStopwatch.Stop();

                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; CalcName: {CalcName}; ElapsedTime: {ElapsedTime} ms",
                        "ExternalCalcQueue",
                        "FetchComplete",
                        calcName,
                        fetchQueueStopwatch.ElapsedMilliseconds
                    );

                    if (objExternalCalcLabQueue != null && objExternalCalcLabQueue.ReportId > 0)
                    {
                        // Stopwatch for Report.Fetch
                        var fetchReportStopwatch = Stopwatch.StartNew();
                        var rpt = Bioreference.LIS.Report.Fetch(objExternalCalcLabQueue.ReportId);
                        fetchReportStopwatch.Stop();

                        _logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; ReportId: {ReportId}; ElapsedTime: {ElapsedTime} ms",
                            "Report",
                            "Fetch",
                            objExternalCalcLabQueue.ReportId,
                            fetchReportStopwatch.ElapsedMilliseconds
                        );

                        _logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}",
                            "TestMaster",
                            "FetchStart",
                            "Fetching External Lab TestMaster information."
                        );

                        // Stopwatch for ExternalCalcLabTests.Fetch
                        var fetchTestsStopwatch = Stopwatch.StartNew();
                        var tests = await Task.Run(() =>
                            Bioreference.Common.TestMaster.ExternalCalcLabTests.Fetch(labId, calcName)
                        );
                        fetchTestsStopwatch.Stop();

                        _logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; CalcName: {CalcName}; ElapsedTime: {ElapsedTime} ms",
                            "TestMaster",
                            "FetchComplete",
                            calcName,
                            fetchTestsStopwatch.ElapsedMilliseconds
                        );

                        // Stopwatch for OrderManager.FetchOrder
                        var fetchOrderStopwatch = Stopwatch.StartNew();
                        Bioreference.LIS.Order ord =
                            await Task.Run(() => OrderManager.FetchOrder(rpt.OrderId));

                        ActivityHelper.SetAccessionLogKey(ord.AccessionNbr);
                        _logger.LogInformation(
                            "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                            ord.AccessionNbr,
                            DateTime.UtcNow
                        );

                        fetchOrderStopwatch.Stop();

                        _logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; OrderId: {OrderId}; ElapsedTime: {ElapsedTime} ms",
                            "Order",
                            "Fetch",
                            rpt.OrderId,
                            fetchOrderStopwatch.ElapsedMilliseconds
                        );

                        foreach (LIS.ReportAnalyte a in rpt.Analytes.List)
                        {
                            foreach (Bioreference.Common.TestMaster.ExternalCalcLabTest t in tests.List)
                            {
                                if (t.TestCode.Equals(a.Code) && !string.IsNullOrEmpty(a.ResultValue))
                                {
                                    analytes.Add(a);
                                }
                            }
                        }

                        foreach (ReportAnalytePanel analytePanel in rpt.AnalytePanels.List)
                        {
                            foreach (LIS.ReportAnalyte analyte in analytePanel.Analytes.List)
                            {
                                foreach (Bioreference.Common.TestMaster.ExternalCalcLabTest test in tests.List)
                                {
                                    if (test.TestCode.Equals(analyte.Code) &&
                                        !string.IsNullOrEmpty(analyte.ResultValue))
                                    {
                                        analytes.Add(analyte);
                                    }
                                }
                            }
                        }

                        _logger.LogDebug(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; AnalyteCount: {AnalyteCount}",
                            "ExternalCalc",
                            "GenerateMessage",
                            "Generating external calculation message.",
                            analytes.Count
                        );

                        var msg = GenerateMessage(ord, analytes, tests, objExternalCalcLabQueue);

                        await _payloadSenderApiClient.SendPayloadAsync(msg, "FIBROSURELAB");

                        _logger.LogDebug(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; ReportId: {ReportId}",
                            "ExternalCalc",
                            "PayloadSent",
                            "Payload successfully sent to FIBROSURELAB.",
                            objExternalCalcLabQueue.ReportId
                        );
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; CalcName: {CalcName}",
                            "ExternalCalc",
                            "NoData",
                            "Nothing to send for calculation.",
                            calcName
                        );
                    }
                }

                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "ExternalCalc",
                    "Completed",
                    "External calculation processing completed."
                );

                isSuccess = true;
            }
            catch (Exception ex)
            {
                isSuccess = false;

                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "ExternalCalc",
                    "Error",
                    "Error occurred while processing external calculations."
                );

                return false;
            }

            return isSuccess;
        }

        private void AppendMessage(string folder, string fileName, string message)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, fileName), message + Constants.vbCrLf);
        }




        public string GenerateMessage(Bioreference.LIS.Order order, List<Bioreference.LIS.ReportAnalyte> analytes, Bioreference.Common.TestMaster.ExternalCalcLabTests tests, ExternalCalcLabQueue objExternalCalcLabQueue)
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

            // Order ORC
            message.Order = new List<DI.Interface.Order>();

            string controlId = "NW";

            foreach (LIS.ReportAnalyte a in analytes)
            {
                if (a.ResultStatus == resultStatusType.Corrected)
                {
                    controlId = "XO";
                    break;
                }
            }
            string requestData = CreateRequestData(order);
            if (!string.IsNullOrEmpty(objExternalCalcLabQueue.RequestData))
            {
                if (requestData != objExternalCalcLabQueue.RequestData)
                {
                    controlId = "XO";
                }
            }
            objExternalCalcLabQueue.RequestData = requestData;

            var newOrder = new DI.Interface.Order
            {
                OrderControlId = "NW",
                AccessionNumber = order.AccessionNbr,
                // ReferenceLabNumber = refLabId.ToString(), // Uncomment if needed
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

            //Order Rows
            int x = 0;
            int countOBX = 0;

            message.GrpORMOrder = new List<GrpORMOrder>();
            var tx = new Tx
            {
                SetId = (x + 1).ToString(),
                TestCode = _appSettings.OrderingCode,
                CollectDatetime = order.DateOfCollection,
                PlacerOrderNumber = order.AccessionNbr
            };

            var orderGroup = new GrpORMOrder
            {
                Tx = new List<Tx> { tx }
            };

            message.GrpORMOrder.Add(orderGroup);

            // OBX


            foreach (var test in analytes)
            {
                var result = new OBX
                {
                    Set_ID = (countOBX + 1).ToString(),
                    Code = test.Code,
                    Description = test.AnalyteName,
                    ValueType = "NM"
                };

                var orderResultGroup = new GrpORMOrder
                {
                    Result = new List<OBX> { result }
                };

                message.GrpORMOrder.Add(orderResultGroup);

                countOBX++;
            }

            tests.Save();

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

        private string CreateRequestData(LIS.Order order)
        {
            return "DOB=" + order.Patient.DateOfBirth + ";Gender=" + order.Patient.Gender.ToString();
        }
       
    }
}
