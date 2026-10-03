using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class ReportAck : Bioreference.Messaging.Kafka.BaseConsumerProcessor<string>
    {
        private readonly ILogger<ReportAck> _logger;
        private readonly IInboundProcessor _inboundProcessor;

        public ReportAck(ILogger<ReportAck> logger, IInboundProcessor inboundProcessor) : base(logger)
        {
            _logger = logger;
            _inboundProcessor = inboundProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<string> messageContext)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(messageContext?.Message?.Payload)) return ConsumerProcessingResult.Skip("Empty message payload");

                string HL7string = messageContext.Message.Payload;
               
                Task.Run(() => ProcessInbound(HL7string)).Wait();

                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "ReportAck", "ProcessMessage", "ReportAck message processed successfully.", sw.ElapsedMilliseconds);

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ErrorMessage: {ErrorMessage}; ElapsedTime: {ElapsedTime}", "ReportAck", "ProcessMessage", "Error while processing ReportAck message.", ex.Message, sw.ElapsedMilliseconds);
                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessInbound(string HL7string)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                string oruPath = Hl7FileHelper.GetMappingFilePath("ORU");
               
                string oruMappingJson = Hl7FileHelper.ReadMappingFile(oruPath);
               
                string? mappedJson = Hl7Mapper.CreateMappedJson(HL7string, oruMappingJson, true);
                if (!String.IsNullOrWhiteSpace(mappedJson))
                {
                    if (ResultService.Common.Utilities.CheckMappedHL7JsonError(mappedJson, out var errorDetail))
                    {
                        sw.Stop();

                        _logger.LogError(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; ErrorDetail: {ErrorDetail}; ElapsedTime: {ElapsedTime}",
                            "Result",
                            "ProcessInbound",
                            "Mapped HL7 JSON indicates an error from HL7 parser tool.",
                            string.IsNullOrWhiteSpace(errorDetail) ? "Unknown error" : errorDetail,
                            $"{sw.ElapsedMilliseconds} ms");

                        return;
                    }
                }
                else
                {
                    sw.Stop();
                    _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Result", "ProcessInbound", "Failed to create mapped HL7 JSON.", $"{sw.ElapsedMilliseconds} ms");
                    return;
                }

                var messageType = Hl7MessageTypeHelper.GetMessageType(mappedJson);
                if (messageType == null)
                {
                    sw.Stop();
                    _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "ReportAck", "ProcessInbound", "Invalid or missing MessageType.", sw.ElapsedMilliseconds);
                    return;
                }
               
                switch (messageType)
                {
                    case MessageType.FUNC:
                        var stsMapping = Hl7FileHelper.ReadMappingFile(Hl7FileHelper.GetMappingFilePath("STS"));
                        var stsJson = Hl7Mapper.CreateMappedJson(HL7string, stsMapping, true);
                        var stsMessage = Hl7Mapper.Deserialize<STSMessage>(stsJson);
                        if (stsMessage != null) await _inboundProcessor.OnSTSMessage(stsMessage);
                        break;

                    default:
                        _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; MessageType: {MessageType}", "ReportAck", "ProcessInbound", "Unsupported MessageType.", messageType);
                        break;
                }

                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "ReportAck", "ProcessInbound", "ProcessInbound completed.", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "ReportAck", "ProcessInbound", "Error in ProcessInbound.", sw.ElapsedMilliseconds);
                throw;
            }
        }
    }


}
