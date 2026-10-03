using Bioreference.Contracts.Order;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Processors;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.AppSettings.QueueOrder;
using Bioreference.ResultService.Common;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.SPM.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using static Bioreference.LIS.OrderSnapshot;

namespace Bioreference.ResultService.Application.Processor
{
    public class QueueOrderInProcessor : IQueueOrderInProcessor
    {
        private ILogger<QueueOrderInProcessor> _logger;
        private readonly ISettingService _settingsProvider;
        private AppSettingsQueueOrderInProcessor _appSettings = null;
        public QueueOrderInEngineLISSettings _queueOrderInEngineLisSettings = null;
        private string connectionString = string.Empty;

        public QueueOrderInProcessor(ILogger<QueueOrderInProcessor> logger, ISettingService settingsProvider, IOptions<AppSettingsQueueOrderInProcessor> options)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _appSettings = options.Value;
        }
        public async Task<bool> QueueOrderInStatus(long orderHistoryId, int status)
        {
            try
            {
                var processOrderControl = Bioreference.LIS.Configuration.LISSettings.GetBool("EnableProcessOrderConsumer");
                if (!processOrderControl)
                {
                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; OrderHistoryId: {OrderHistoryId}",
                        "ProcessOrder",
                        "QueueOrderInFetch",
                        "ProcessOrderConsumer is disabled or missing config.",
                        orderHistoryId);

                    return false;
                }

                var queueOrderIn =
                    await Task.Run(() =>
                        Bioreference.LIS.QueueOrderIn.FetchByOrderHistoryId(orderHistoryId, status));

                if (queueOrderIn != null && (queueOrderIn.QueueItems.Count() > 0 || queueOrderIn.QueueItemDetails.Count() > 0))
                {
                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; OrderHistoryId: {OrderHistoryId}",
                        "ProcessOrder",
                        "QueueOrderInFetch",
                        "QueueOrderIn record found.",
                        orderHistoryId);

                    return true;
                }

                _logger.LogWarning(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; OrderHistoryId: {OrderHistoryId}",
                    "ProcessOrder",
                    "QueueOrderInFetch",
                    "No QueueOrderIn record found.",
                    orderHistoryId);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in QueueOrderInStatus. OrderHistoryId: {OrderHistoryId}",
                    orderHistoryId);

                return false;
            }
        }
        public async Task<bool> ProcessMessage(Contracts.Order.Order order, ProcessOrderConfiguration processOrderConfig)
        {
            try
            {
                var orderStopWatch = Stopwatch.StartNew();
                // Request-scoped override so domain uses API-provided value instead of static config
                if (processOrderConfig != null)
                {
                    ConfigurationOverrides.SetOverride(
                        ConfigurationOverrideValue.ToFollowEnabledKey,
                        processOrderConfig.ToFollowEnabled);
                    ConfigurationOverrides.SetOverride(
                        ConfigurationOverrideValue.LoggingNameKey,
                        processOrderConfig.LoggingName);
                }
                ActivityHelper.SetAccessionLogKey(order.AccessionNumber);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    order.AccessionNumber,
                    DateTime.UtcNow
                );

                string connection = _settingsProvider.GetConnectionString("Bioreference.LIS");
                _queueOrderInEngineLisSettings = await _settingsProvider.FetchSetting<QueueOrderInEngineLISSettings>(connection, "BioreferenceLIS", "");

                orderStopWatch.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedMs}", "Settings", "Fetch", "Order settings fetched.", order.AccessionNumber, orderStopWatch.ElapsedMilliseconds);

                if (order.CallingApplication == CallingApplication.NEXTGATE.ToString())
                {
                    return await ProcessNextGate(order);
                }
                else
                {
                    return await ProcessQueueItem(order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Entity: {0}; Event: {1}; Message: {2}; ErrorMessage: {3}",
                    "Order", "Exception", "Exception occurred while processing Order Message.", ex.Message));
                return false;
            }
            finally
            {
                ConfigurationOverrides.Clear();
            }
        }

        private async Task<bool> ProcessNextGate(Contracts.Order.Order order)
        {
            bool status = false;
            try
            {
              
                int result = await Task.Run(() =>Bioreference.LIS.Order.UpdateEUID(order.AccessionNumber, DateTime.ParseExact(order.DateOfService, "MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture), order.Patient.EUID));

                switch (result)
                {
                    case -2:
                        {
                            _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Order", "Update", "EUID Update: Unable to find Order.", order.AccessionNumber);
                            break;
                        }
                    case -1:
                        {
                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Order", "Update", "EUID Update: No change.", order.AccessionNumber);
                            status = true;
                            break;
                        }
                    case 0:
                        {
                            _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Order", "Update", "EUID Update: Failed.", order.AccessionNumber);                           
                            break;
                        }
                    case 1:
                        {
                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Order", "Update", "EUID Update: Success.", order.AccessionNumber);
                            status = true;
                            break;
                        }
                    default:
                        {
                            _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Result: {Result}", "Order", "Update", "EUID Update: Unknown status.", order.AccessionNumber, result);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ErrorMessage: {ErrorMessage}", "Order", "Exception", "Exception during EUID update.", order.AccessionNumber, ex.Message);
                return false;
            }

            return status;
        }

        public OrderSnapshot CreateOrderSnapshot(Contracts.Order.Order order)
        {
            List<Bioreference.LIS.OrderSnapshot.Specimen> specimenList = new List<Bioreference.LIS.OrderSnapshot.Specimen>();
            List<SpecimenCode> testSpecimens = new List<SpecimenCode>();
            List<ParentTestCode> parentTests = new List<ParentTestCode>();

            if (order.Specimens != null)
            {
                foreach (var spec in order.Specimens)
                {
                    Bioreference.LIS.OrderSnapshot.Specimen specimen = new OrderSnapshot.Specimen();
                    specimen.Quantity = Convert.ToInt32(spec.Quantity);
                    specimen.SpecimenCode = spec.SpecimenCode;
                    specimenList.Add(specimen);
                }
            }

            if (order.Tests != null)
            {
                foreach (var test in order.Tests)
                {
                    if (test.PrimarySpecimenCodes != null)
                    {
                        foreach (var specimenCode in test.PrimarySpecimenCodes)
                            testSpecimens.Add(new SpecimenCode() { TestCode = test.TestCode, Specimen = (string)specimenCode });
                    }

                    if (test.AlternateSpecimenCodes != null)
                    {
                        foreach (var specimenCode in test.AlternateSpecimenCodes)
                            testSpecimens.Add(new SpecimenCode() { TestCode = test.TestCode, Specimen = (string)specimenCode });
                    }

                    if (test.ParentTestCode != "")
                    {
                        parentTests.Add(new ParentTestCode() { TestCode = test.TestCode, ParentTestCodeField = test.ParentTestCode });
                    }
                }
            }

            return new OrderSnapshot(specimenList, testSpecimens, parentTests);
        }

        private async Task<bool> ProcessQueueItem(Contracts.Order.Order queueItem)
        {
            bool queueError = false;
            Bioreference.LIS.Order o = default;
            try
            {
                if (queueItem.AccessionNumber.Length == 9 && queueItem.AccessionNumber.StartsWith("10"))
                {
                    queueItem.AccessionNumber = queueItem.AccessionNumber.Substring(2);
                }

                long _messageLogId = 0L;
                var addTests = new List<OrderReport.TestCodeStruct>();
                var deleteTests = new List<OrderReport.TestCodeStruct>();
                var updateTests = new List<OrderReport.TestCodeStruct>();
                var changeTests = new List<OrderReport.TestCodeStruct>();
                var comments = new List<string>();
                HoldType holdType;
                bool isOnHold;
                bool isTNPOnHold;
                var deletedComments = new List<int>();
                List<string> AOEExclusionList = _appSettings.AOEExclusionList;


                var testMasterStopWatch = Stopwatch.StartNew();
                Bioreference.Common.TestMaster.NonResult[] nonResultsList = Bioreference.Common.TestMaster.NonResults.Fetch().List;
                testMasterStopWatch.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Count{Count}; ElapsedTime: {ElapsedMs}", "TestMaster", "Fetch", "NonResults fetch duration.", queueItem.AccessionNumber, nonResultsList.Length, testMasterStopWatch.ElapsedMilliseconds);

                var snapShot = CreateOrderSnapshot(queueItem);

                var orderStopWatch = Stopwatch.StartNew();
                LIS.OrderManager.Status st = LIS.OrderManager.CreateOrder(queueItem.AccountNumber, queueItem.AccessionNumber, DateTime.ParseExact(queueItem.DateOfService, "MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture).ToShortDateString(), queueItem.OrderId, true);

                orderStopWatch.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedMs}", "Order", "Create", "Order creation duration.", queueItem.AccessionNumber, orderStopWatch.ElapsedMilliseconds);

                o = (Bioreference.LIS.Order)st.ReturnValue;

                if (st.StatusType == LIS.OrderManager.StatusType.Failure)
                {
                    queueError = true;
                    _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Details: {Details}", "Order", "Error", "Failed to create order from queue.", st.Message);
                    return false;
                }

                foreach (var q in queueItem.Tests)
                {
                    string result = "";
                    holdType = GetHoldType(q.TestResultHold);
                    isOnHold = q.IsPerformOnHold;
                    isTNPOnHold = IsTNPHold(q, nonResultsList);

                    if (q.MetaData != null)
                    {
                        var meta = q.MetaData.Find(x => x.Name == "POCTResult");
                        if (meta != null)
                        {
                            result = meta.Value;
                        }
                    }

                    if (isTNPOnHold)
                    {
                        if (q.HoldCode.Contains(_appSettings.AltHoldCodes))
                        {
                            result = "Alt. Test Performed";
                        }
                        else
                        {
                            result = "TNP";
                        }
                    }

                    q.TestCode = q.TestCode.ToUpper();
                    q.OrderedTestCode = q.OrderedTestCode.ToUpper();

                    if (q.ActionType == TestActionType.Add)
                    {
                        _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "Tests", "Add", "New test added.", queueItem.AccessionNumber, q.TestCode);

                        if (Convert.ToInt32(q.CodeTypeId) == (int)SPM.Core.Code_Type.Profile)
                        {
                            if (q.OrderTestAnswers != null && q.OrderTestAnswers.Count > 0)
                            {
                                foreach (var answer in q.OrderTestAnswers)
                                {
                                    if (IsComponentLevel(q.TestCode, answer.QuestionCode))
                                    {
                                        _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}; AOE: {AOE}", "Tests", "Add", "Unable to find AOE for the Test Code in the Test Master", queueItem.AccessionNumber, q.TestCode, answer.QuestionCode);
                                        continue;
                                    }
                                    addTests.Add(new OrderReport.TestCodeStruct()
                                    {
                                        TestCode = answer.QuestionCode,
                                        OrderedTestCode = answer.QuestionCode,
                                        ResultValue = answer.AnswerText,
                                        OrderedTestName = q.OrderedTestName,
                                        PerformingFacility = q.PerformingFacility,
                                        AccessioningFacility = q.AccessioningFacility,
                                        IsOnHold = isOnHold,
                                        SPMOrderTestId = Convert.ToInt64(answer.OrderTestId),
                                        HoldType = holdType,
                                        IsPresumptiveHold = q.IsPresumptiveTest,
                                        SPMStatus = ToSPMStatus(answer.AnswerText)
                                    });
                                }
                            }


                        }
                        else
                        {
                            addTests.Add(new OrderReport.TestCodeStruct()
                            {
                                TestCode = q.TestCode,
                                OrderedTestCode = q.OrderedTestCode,
                                ResultValue = result,
                                OrderedTestName = q.OrderedTestName,
                                PerformingFacility = q.PerformingFacility,
                                AccessioningFacility = q.AccessioningFacility,
                                IsOnHold = isOnHold,
                                Cmnt = q.Comments,
                                SPMOrderTestId = Convert.ToInt64(q.OrderTestId),
                                HoldType = holdType,
                                IsPresumptiveHold = q.IsPresumptiveTest,
                                SPMStatus = ToSPMStatus(result)
                            });

                            if (q.OrderTestAnswers != null && q.OrderTestAnswers.Count > 0)
                            {
                                foreach (var answer in q.OrderTestAnswers)
                                {
                                    if (AOEExclusionList.Contains(answer.QuestionCode))
                                    {
                                        continue;
                                    }

                                    if (IsAOESystemGenerated(answer.TestCode, queueItem.AccessionNumber))
                                    {
                                        continue;
                                    }

                                    addTests.Add(new OrderReport.TestCodeStruct()
                                    {
                                        TestCode = answer.QuestionCode,
                                        OrderedTestCode = answer.QuestionCode,
                                        ResultValue = answer.AnswerText,
                                        OrderedTestName = q.OrderedTestName,
                                        PerformingFacility = q.PerformingFacility,
                                        AccessioningFacility = q.AccessioningFacility,
                                        IsOnHold = isOnHold,
                                        SPMOrderTestId = Convert.ToInt64(answer.OrderTestId),
                                        HoldType = holdType,
                                        IsPresumptiveHold = q.IsPresumptiveTest,
                                        SPMStatus = ToSPMStatus(answer.AnswerText)
                                    });
                                }
                            }
                        }
                    }
                    else if (q.ActionType == TestActionType.Delete)
                    {
                        _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}", "Tests", "Delete", "Test deleted.", queueItem.AccessionNumber, q.TestCode);

                        deleteTests.Add(new OrderReport.TestCodeStruct()
                        {
                            TestCode = q.TestCode,
                            OrderedTestCode = q.OrderedTestCode
                        });
                    }
                    else if (q.ActionType == TestActionType.Update)
                    {
                        LIS.ReportAnalyte? clonnedTest = o?.Report?.Analytes?.FindFirst(q.TestCode);

                        if (IsPresumptiveStateChanged(q, clonnedTest))
                        {

                            changeTests.Add(new OrderReport.TestCodeStruct()
                            {
                                TestCode = q.TestCode,
                                OrderedTestCode = q.OrderedTestCode,
                                ResultValue = result,
                                OrderedTestName = q.OrderedTestName,
                                PerformingFacility = q.PerformingFacility,
                                AccessioningFacility = q.AccessioningFacility,
                                IsOnHold = isOnHold,
                                Cmnt = q.Comments,
                                SPMOrderTestId = Convert.ToInt64(q.OrderTestId),
                                HoldType = holdType,
                                IsPresumptiveHold = q.IsPresumptiveTest,
                                SPMStatus = ToSPMStatus(result)
                            });
                        }
                        else
                        {
                            updateTests.Add(new OrderReport.TestCodeStruct()
                            {
                                TestCode = q.TestCode,
                                OrderedTestCode = q.OrderedTestCode,
                                ResultValue = result,
                                OrderedTestName = q.OrderedTestName,
                                PerformingFacility = q.PerformingFacility,
                                AccessioningFacility = q.AccessioningFacility,
                                IsOnHold = isOnHold,
                                Cmnt = q.Comments,
                                SPMOrderTestId = Convert.ToInt64(q.OrderTestId),
                                HoldType = holdType,
                                IsPresumptiveHold = q.IsPresumptiveTest,
                                SPMStatus = ToSPMStatus(result)
                            });

                            if (q.OrderTestAnswers != null && q.OrderTestAnswers.Count > 0)
                            {
                                foreach (var answer in q.OrderTestAnswers)
                                {
                                    LIS.ReportAnalyte? aoeAnalyte = o?.Report?.Analytes?.FindFirst(answer.QuestionCode);

                                    if (aoeAnalyte == null || aoeAnalyte.ResultValue != answer.AnswerText)
                                    {
                                        updateTests.Add(new OrderReport.TestCodeStruct()
                                        {
                                            TestCode = answer.QuestionCode,
                                            OrderedTestCode = answer.QuestionCode,
                                            ResultValue = answer.AnswerText,
                                            OrderedTestName = q.OrderedTestName,
                                            PerformingFacility = q.PerformingFacility,
                                            AccessioningFacility = q.AccessioningFacility,
                                            IsOnHold = isOnHold,
                                            SPMOrderTestId = Convert.ToInt64(answer.OrderTestId),
                                            HoldType = holdType,
                                            IsPresumptiveHold = q.IsPresumptiveTest,
                                            SPMStatus = ToSPMStatus(answer.AnswerText)
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var q in queueItem.ProcessedComments)
                {
                    if (q.CommentType == CommentTypes.Comment2)
                    {
                        if (q.ActionType == ActionType.Add)
                        {
                            comments.Add(q.Comment);
                        }
                        else if (q.ActionType == ActionType.Delete)
                        {
                            try
                            {
                                int commentId = int.Parse(q.Comment);
                                if (!deletedComments.Contains(commentId))
                                {
                                    deletedComments.Add(commentId);
                                }
                            }
                            catch
                            {
                                _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Comment: {Comment}", "OrderIn", "DeleteComment", "Unable to delete comment. Comment ID is not a valid numeric.", q.Comment);
                            }
                        }
                    }
                }

                if (o is not null)
                {
                    _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; DOS: {DOS}; AddCount: {AddCount}; UpdateCount: {UpdateCount}; DeleteCount: {DeleteCount}", "Order", "OrderSummary", "Order summary.", o.AccessionNbr, o.DateOfService, addTests.Count, updateTests.Count, deleteTests.Count);

                    o.DivisionId = queueItem.DivisionId;
                    o.RelaxValidation();
                    if (o.AccountNumber != queueItem.AccountNumber)
                    {
                        o.SetAccountNumber(queueItem.AccountNumber);
                    }

                    o.EnterersLocationType = queueItem.EnterersLocationType;
                    o.EnterersLocation = queueItem.EnterersLocation;
                    o.EnterersLastName = queueItem.EnterersLastName;
                    o.EnterersFirstName = queueItem.EnterersFirstName;

                    if (string.IsNullOrEmpty(queueItem.Patient.DateOfBirth))
                    {
                        string[] age = queueItem.Patient.Age.Split(' ');
                        if (age.Length == 2)
                        {
                            o.Patient.AgeNumber = int.TryParse(age[0], out var parsedAge) ? parsedAge : 0;
                            o.Patient.AgeType = age[1];
                        }
                    }


                    if (queueItem.Priority.HasValue)
                    {
                        switch ((SPMOrderPriority)queueItem.Priority.Value)
                        {
                            case SPMOrderPriority.Routine:
                                o.Priority = Bioreference.LIS.OrderPriority.Routine;
                                break;

                            case SPMOrderPriority.STAT:
                                o.Priority = Bioreference.LIS.OrderPriority.STAT;
                                break;

                            case SPMOrderPriority.Rush:
                                o.Priority = Bioreference.LIS.OrderPriority.Rush;
                                break;
                        }
                    }


                    if (o.IsReportHold && !queueItem.ReportHold)
                    {
                        o.IsReportHoldDirty = true;
                    }

                    o.IsReportHold = queueItem.ReportHold;
                    o.SPMOrderId = Convert.ToInt32(queueItem.OrderId);
                    o.SPMParentOrderId = Convert.ToInt32(queueItem.ParentOrderId);
                    o.MarketType = (Bioreference.Common.Client.MarketType)Convert.ToInt32(queueItem.AccountInfo.MarketType);
                    o.ClientType = queueItem.AccountPriority.Equals("BB") ? clientType.BlueBag : clientType.None;
                    o.AccountPriority = queueItem.AccountPriority;
                    if (queueItem.Patient != null)
                    {
                        if (queueItem.Patient.EUID > 0)
                        {
                            o.EUID = queueItem.Patient.EUID;
                        }
                        o.Patient.Gender = (Bioreference.Common.Gender)queueItem.Patient.Gender;
                        o.Patient.LastName = queueItem.Patient.LastName;
                        o.Patient.FirstName = queueItem.Patient.FirstName;
                        o.Patient.PrimaryAddress.StreetLine1 = queueItem.Patient.StreetLine1;
                        o.Patient.PrimaryAddress.StreetLine2 = queueItem.Patient.StreetLine2;
                        o.Patient.PrimaryAddress.City = queueItem.Patient.City;
                        o.Patient.PrimaryAddress.State = queueItem.Patient.State;
                        o.Patient.PrimaryAddress.ZipCode = queueItem.Patient.ZipCode;
                        o.Patient.PrimaryAddress.Country = queueItem.Patient.Country;
                        o.Patient.HomePhoneNumber = queueItem.Patient.HomePhoneNumber;

                        if (string.IsNullOrEmpty(queueItem.Patient.DateOfBirth))
                        {
                            o.Patient.ResetDOB();
                        }
                        else
                        {
                            o.Patient.DateOfBirth = queueItem.Patient.DateOfBirth;
                        }
                    }
                    o.PrimaryPhysician.FirstName = queueItem.PrimaryPhysician.FirstName;
                    o.PrimaryPhysician.LastName = queueItem.PrimaryPhysician.LastName;

                    if (o.DateOfService <= DateTime.Parse("1900-01-01"))
                    {
                        o.DateOfService = DateTime.ParseExact(queueItem.DateOfService, "MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                    }

                    o.DateOfCollection = string.IsNullOrWhiteSpace(queueItem.TimeOfCollection)
                                        ? DateTime.ParseExact(queueItem.DateOfCollection, "MM/dd/yyyy", CultureInfo.InvariantCulture)
                                        : DateTime.ParseExact($"{queueItem.DateOfCollection} {queueItem.TimeOfCollection}", "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture);
                    o.TimeOfCollection = queueItem.TimeOfCollection;
                    o.Patient.UpdateTrackingId = queueItem.DemoUpdateTrackingId;
                    o.VisitNumber = queueItem.VisitNumber;
                    o.StudyNumber = queueItem.StudyNumber;
                    o.SpecimenUpdated = false;

                    string metaDataJson = string.Empty;
                    if (queueItem.MetaData is not null)
                    {
                        var eTSMetaData = queueItem.MetaData.Find(m => m.Name == "IsRestrictedAccession");
                        if (eTSMetaData is not null)
                        {
                            var metaData = new OrderMetaData(eTSMetaData.Value, eTSMetaData.Name);
                            var mdata = new List<OrderMetaData>();
                            mdata.Add(metaData);
                            metaDataJson = JsonConvert.SerializeObject(mdata, Formatting.Indented);
                            o.MetaData = metaDataJson;
                        }
                        else
                        {
                            o.MetaData = "";
                        }
                    }


                    foreach (int i in deletedComments)
                        o.OrderComments.DeleteComment(i);
                    foreach (string s in comments)
                        o.OrderComments.AddComment(s, "", externalApplicationType.SPM);

                    var commentStopWatch = Stopwatch.StartNew();
                    Bioreference.LIS.Report r = default;
                    if (deleteTests.Count > 0)
                    {
                        if (r == null)
                        {
                            r = Bioreference.LIS.Report.Fetch(o.AccessionNbr);
                        }

                        foreach (OrderReport.TestCodeStruct t in deleteTests)
                        {
                            if (r.RemoveByOrderableCode(t.TestCode, t.OrderedTestCode))
                            {
                                _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; TestCode: {TestCode}; OrderedTestCode: {OrderedTestCode}", "Report", "Delete Test", "Delete analyte by test code.", t.TestCode, t.OrderedTestCode);
                            }
                        }

                        await Task.Run(() => r.Save());
                    }

                    commentStopWatch.Stop();

                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedMs}", "Report", "Delete", "Delete tests.", queueItem.AccessionNumber, commentStopWatch.ElapsedMilliseconds);

                    if (addTests.Count > 0 || updateTests.Count > 0 || changeTests.Count > 0)
                    {
                        var timer = new Stopwatch();
                        timer.Start();
                        var oReport = new OrderReport(o, addTests, updateTests, changeTests);
                        timer.Stop();
                        long elapsedTime = timer.ElapsedMilliseconds;
                        _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedMs}", "Report", "Create", "Report creation duration.", queueItem.AccessionNumber, elapsedTime);

                        timer.Reset();
                        timer.Start();
                        oReport.Snapshot = snapShot;
                        await Task.Run(() => oReport.Save());
                        timer.Stop();
                        elapsedTime = timer.ElapsedMilliseconds;
                        _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedMs}", "Report", "Save", "Report save duration.", queueItem.AccessionNumber, elapsedTime);

                        if (oReport.Status.StatusType == LIS.OrderManager.StatusType.Failure)
                        {
                            _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Details: {Details}", "Report", "Error", "Report creation failed.", oReport.Status.Message);
                            queueError = true;
                        }
                        else if (oReport.Status.StatusType == LIS.OrderManager.StatusType.Warning)
                        {
                            _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Details: {Details}", "Report", "Warning", "Report creation warning.", oReport.Status.Message);
                        }
                        else
                        {
                            _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Details: {Details}", "Report", "Add", "Order/Report successfully created.", oReport.Status.Message);
                        }
                    }
                    else
                    {
                        
                        o.ReReleaseResults = LIS.Configuration.AppSettings.GetBool("Bioreference.LIS:ReleaseOnDemographicUpdate");                        
                        var saveStopWatch = Stopwatch.StartNew();
                        o.Save();
                        saveStopWatch.Stop();
                        _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedMs: {ElapsedMs}", "Order", "Save", "Save order duration.", queueItem.AccessionNumber, saveStopWatch.ElapsedMilliseconds);
                    }

                }
                else
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Order", "Error", "Fetched order is null.");
                }

                return true;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ErrorMessage: {ErrorMessage}", "Order", "Exception", "Exception while processing queue item.", ex.Message);
                if (o is not null)
                {
                    o.CleanUpOrder();
                }

                return false;
            }

        }

        public bool IsComponentLevel(string testCode, string aoeCode)
        {
            var orderableTestsList = Bioreference.Common.TestMaster.OrderableTest.Fetch(testCode).AOEs;
            bool retIsComponentLevel = true;

            foreach (Bioreference.Common.TestMaster.AOE _aoe in orderableTestsList)
            {
                if (_aoe.AOECode == aoeCode)
                {
                    retIsComponentLevel = false;
                    break;
                }
            }

            return retIsComponentLevel;
        }

        private HoldType GetHoldType(bool isTestHold)
        {
            if (isTestHold)
            {
                return HoldType.TestHold;
            }
            else
            {
                return HoldType.None;
            }
        }

        private SPMStatusValue ToSPMStatus(string result)
        {
            if (_queueOrderInEngineLisSettings.SPMTNPTriggers.Contains(result, new CompareText()))
            {
                return SPMStatusValue.TNP;
            }
            if (_queueOrderInEngineLisSettings.SPMTNPTriggers.Contains(result, new CompareText()))
            {
                return SPMStatusValue.ATP;
            }
            return SPMStatusValue.None;
        }

        private bool IsAOESystemGenerated(string code, string accesionNumber)
        {
            var aoeStopWatch = Stopwatch.StartNew();

            var testAnswers = Bioreference.Common.TestMaster.AOE.Fetch(code).AOEAnswers;

            if (testAnswers != null)
            {
                foreach (var ans in testAnswers)
                {
                    if (ans.AOEAnswerText.ToUpper() == "[SYSTEM_GENERATED]")
                    {
                        return true;
                    }
                }
            }

            aoeStopWatch.Stop();
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedTime: {ElapsedMs}", "AOETestMaster", "Fetch", "IsAOESystemGenerated execution duration.", accesionNumber, aoeStopWatch.ElapsedMilliseconds);

            return false;
        }


        private bool IsTNPHold(Test ot, Bioreference.Common.TestMaster.NonResult[] nonResultsList)
        {
            if (ot == null) return false;
            if (!isOnHold(ot)) return false;

            var match = nonResultsList
                .FirstOrDefault(nr =>
                   (int)nr.ApplicationID == 1 &&
                    nr.ValueType == "T" &&
                    string.Equals(nr.Condition, ot.HoldCode, StringComparison.InvariantCultureIgnoreCase));

            return match != null;
        }

        private bool isOnHold(Test ot)
        {
            return !string.IsNullOrEmpty(ot.HoldCode) && !ot.IsHoldOverriden;
        }

        private bool IsPresumptiveStateChanged(Test orderTest, LIS.ReportAnalyte clonnedTest)
        {
            if (clonnedTest != null && orderTest != null && orderTest.IsPresumptiveTest != clonnedTest.IsPresumptiveHold)
            {
                return true;
            }
            return false;
        }



    }

}
