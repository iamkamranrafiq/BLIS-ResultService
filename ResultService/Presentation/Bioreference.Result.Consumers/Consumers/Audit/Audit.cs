using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class Audit : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Contracts.Result.AuditOut>
    {
        private readonly ILogger<Audit> _logger;
        private readonly IInboundAuditProcessor _inboundAuditProcessor;
        public Audit(ILogger<Audit> logger, IInboundAuditProcessor inboundAuditProcessor) : base(logger)
        {
            _logger = logger;
            _inboundAuditProcessor = inboundAuditProcessor;
        }
        public override ConsumerProcessingResult ProcessMessage(MessageContext<Contracts.Result.AuditOut> messageContext)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "AuditOut",
                    "ProcessingBegin",
                    "AuditOut message processing started."
                );

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; MessageType: {MessageType}",
                    "AuditOut",
                    "Start",
                    nameof(Contracts.Result.AuditOut)
                );

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Partition: {Partition}; Offset: {Offset}; SuccessFlag: {SuccessFlag}; FailedMessageLogId: {FailedMessageLogId}",
                    "AuditOut",
                    "Metadata",
                    messageContext.PartitionInfo?.Partition,
                    messageContext.PartitionInfo?.Offset,
                    messageContext.PartitionInfo?.Success,
                    messageContext.PartitionInfo?.FailedMessageLogId
                );

                var response = messageContext.Message.Payload;

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; MessageLength: {MessageLength}",
                    "AuditOut",
                    "Processing",
                    response?.Message?.Length ?? 0
                );

                _inboundAuditProcessor.ProcessMessage(response.Message);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "AuditOut",
                    "Completed",
                    "Success",
                    stopwatch.ElapsedMilliseconds
                );

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "AuditOut",
                    "Error",
                    "Failure",
                    stopwatch.ElapsedMilliseconds
                );

                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }


    }
}
