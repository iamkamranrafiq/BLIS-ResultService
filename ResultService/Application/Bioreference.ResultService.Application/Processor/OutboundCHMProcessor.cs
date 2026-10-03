using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Application.Processor
{
    public class OutboundCHMProcessor : IOutboundCHMProcessor
    {
        private ILogger<OutboundCHMProcessor> _logger;
        private readonly IPayloadSenderApiClient _payloadSenderApiClient;

        public OutboundCHMProcessor(ILogger<OutboundCHMProcessor> logger, IPayloadSenderApiClient payloadSenderApiClient)
        {
            _logger = logger;
            _payloadSenderApiClient = payloadSenderApiClient;
        }

        public async Task ProcessMessage(string message)
        {
            _logger.LogInformation(
             "Entity: {Entity}; Event: {Event}; Message: {message}",
             "OutboundCHM",
             "Send",
             message);
            await _payloadSenderApiClient.SendPayloadAsync(message, "OUTBOUNDCHM");
        }

    }
}
