using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class CreateManifest : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Contracts.Result.CreateManifest>
    {
        private readonly ILogger<CreateManifest> _logger;
        private readonly ICreateManifestProcessor _createManifestProcessor;

        public CreateManifest(ILogger<CreateManifest> logger, ICreateManifestProcessor createManifestProcessor): base(logger)
        {
            _logger = logger;
            _createManifestProcessor = createManifestProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(
      MessageContext<Contracts.Result.CreateManifest> messageContext)
        {

            try
            {
                if (messageContext?.Message?.Payload is null)
                {
                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "CreateManifest",
                        "Skip",
                        "Empty message or payload not found."
                    );

                    return ConsumerProcessingResult.Skip("Empty message or payload not found");
                }

                var createManifest = messageContext.Message.Payload;

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; MessageType: {MessageType}",
                    "CreateManifest",
                    "Start",
                    nameof(Contracts.Result.CreateManifest)
                );

                Task.Run(() => ProcessCreateManifest(createManifest)).Wait();

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Result: {Result};",
                    "CreateManifest",
                    "Error",
                    "Failure");

                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessCreateManifest(Contracts.Result.CreateManifest createManifest)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                    "CreateManifest",
                    "ProcessingBegin",
                    createManifest?.AccessionNumber,
                    "ProcessCreateManifest started."
                );

                ActivityHelper.SetAccessionLogKey(createManifest?.AccessionNumber);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    createManifest?.AccessionNumber,
                    DateTime.UtcNow);

                await _createManifestProcessor.OnCreateManifestMessage(createManifest);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "CreateManifest",
                    "Completed",
                    createManifest?.AccessionNumber,
                    "Success",
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "CreateManifest",
                    "Error",
                    createManifest?.AccessionNumber,
                    "Failure",
                    stopwatch.ElapsedMilliseconds
                );

                throw;
            }
        }


    }

}
