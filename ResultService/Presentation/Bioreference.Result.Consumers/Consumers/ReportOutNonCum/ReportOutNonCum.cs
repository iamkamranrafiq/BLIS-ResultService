using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Consumers
{
    public class ReportOutNonCum : BaseConsumerProcessor<Contracts.Result.ReportOutNonCum>
    {
        private readonly ILogger<ReportOutNonCum> logger;
        private readonly IReportOutNonCumProcessor processor;

        public ReportOutNonCum(ILogger<ReportOutNonCum> logger, IReportOutNonCumProcessor processor)
            : base(logger)
        {
            this.logger = logger;
            this.processor = processor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<Contracts.Result.ReportOutNonCum> messageContext)
        {
            try
            {
                logger.LogInformation("ProcessMessage started for ReportOutNonCum");

                logger.LogInformation(
                    "ReportOutNonCum Message Metadata. Partition={Partition}, Offset={Offset}, SuccessFlag={Success}, FailedMessageLogId={FailedMessageLogId}",
                    messageContext.PartitionInfo?.Partition,
                    messageContext.PartitionInfo?.Offset,
                    messageContext.PartitionInfo?.Success,
                    messageContext.PartitionInfo?.FailedMessageLogId
                );

                var reportOutNonCum = messageContext.Message.Payload;

                ActivityHelper.SetAccessionLogKey(reportOutNonCum.AccessionNbr);
                logger.LogInformation(
                    "Processing ReportOutNonCum. Accession={Accession}, ReportId={ReportId}",
                    reportOutNonCum?.AccessionNbr,
                    reportOutNonCum?.ReportId
                );

                Task.Run(() => ProcessReportOutNonCum(reportOutNonCum)).Wait();

                logger.LogInformation(
                    "ProcessMessage completed successfully for ReportOutNonCum. Accession={Accession}, ReportId={ReportId}",
                    reportOutNonCum?.AccessionNbr,
                    reportOutNonCum?.ReportId
                );

                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error while processing ReportOutNonCum message. Accession={Accession}, ReportId={ReportId}. Error={ErrorMessage}",
                    messageContext.Message?.Payload?.AccessionNbr,
                    messageContext.Message?.Payload?.ReportId,
                    ex.Message
                );

                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }

        public async Task ProcessReportOutNonCum(Contracts.Result.ReportOutNonCum reportOutNonCum)
        {
            await processor.OnReportOutNonCumMessage(reportOutNonCum);
        }
    }

}
