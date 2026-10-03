using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.ReportOutNonCum;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Bioreference.ResultService.Application.Jobs
{
    public class ReportOutNonCumJob : IBLISJob
    {
        private ILogger<ReportOutNonCumJob> logger;
        private readonly IMessageProducer<ReportOutNonCum> producer;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReportOutNonCum appSettings;
        private ReportOutNonCumSettings settings = null;
        private string connectionString = string.Empty;
        int TotalChannelCount = 1;
        int FetchCount;
        int CurrentChannelIndex;
        int AllergenReportType;
        bool ResultOut_SendOrderedCode;
        private List<string> ResultsOut_TestsNoRefBypass = null;

        public ReportOutNonCumJob(ILogger<ReportOutNonCumJob> logger, IProducerProvider provider, ISettingService settingsProvider, IOptions<AppSettingsReportOutNonCum> options)
        {
            this.logger = logger;
            this.producer = provider.GetMessageProducer<ReportOutNonCum>();
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

                settings = await settingsProvider.FetchSetting<ReportOutNonCumSettings>(connectionString, "ResultOutEngine", "");

                FetchCount = this.appSettings.FetchCount;
                CurrentChannelIndex = settings.CurrentChannelIndex;
                AllergenReportType = this.appSettings.AllergenReportType;
                ResultOut_SendOrderedCode = settings.ResultOut_SendOrderedCode;
                ResultsOut_TestsNoRefBypass = settings.ResultsOut_TestsNoRefBypass;
              
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; AllergenReportType: {AllergenReportType}",
                    "ReportOutNonCum", "Fetch", "Fetching outbound reports.", AllergenReportType);

                OutboundAllergenQueueItem[] reports = OutboundAllergenQueue.FetchOutbound(
                    FetchCount,
                    AllergenReportType,
                    TotalChannelCount,
                    CurrentChannelIndex);

                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ReportCount: {ReportCount}",
                    "ReportOutNonCum", "Fetch", "Fetched outbound reports.", reports.Length);

                if (reports.Length == 0)
                {
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "ReportOutNonCum", "Execute", "No reports to process.");
                }

                foreach (OutboundAllergenQueueItem report in reports)
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

                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ReportId: {ReportId}; IsReportable: {IsReportable}",
                            "ReportOutNonCum", "SendReport", "Sending ReportOutNonCum message.", report.AccessionNbr, report.ReportId, true);

                        ReportOutNonCum message = new ReportOutNonCum()
                        {
                            AccessionNbr = report.AccessionNbr,
                            ReportId = report.ReportId,
                            IsReportable = true
                        };

                        await ProduceMessage(message);
                    }
                    catch (Exception exLoop)
                    {
                        logger.LogError(exLoop,
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ReportId: {ReportId}",
                            "ReportOutNonCum", "SendReport", "Error sending ReportOutNonCum message. Continuing.", report.AccessionNbr, report.ReportId);
                    }
                }

                isSuccess = true;
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "ReportOutNonCum", "Execute", "ReportsOutNonCum processing completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "ReportOutNonCum", "Execute", "Error processing ReportsOutNonCum.");
            }
            finally
            {
                if (count > 0)
                {
                    stopwatch.Stop();
                    TimeSpan elapsed = stopwatch.Elapsed;
                    double secondsPerReport = elapsed.TotalSeconds / count;

                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}; TotalSeconds: {TotalSeconds}; SecondsPerReport: {SecondsPerReport}",
                        "ReportOutNonCum", "Summary", "ReportsOutNonCum processing summary.", count, elapsed.TotalSeconds, secondsPerReport);

                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "ReportOutNonCum", "Execute", "ReportsOutNonCum End.");
                }
                else
                {
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}",
                        "ReportOutNonCum", "Execute", "ReportsOutNonCum End - no reports were processed.");
                }
            }

            return isSuccess;
        }


        private async Task ProduceMessage(ReportOutNonCum msg)
        {
            if (msg == null) return;

            try
            {
                var wrapper = new Message<ReportOutNonCum>
                {
                    Payload = msg,
                    Key = msg.AccessionNbr,
                    MessageId = Guid.NewGuid().ToString(),
                };
                wrapper.AddHeader("log_key", msg.AccessionNbr);

                MessagePartitionInfo result = await producer.ProduceAsync(wrapper);

                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Partition: {Partition}; Offset: {Offset}", "ReportOutNonCum", "ProduceMessage", "Message produced to Kafka.", msg.AccessionNbr, result.Partition, result.Offset);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "ReportOutNonCum", "ProduceMessage", "Error producing ReportOutNonCum message.", msg.AccessionNbr);
                throw;
            }
        }
    }
}
