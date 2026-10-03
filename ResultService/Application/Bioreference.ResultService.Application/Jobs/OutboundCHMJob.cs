using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.OutboundCHM;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Bioreference.ResultService.Application.Jobs
{
    public class OutboundCHMJob : IBLISJob
    {
        private ILogger<OutboundCHMJob> logger;
        private readonly AppSettingsOutboundCHMEngine _appSettings;
        private readonly IMessageProducer<Outbound_CHM> producer;
        private static int _outboundStatusMessagePathIndex = 0;
        private static int m_inboundStatusMessagePathIndex = 0;
        private static int _fetchcount = 0;
        private string connectionString = string.Empty;
        private readonly ISettingService settingsProvider;

        public OutboundCHMJob(ILogger<OutboundCHMJob> _logger, IOptions<AppSettingsOutboundCHMEngine> options, ISettingService settingsProvider, IProducerProvider provider = null)
        {
            logger = _logger;
            _appSettings = options.Value;
            producer = provider.GetMessageProducer<Outbound_CHM>();
            this.settingsProvider = settingsProvider;          
        }

        public async Task<bool> Execute()
        {
            bool isSuccess = false;
            string sMessage = "";

            logger.LogDebug(
                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                "OutboundCHM",
                "Start",
                "OutboundCHM execution started."
            );

            connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");

            try
            {
                // Start stopwatch for database fetch
                var dbStopwatch = Stopwatch.StartNew();
                OutboundMessageCHM obCHM = OutboundMessageCHM.Fetch(_fetchcount);
                dbStopwatch.Stop();

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Count: {Count}; ElapsedTime: {ElapsedTime} ms",
                    "OutboundCHM",
                    "Fetch",
                    obCHM?.List.Count ?? 0,
                    dbStopwatch.ElapsedMilliseconds
                );

                if (obCHM != null)
                {
                    try
                    {
                        if (obCHM.List.Count > 0)
                        {
                            logger.LogInformation(
                                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                                "OutboundCHM",
                                "MessagesAvailable",
                                "Outbound CHM messages available for processing."
                            );
                        }

                        int messageIndex = 0;

                        foreach (OutboundMessagesCHM m in obCHM.List)
                        {
                            ActivityHelper.SetAccessionLogKey(m.AccessionNumber);
                            logger.LogInformation(
                                "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                                m.AccessionNumber,
                                DateTime.UtcNow
                            );
                            messageIndex++;

                            sMessage = m.Message;

                            logger.LogInformation(
                                "Entity: {Entity}; Event: {Event}; Index: {Index}; Total: {Total}; Processor: {Processor}; AccessionNumber: {AccessionNumber}",
                                "OutboundCHM",
                                "Processing",
                                messageIndex,
                                obCHM.List.Count,
                                "OutboundCHMJob",
                                m.AccessionNumber
                            );

                            await Task.Run(() =>
                                ProduceOutboundCHMMessage(sMessage, m.AccessionNumber));
                        }
                    }
                    catch (Exception ex)
                    {
                        ResultService.Common.Utilities.SaveMessageToArchives(sMessage, "OutboundCHM", false, false);
                        logger.LogError(
                            ex,
                            "Entity: {Entity}; Event: {Event}; Message: {Message}",
                            "OutboundCHM",
                            "ProcessingError",
                            "Error while processing OutboundMessageCHM list."
                        );

                        isSuccess = false;
                    }
                }
                else
                {
                    logger.LogDebug(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "OutboundCHM",
                        "NoData",
                        "No OutboundMessageCHM found (result was null)."
                    );
                }

                isSuccess = true;

                logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "OutboundCHM",
                    "Completed",
                    "OutboundCHM execution finished successfully."
                );
            }
            catch (Exception ex)
            {
                ResultService.Common.Utilities.SaveMessageToArchives(sMessage, "OutboundCHM", false, false);
                logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "OutboundCHM",
                    "UnhandledException",
                    "Unhandled exception in OutboundCHM Execute."
                );
            }

            return isSuccess;
        }
        public async Task ProduceOutboundCHMMessage(string message, string accessionNumber)
        {
            if (message == null) return;

            Outbound_CHM outboundCHM = new Outbound_CHM();
            outboundCHM.Message = message;

            var messageWrapper = new Message<Outbound_CHM>
            {
                Payload = outboundCHM,
                Key = StringExtensions.Get9Digit(accessionNumber),
                MessageId = Guid.NewGuid().ToString(),
            };

            messageWrapper.AddHeader("log_key", accessionNumber);
            MessagePartitionInfo result = await producer.ProduceAsync(messageWrapper);
        }
    }
}
