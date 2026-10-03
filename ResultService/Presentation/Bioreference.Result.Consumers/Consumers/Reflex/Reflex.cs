using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class Reflex : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Contracts.Result.Reflex>
    {
        private readonly ILogger<Reflex> _logger;
        private readonly IReflexProcessor _reflexProcessor;

        public Reflex(ILogger<Reflex> logger, IReflexProcessor reflexProcessor)
            : base(logger)
        {
            _logger = logger;
            _reflexProcessor = reflexProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(
      MessageContext<Contracts.Result.Reflex> messageContext)
        {
            try
            {
                if (messageContext?.Message?.Payload is null)
                {
                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "Reflex",
                        "Skip",
                        "Empty message or payload not found."
                    );

                    return ConsumerProcessingResult.Skip("Empty message or payload not found");
                }

                var reflex = messageContext.Message.Payload;

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; MessageType: {MessageType}",
                    "Reflex",
                    "Start",
                    nameof(Contracts.Result.Reflex)
                );

                Task.Run(() => ProcessReflex(reflex)).Wait();

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ErrorMessage: {ErrorMessage}",
                    "Reflex",
                    "Error",
                    messageContext.Message.Payload?.AccessionNumber,
                    "Failure",
                    ex.Message
                );

                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessReflex(Contracts.Result.Reflex reflex)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                    "Reflex",
                    "ProcessingBegin",
                    reflex?.AccessionNumber,
                    "ProcessReflex started."
                );

                ActivityHelper.SetAccessionLogKey(reflex?.AccessionNumber);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    reflex?.AccessionNumber,
                    DateTime.UtcNow);

                await _reflexProcessor.OnReflexMessage(reflex);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "Reflex",
                    "Completed",
                    reflex?.AccessionNumber,
                    "Success",
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ElapsedMs: {ElapsedMs}; ErrorMessage: {ErrorMessage}",
                    "Reflex",
                    "Error",
                    reflex?.AccessionNumber,
                    "Failure",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message
                );

                throw;
            }
        }

    }

}


