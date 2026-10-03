using Bioreference.LIS;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Options;
using Bioreference.ResultService.Application.Model.AppSettings.Genecys;
using Bioreference.Contracts.Result;
using static Bioreference.LIS.OrderReport;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Processor
{

    public class GenecysOrderProcessor : IGenecysOrderProcessor
    {
        private ILogger<GenecysOrderProcessor> logger;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsGenecysOrder appSettings;
        private GenecysOrdersResultsSettings genecysOrderSettings;

        public GenecysOrderProcessor(ILogger<GenecysOrderProcessor> logger, ISettingService settingsProvider, IOptions<AppSettingsGenecysOrder> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }

        public async Task OnGenecysOrderMessage(GenecysOrder msg)
        {
            try
            {
                if (msg == null)
                    return;

                string connection = settingsProvider.GetConnectionString("Bioreference.LIS");

                ActivityHelper.SetAccessionLogKey(msg.AccessionNumber);
                logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    msg.AccessionNumber,
                    DateTime.UtcNow
                );

                // FetchSetting call is ignored for stopwatch as requested
                genecysOrderSettings =
                    await settingsProvider.FetchSetting<GenecysOrdersResultsSettings>(
                        connection,
                        "B2GenecysOrdersResults",
                        ""
                    );

                int externalAppId = 5;

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}",
                    "GenecysOrder",
                    "Received",
                    msg.AccessionNumber
                );

                string dos = $"{msg.DateServiced:MM/dd/yyyy}";

                // Start stopwatch for CreateOrder
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                OrderManager.Status orderStatus =
                    OrderManager.CreateOrder(
                        "J9999", //msg.AccountNumber, Business required a fixed value J9999 here
                        msg.AccessionNumber,
                        dos,
                        0,
                        true
                    );
                stopwatch.Stop();

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; ElapsedTime: {ElapsedTime} ms",
                    "GenecysOrder",
                    "CreateOrder",
                    msg.AccessionNumber,
                    stopwatch.ElapsedMilliseconds
                );

                if (orderStatus.StatusType == OrderManager.StatusType.Failure)
                {
                    throw new Exception(
                        $"Error creating order for {msg.AccessionNumber}: {orderStatus.Message}"
                    );
                }

                var tests = new List<OrderReport.TestCodeStruct>();

                Bioreference.LIS.Order order =
                    (Bioreference.LIS.Order)orderStatus.ReturnValue;

                order.ExternalApplicationId = externalAppId;
                order.RelaxValidation();

                if (!string.IsNullOrWhiteSpace(msg.PatientName))
                {
                    string[] tokens =
                        msg.PatientName.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    order.Patient.LastName = tokens[0].Trim();

                    if (tokens.Length > 1)
                        order.Patient.FirstName = tokens[1].Trim();
                }

                order.Patient.DateOfBirth = $"{msg.DateOfBirth:MM/dd/yyyy}";

                char gender = msg.PatientSex.ToUpper()[0];
                order.Patient.Gender =
                    gender == 'F' ? Bioreference.Common.Gender.Female :
                    gender == 'M' ? Bioreference.Common.Gender.Male :
                    Bioreference.Common.Gender.Unknown;

                order.AccessionNbr = msg.AccessionNumber;
                order.DateOfCollection = msg.CollectionDate;
                order.DateOfService = msg.DateServiced;

                foreach (GenecysOrderTest testCode in msg.TestCodes)
                {
                    tests.Add(new OrderReport.TestCodeStruct
                    {
                        TestCode = testCode.B2TestCode,
                        OrderedTestCode = testCode.B2TestCode,
                        AccessioningFacility = msg.AccessionFacilityId
                    });
                }

                logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; TestCount: {TestCount}",
                    "GenecysOrder",
                    "Tests",
                    order.AccessionNbr,
                    tests.Count
                );

                if (order.IsValid)
                {
                    Bioreference.LIS.OrderReport report =
                        new OrderReport(order, tests.ToArray());

                    report = (OrderReport)report.Save();

                    if (report.Status.StatusType == OrderManager.StatusType.Warning)
                    {
                        logger.LogWarning(
                            "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                            "GenecysOrder",
                            "OrderWarning",
                            order.AccessionNbr,
                            report.Status.Message
                        );
                    }
                    else if (report.Status.StatusType == OrderManager.StatusType.Failure)
                    {
                        throw new Exception(
                            $"{order.AccessionNbr}: {report.Status.Message}"
                        );
                    }
                }
                else
                {
                    throw new Exception(
                        $"{order.AccessionNbr}: {order.GetCompleteRules()}"
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                    "GenecysOrder",
                    "Error",
                    msg?.AccessionNumber,
                    "Error while processing Genecys order message."
                );

                throw;
            }
        }
    }
}
