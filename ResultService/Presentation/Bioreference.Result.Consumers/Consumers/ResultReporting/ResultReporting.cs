using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using HL7Parser.Builders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{
    public class ResultReporting : Bioreference.Messaging.Kafka.BaseConsumerProcessor<string>
    {
        private readonly ILogger<ResultReporting> _logger;
        private readonly IInboundReportingProcessor _inboundReportingProcessor;

        public ResultReporting(ILogger<ResultReporting> logger, IInboundReportingProcessor inboundReportingProcessor) : base(logger)
        {
            _logger = logger;
            _inboundReportingProcessor = inboundReportingProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<string> messageContext)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(messageContext?.Message?.Payload)) return ConsumerProcessingResult.Skip("Empty message payload");

                string HL7string = messageContext.Message.Payload;
                Task.Run(() => ProcessInboundReporting(HL7string)).Wait();

                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "ResultReporting", "ProcessMessage", "ResultReporting message processed successfully.", sw.ElapsedMilliseconds);

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ErrorMessage: {ErrorMessage}; ElapsedTime: {ElapsedTime}", "ResultReporting", "ProcessMessage", "Error while processing ResultReporting message.", ex.Message, sw.ElapsedMilliseconds);
                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessInboundReporting(string HL7string)
        {
            var sw = Stopwatch.StartNew();
            try
            {            
                string ORUMapping = Hl7FileHelper.ReadMappingFile(Hl7FileHelper.GetMappingFilePath("ResultReportingORU"));
                var ORUJson = Hl7Mapper.CreateMappedJson(HL7string, ORUMapping, true);
                if (!String.IsNullOrWhiteSpace(ORUJson))
                {
                    if (ResultService.Common.Utilities.CheckMappedHL7JsonError(ORUJson, out var errorDetail))
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
                var oruMessage = Hl7Mapper.Deserialize<ORUMessage>(ORUJson);

                if (oruMessage != null)
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "ResultReporting", "Deserialize", "Deserialization successful. Invoking ProcessORUMessage.");
                    await _inboundReportingProcessor.ProcessORUMessage(oruMessage);
                }
                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "ResultReporting", "ProcessInboundReporting", "ProcessInboundReporting completed.", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "ResultReporting", "ProcessInboundReporting", "Error in ProcessInboundReporting.", sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

}
