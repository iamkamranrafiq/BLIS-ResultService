using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Consumers
{ 
    public class ReportOut : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Contracts.Result.ReportOut>
    {
        private readonly ILogger<ReportOut> _logger;
        private readonly IReportOutProcessor _processor;

        private const string JobName = "Bioreference.ResultService.Consumers.ReportOut";

        public ReportOut(ILogger<ReportOut> logger, IReportOutProcessor processor) : base(logger)
        {
            _logger = logger;
            _processor = processor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<Contracts.Result.ReportOut> messageContext)
        {
            var stopwatch = Stopwatch.StartNew();
           
            try
            {
                // Kafka metadata as structured object
                var messageMetadata = new
                {
                    Partition = messageContext.PartitionInfo?.Partition,
                    Offset = messageContext.PartitionInfo?.Offset,
                    Success = messageContext.PartitionInfo?.Success,
                    FailedMessageLogId = messageContext.PartitionInfo?.FailedMessageLogId
                };

                _logger.LogDebug("Entity: {Entity}; Event: {Event}; Message: {Message}; Details: {@Details}", "ReportOut", "ProcessMessage", "Kafka message received for ReportOut.", messageMetadata);


                var reportOut = messageContext.Message.Payload;

                // Set activity key
                ActivityHelper.SetAccessionLogKey(reportOut.AccessionNumber);
                _logger.LogInformation(
                     "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                     reportOut.AccessionNumber,
                     DateTime.UtcNow);

                // Process synchronously
                Task.Run(() => ProcessReportOut(reportOut)).Wait();

                stopwatch.Stop();

                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "ReportOut", "ProcessMessage", "ReportOut message processed successfully.", stopwatch.ElapsedMilliseconds);
              
                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Best-effort report info even in failure
                var failedReport = messageContext.Message?.Payload == null
                    ? null
                    : new
                    {
                        messageContext.Message.Payload.AccessionNumber,
                        messageContext.Message.Payload.ReportId
                    };

                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms; Report: {@ReportDetails}", "ReportOut", "ProcessMessage", "Error while processing ReportOut message.", stopwatch.ElapsedMilliseconds, failedReport);


                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessReportOut(Contracts.Result.ReportOut reportOut)
        {
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ReportId: {ReportId}; Accession: {Accession}", "ReportOut", "ProcessReportOut", "Dispatching ReportOut to processor.", reportOut.ReportId, reportOut.AccessionNumber);
            await _processor.OnReportOutMessage(reportOut);
        }
    }


}


