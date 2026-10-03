using Bioreference.Contracts.Result;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class OutboundCHM : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Contracts.Result.Outbound_CHM>
    {
        private readonly ILogger<OutboundCHM> _logger;
        private readonly IOutboundCHMProcessor _outboundCHMProcessor;

        public OutboundCHM(ILogger<OutboundCHM> logger, IOutboundCHMProcessor outboundCHMProcessor)
            : base(logger)
        {
            _logger = logger;
            _outboundCHMProcessor = outboundCHMProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<Contracts.Result.Outbound_CHM> messageContext)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; MessageType: {MessageType}",
                    "Outbound_CHM",
                    "Start",
                    nameof(Contracts.Result.Outbound_CHM)
                );

                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Partition: {Partition}; Offset: {Offset}; SuccessFlag: {SuccessFlag}; FailedMessageLogId: {FailedMessageLogId}",
                    "Outbound_CHM",
                    "Metadata",
                    messageContext.PartitionInfo?.Partition,
                    messageContext.PartitionInfo?.Offset,
                    messageContext.PartitionInfo?.Success,
                    messageContext.PartitionInfo?.FailedMessageLogId
                );

                var response = messageContext.Message.Payload;

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; MessageLength: {MessageLength}",
                    "Outbound_CHM",
                    "Processing",
                    response?.Message?.Length ?? 0
                );

                if(response != null)
                {
                    Task.Run(() => _outboundCHMProcessor.ProcessMessage(response.Message)).Wait(); ;
                }
                else {                     
                        _logger.LogWarning(
                        "Entity: {Entity}; Event: {Event}; Result: {Result}; Message: {Message}",
                        "Outbound_CHM",
                        "Processing",
                        "Failure",
                        "Received null payload in message context.");
                }



                stopwatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "Outbound_CHM",
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
                    "Entity: {Entity}; Event: {Event}; Result: {Result}; ElapsedMs: {ElapsedMs}; ErrorMessage: {ErrorMessage}",
                    "Outbound_CHM",
                    "Error",
                    "Failure",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message
                );

                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

    }

}
