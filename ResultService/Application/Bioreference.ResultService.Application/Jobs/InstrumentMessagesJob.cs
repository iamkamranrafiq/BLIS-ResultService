using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.Reflex;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Bioreference.ResultService.Application.Jobs
{
    public class InstrumentMessagesJob : IBLISJob
    {
        private ILogger<InstrumentMessagesJob> logger;
        private readonly IMessageProducer<CreateManifest> producer;
        private readonly ISettingService settingsProvider;
        private string connectionString = string.Empty;

        public InstrumentMessagesJob(ILogger<InstrumentMessagesJob> logger, IProducerProvider provider, ISettingService settingsProvider, IOptions<AppSettingsReflex> options)
        {
            this.logger = logger;
            producer = provider.GetMessageProducer<CreateManifest>();
            this.settingsProvider = settingsProvider;
        }

        /// <summary>
        /// Process Instrument Messages
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Execute()
        {
            bool isSuccess = false;
            try
            {
                connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");

                var fetchWatch = Stopwatch.StartNew();
                InstrumentQueries messages = InstrumentQueries.Fetch();
                fetchWatch.Stop();

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Count: {Count}; ElapsedTime:{FetchElapsedMs} ms",
                    "InstrumentMessages",
                    "Fetch",
                    messages.List.Count,
                    fetchWatch.ElapsedMilliseconds
                );

                foreach (Bioreference.LIS.InstrumentQuery msg in messages.List)
                {
                    ActivityHelper.SetAccessionLogKey(msg.AccessionNbr);
                    logger.LogInformation(
                        "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                        msg.AccessionNbr,
                        DateTime.UtcNow
                    );

                    CreateManifest message = new CreateManifest()
                    {
                        AccessionNumber = msg.AccessionNbr
                    };
                    await Task.Run(() => ProduceMessage(message));

                    var dbWatch = Stopwatch.StartNew();
                    msg.MarkAsSentToETS();
                    msg.Save();
                    dbWatch.Stop();

                    logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; ElapsedTime: {DbElapsedMs} ms",
                        "InstrumentMessages",
                        "Saved",
                        msg.AccessionNbr,
                        dbWatch.ElapsedMilliseconds
                    );
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                            ex,
                            "Entity: {Entity}; Event: {Event}; Message: {Message};",
                            "InstrumentMessages",
                            "Error",
                            "Error processing instrument messages: "+ ex.Message );
            }
            finally
            {
                logger.LogDebug(
                            "Entity: {Entity}; Event: {Event}; Message: {Message};",
                            "InstrumentMessages",
                            "End",
                            "Finished execution of instrument messages job." );
            }
            return isSuccess;
        }

        public async Task ProduceMessage(CreateManifest message)
        {
            if (message == null) return;
            var messageWrapper = new Message<CreateManifest>
            {
                Payload = message,
                Key = StringExtensions.Get9Digit(message.AccessionNumber),
                MessageId = Guid.NewGuid().ToString(),
            };
            messageWrapper.AddHeader("log_key", message.AccessionNumber);
            MessagePartitionInfo result = await producer.ProduceAsync(messageWrapper);
            logger.LogInformation(
                "Entity: {Entity}; Event: {Event}; Message: {Message}; AccessionNumber: {AccessionNumber}; Partition: {Partition}; Offset: {Offset}",
                "InstrumentMessages",
                "ProducedMessage",
                "Produced instrument message to partition.",
                message.AccessionNumber,
                result.Partition,
                result.Offset
            );
        }
    }
}
