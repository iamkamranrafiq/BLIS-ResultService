using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class ResultCompleted : Bioreference.Messaging.Kafka.BaseConsumerProcessor<string>
    {
        private readonly ILogger<ResultCompleted> _logger;
        private readonly IInboundCompletedProcessor _inboundCompletedProcessor;

        public ResultCompleted(ILogger<ResultCompleted> logger, IInboundCompletedProcessor inboundCompletedProcessor) : base(logger)
        {
            _logger = logger;
            _inboundCompletedProcessor = inboundCompletedProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<string> messageContext)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(messageContext?.Message?.Payload)) return ConsumerProcessingResult.Skip("Empty message payload");

                string message = messageContext.Message.Payload;
                Task.Run(() => _inboundCompletedProcessor.ProcessMessage(message)).Wait();

                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "ResultCompleted", "ProcessMessage", "ResultCompleted message processed successfully.", sw.ElapsedMilliseconds);

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ErrorMessage: {ErrorMessage} ms; ElapsedTime: {ElapsedTime}", "ResultCompleted", "ProcessMessage", "Error while processing ResultCompleted message.", ex.Message, sw.ElapsedMilliseconds);
                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }
    }

}
