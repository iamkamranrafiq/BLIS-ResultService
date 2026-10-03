using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.ReportsOut;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using static Bioreference.LIS.Reports;

namespace Bioreference.ResultService.Application.Jobs
{
    public class ReportsOutJob : IBLISJob
    {
        private ILogger<ReportsOutJob> logger;
        private readonly IMessageProducer<ReportOut> producer;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReportsOut appSettings;
        private string connectionString = string.Empty;
        private Reports.processDataType processDataType;

        public ReportsOutJob(ILogger<ReportsOutJob> logger, IProducerProvider provider, ISettingService settingsProvider, IOptions<AppSettingsReportsOut> options)
        {
            this.logger = logger;
            this.producer = provider.GetMessageProducer<ReportOut>();
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }

        public async Task<bool> Execute()
        {
            bool isSuccess = false;
            int count = 0;
            var stopwatch = Stopwatch.StartNew();

            try
            {               
                connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");

                int fetchCount = (int)appSettings.FetchCount;
                processDataType = (Reports.processDataType)appSettings.ProcessDataType;
                List<int> outboundOrderQueueId = new List<int>();   

                OutboundReport[] reports = Reports.FetchOutbound(fetchCount, 0, false, processDataType, 1, 0);

                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ReportCount: {ReportCount}",
                    "ReportsOut", "Fetch", "Fetched outbound reports.", reports.Length);

                foreach (OutboundReport report in reports)
                {
                    try
                    {
                        ActivityHelper.SetAccessionLogKey(report.AccessionNbr);
                        logger.LogInformation(
                            "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                            report.AccessionNbr,
                            DateTime.UtcNow
                        );

                        count++;

                        logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ReportId: {ReportId}; IsReportable: {IsReportable}",
                            "ReportsOut", "SendReport", "Sending ReportOut message.",
                            report.AccessionNbr, report.ReportId, report.IsReportable);

                        ReportOut message = new ReportOut()
                        {
                            AccessionNumber = report.AccessionNbr,
                            ReportId = report.ReportId,
                            IsReportable = report.IsReportable
                        };

                        await ProduceMessage(message);
                        outboundOrderQueueId.Add(report.OutboundOrderQueueId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ReportId: {ReportId}",
                            "ReportsOut", "SendReport", "Error processing report.",
                            report.AccessionNbr, report.ReportId);
                    }
                }
                if (outboundOrderQueueId.Count > 0)
                {
                    try
                    {
                        Reports.OutboundLogDelete(outboundOrderQueueId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}; Ids: {Ids}",
                            "ReportsOut",
                            "OutboundLog",
                            "Kafka messages were produced, but failed to delete records from OutboundOrderQueueLog.",
                            outboundOrderQueueId.Count,
                            string.Join(",", outboundOrderQueueId));
                    }
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "ReportsOut", "Execute", "Error processing ReportsOut job.");
            }
            finally
            {
                if (count > 0)
                {
                    stopwatch.Stop();
                    TimeSpan elapsed = stopwatch.Elapsed;

                    logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}; TotalSeconds: {TotalSeconds}; SecondsPerReport: {SecondsPerReport}",
                        "ReportsOut", "Summary", "ReportsOut processing summary.",
                        count, elapsed.TotalSeconds, elapsed.TotalSeconds / count);

                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "ReportsOut", "Execute", "ReportsOut job completed.");
                }
            }

            return isSuccess;
        }

        private async Task ProduceMessage(ReportOut msg)
        {
            if (msg == null) return;

            try
            {
                var wrapper = new Message<ReportOut>
                {
                    Payload = msg,
                    Key = msg.AccessionNumber,
                    MessageId = Guid.NewGuid().ToString(),
                };

                wrapper.AddHeader("log_key", msg.AccessionNumber);

                MessagePartitionInfo result = await producer.ProduceAsync(wrapper);

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Partition: {Partition}; Offset: {Offset}",
                    "ReportsOut", "ProduceMessage", "Message produced.",
                    msg.AccessionNumber, result.Partition, result.Offset);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                    "ReportsOut", "ProduceMessage", "Message production failed.",
                    msg.AccessionNumber);

                throw;
            }
        }
    }
}
