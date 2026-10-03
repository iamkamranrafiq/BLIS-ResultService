using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.DI.Interface;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{ 
    public class Result : Bioreference.Messaging.Kafka.BaseConsumerProcessor<string>
    {
        private readonly ILogger<Result> _logger;
        private readonly IInboundProcessor _inboundProcessor;
       
        public Result(ILogger<Result> logger, IInboundProcessor inboundProcessor) : base(logger)
        {
            _logger = logger;
            _inboundProcessor = inboundProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<string> messageContext)
        {          
           
            try
            {                
                ActivityHelper.SetAccessionLogKey(messageContext.Message.Key);
                string HL7string = messageContext.Message.Payload;
                Task.Run(() => ProcessInbound(HL7string)).Wait();             
                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "ProcessMessage", "Exception occurred while processing Kafka message.");
                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessInbound(string HL7string)
        {
            var stopwatch = Stopwatch.StartNew();  


            string oruPath = Hl7FileHelper.GetMappingFilePath("ORU");
            _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; MappingPath: {MappingPath}", "Result", "Fetch", "ORU mapping file path resolved.", oruPath);

            string oruMappingJson = Hl7FileHelper.ReadMappingFile(oruPath);
            _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "Fetch", "ORU mapping file loaded.");

            string? mappedJson = Hl7Mapper.CreateMappedJson(HL7string, oruMappingJson, true);
            if (!String.IsNullOrWhiteSpace(mappedJson))
            {
                if (ResultService.Common.Utilities.CheckMappedHL7JsonError(mappedJson, out var errorDetail))
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; ErrorDetail: {ErrorDetail}; ElapsedTime: {ElapsedTime}",
                        "Result",
                        "ProcessInbound",
                        "Mapped HL7 JSON indicates an error from HL7 parser tool.",
                        string.IsNullOrWhiteSpace(errorDetail) ? "Unknown error" : errorDetail,
                        $"{stopwatch.ElapsedMilliseconds} ms");

                    return;
                }
            }
            else 
            {   
                stopwatch.Stop();
                _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Result", "ProcessInbound", "Failed to create mapped HL7 JSON.", $"{stopwatch.ElapsedMilliseconds} ms");
                return;
            }

            var messageType = Hl7MessageTypeHelper.GetMessageType(mappedJson);
            if (messageType == null)
            {
                stopwatch.Stop();

                _logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Result", "ProcessInbound", "Invalid mapped HL7 JSON.", $"{stopwatch.ElapsedMilliseconds} ms");

                return;
            }

            _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; MessageType: {MessageType}", "Result", "ProcessInbound", "HL7 message type detected.", messageType.ToString());

            switch (messageType)
            {
                case MessageType.ORU:
                   
                    var oruMessage = Hl7Mapper.Deserialize<ORUMessage>(mappedJson);
                    if (oruMessage != null)
                    {
                        _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "ORU", "ORU message deserialized. Calling inbound processor.");

                        await _inboundProcessor.OnORUMessage(oruMessage, HL7string);
                    }
                    break;

                case MessageType.SSU:
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "SSU", "Processing SSU message.");

                    var ssuMapping = Hl7FileHelper.ReadMappingFile(Hl7FileHelper.GetMappingFilePath("SSU"));
                    var ssuJson = Hl7Mapper.CreateMappedJson(HL7string, ssuMapping, true);
                    var ssuMessage = Hl7Mapper.Deserialize<SSUMessage>(ssuJson);

                    if (ssuMessage != null)
                    {
                        _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Result", "SSU", "SSU message deserialized. Calling inbound processor.");

                        await _inboundProcessor.OnSSUMessage(ssuMessage);
                    }
                    break;              

                default:
                    _logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; MessageType: {MessageType}", "Result", "ProcessInbound", "Unsupported HL7 MessageType encountered.", messageType);
                    break;
            }

            stopwatch.Stop();

            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Result", "ProcessInbound", "Inbound HL7 processing completed.", $"{stopwatch.ElapsedMilliseconds} ms");
        }
    }



}
