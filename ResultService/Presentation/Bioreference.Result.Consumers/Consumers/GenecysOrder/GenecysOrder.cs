using Bioreference.Common;
using Bioreference.Contracts.Result;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Enumerations;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using HL7Parser.Builders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class GenecysOrder : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Contracts.Result.GenecysOrder>
    {
        private readonly ILogger<GenecysOrder> _logger;
        private readonly IGenecysOrderProcessor _genecysOrderProcessor;

        public GenecysOrder(ILogger<GenecysOrder> logger, IGenecysOrderProcessor genecysOrderProcessor): base(logger)
        {
            _logger = logger;
            _genecysOrderProcessor = genecysOrderProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(
       MessageContext<Contracts.Result.GenecysOrder> messageContext)
        {

            try
            {
                if (messageContext?.Message?.Payload is null)
                {
                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "GenecysOrder",
                        "Skip",
                        "Empty message or payload not found."
                    );

                    return ConsumerProcessingResult.Skip("Empty message or payload not found");
                }

                var genecysOrder = messageContext.Message.Payload;

                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; MessageType: {MessageType}; Partition: {Partition}; Offset: {Offset}; SuccessFlag: {SuccessFlag}; FailedMessageLogId: {FailedMessageLogId}",
                    "GenecysOrder",
                    "Start",
                    nameof(Contracts.Result.GenecysOrder),
                    messageContext.PartitionInfo?.Partition,
                    messageContext.PartitionInfo?.Offset,
                    messageContext.PartitionInfo?.Success,
                    messageContext.PartitionInfo?.FailedMessageLogId
                );

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}",
                    "GenecysOrder",
                    "ProcessingBegin",
                    genecysOrder.AccessionNumber
                );

                ActivityHelper.SetAccessionLogKey(genecysOrder.AccessionNumber);
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    genecysOrder.AccessionNumber,
                    DateTime.UtcNow);

                Task.Run(() => ProcessGenecysOrder(genecysOrder)).Wait();

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ErrorMessage: {ErrorMessage}",
                    "GenecysOrder",
                    "Error",
                    messageContext.Message.Payload?.AccessionNumber,
                    "Failure",
                    ex.Message
                );

                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessGenecysOrder(Contracts.Result.GenecysOrder genecysOrder)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                    "GenecysOrder",
                    "ProcessingBegin",
                    genecysOrder?.AccessionNumber,
                    "ProcessGenecysOrder started."
                );

                await _genecysOrderProcessor.OnGenecysOrderMessage(genecysOrder);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Result: {Result}; ElapsedMs: {ElapsedMs}",
                    "GenecysOrder",
                    "Completed",
                    genecysOrder?.AccessionNumber,
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
                    "GenecysOrder",
                    "Error",
                    genecysOrder?.AccessionNumber,
                    "Failure",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message
                );

                throw;
            }
        }

    }
}
