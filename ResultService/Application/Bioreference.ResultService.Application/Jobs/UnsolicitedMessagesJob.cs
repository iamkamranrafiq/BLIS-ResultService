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
    public class UnsolicitedMessagesJob : IBLISJob
    {
        private ILogger<UnsolicitedMessagesJob> logger;
        private readonly IMessageProducer<CreateManifest> producer;
        private readonly ISettingService settingsProvider;
        private string connectionString = string.Empty;

        public UnsolicitedMessagesJob(ILogger<UnsolicitedMessagesJob> logger, IProducerProvider provider, ISettingService settingsProvider, IOptions<AppSettingsReflex> options)
        {
            this.logger = logger;
            this.producer = provider.GetMessageProducer<CreateManifest>();
            this.settingsProvider = settingsProvider;
        }

        public async Task<bool> Execute()
        {
            bool isSuccess = false;
            var sw = Stopwatch.StartNew();
            try
            {
                connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");
                UnsolicitedMessage.FetchUnSent();
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}", "UnsolicitedMessages", "Fetch", "Found unsolicited messages to process.", UnsolicitedMessage.List.Count);
                foreach (UnsolicitedMessage msg in UnsolicitedMessage.List)
                {
                    ActivityHelper.SetAccessionLogKey(msg.AccessionNbr);
                    logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    msg.AccessionNbr,
                    DateTime.UtcNow);
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "UnsolicitedMessages", "ProcessMessage", "Processing unsolicited message.", msg.AccessionNbr);
                    if (msg.ProcessedDate > DateTime.MinValue) continue;
                    CreateManifest message = new CreateManifest() { AccessionNumber = msg.AccessionNbr };
                    await ProduceMessage(message);
                    msg.MarkSentToETS();
                    msg.Save();
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "UnsolicitedMessages", "Execute", "Error processing unsolicited messages.");
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "UnsolicitedMessages", "Execute", "Unsolicited Messages job completed.", sw.ElapsedMilliseconds);
            }
            return isSuccess;
        }

        public async Task ProduceMessage(CreateManifest message)
        {
            if (message == null) return;
            var messageWrapper = new Message<CreateManifest> { Payload = message, Key = StringExtensions.Get9Digit(message.AccessionNumber), MessageId = Guid.NewGuid().ToString() };
            messageWrapper.AddHeader("log_key", message.AccessionNumber);
            MessagePartitionInfo result = await producer.ProduceAsync(messageWrapper);
            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Partition: {Partition}; Offset: {Offset}", "UnsolicitedMessages", "ProduceMessage", "Produced unsolicited message.", message.AccessionNumber, result.Partition, result.Offset);
        }
    }

}
