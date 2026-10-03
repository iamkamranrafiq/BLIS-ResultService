using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.Genecys;
using Bioreference.ResultService.Common.Enumerations;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Bioreference.ResultService.Application.Jobs
{
    public class GenecysResultsJob : IBLISJob
    {
        private ILogger<GenecysResultsJob> logger;
        private readonly IMessageProducer<GenecysResult> producer;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsGenecysOrder appSettings;
        private GenecysOrdersResultsSettings settings;
        private string connectionString = string.Empty;

        public GenecysResultsJob(ILogger<GenecysResultsJob> logger, IProducerProvider provider, ISettingService settingsProvider, IOptions<AppSettingsGenecysOrder> options)
        {
            this.logger = logger;
            this.producer = provider.GetMessageProducer<GenecysResult>();
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }

        /// <summary>
        /// Process Genecys Results
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Execute()
        {
            bool isSuccess = false;

            logger.LogInformation(
                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                "GenecysResultsJob",
                "Execute",
                "Execution started."
            );

            try
            {
                connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");

                settings = await settingsProvider.FetchSetting<GenecysOrdersResultsSettings>(
                    connectionString,
                    "B2GenecysOrdersResults",
                    ""
                );

                int externalAppId = 5;

                logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; ExternalAppId: {ExternalAppId}; Status: {Status}",
                    "ReportsLite",
                    "Fetch",
                    "Fetching ReportsLite records.",
                    externalAppId,
                    transmitStatusType.Released
                );

                // ----------------------------
                // DB CALL #1 → Fetch Reports
                // ----------------------------
                var fetchReportsWatch = Stopwatch.StartNew();
                ReportsLite objReports = ReportsLite.Fetch(externalAppId, transmitStatusType.Released);
                fetchReportsWatch.Stop();

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}; ElapsedMs: {ElapsedMs}",
                    "ReportsLite",
                    "Fetch",
                    "Fetched ReportsLite records.",
                    objReports.List.Count,
                    fetchReportsWatch.ElapsedMilliseconds
                );

                int reportIndex = 0;

                foreach (ReportInfo ri in objReports.List)
                {
                    reportIndex++;

                    logger.LogDebug(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; Index: {Index}; Total: {Total}; ReportId: {ReportId}",
                        "GenecysResults",
                        "Process",
                        "Processing report.",
                        reportIndex,
                        objReports.List.Count,
                        ri.ReportId
                    );

                    ActivityHelper.SetAccessionLogKey(ri.AccessionNbr);
                    logger.LogInformation(
                        "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                        ri.AccessionNbr,
                        DateTime.UtcNow
                    );

                    try
                    {
                        // ----------------------------
                        // DB CALL #2 → Fetch Report
                        // ----------------------------
                        var fetchReportWatch = Stopwatch.StartNew();
                        Bioreference.LIS.Report r = OrderManager.FetchReport(ri.ReportId);
                        fetchReportWatch.Stop();

                        logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; ReportId: {ReportId}; ElapsedMs: {ElapsedMs}",
                            "Report",
                            "Fetch",
                            "Fetched report.",
                            ri.ReportId,
                            fetchReportWatch.ElapsedMilliseconds
                        );

                        if (r == null)
                        {
                            logger.LogWarning(
                                "Entity: {Entity}; Event: {Event}; Message: {Message}; ReportId: {ReportId}",
                                "Report",
                                "Fetch",
                                "Report returned null. Skipping.",
                                ri.ReportId
                            );

                            continue;
                        }

                        GenecysResult result = new GenecysResult()
                        {
                            AccessionNumber = r.AccessionNbr,
                            Results = new List<GenecysTestResult>()
                        };
     
                        foreach (Bioreference.LIS.ReportAnalyte a in r.Analytes.List)
                        {
                            if (ReadyToSend(a.TransmitStatus))
                            {
                                logger.LogInformation(
                                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}",
                                    "GenecysResults",
                                    "Add",
                                    "Processing analyte.",
                                    r.AccessionNbr,
                                    a.Analyte.Code
                                );

                                result.Results.Add(ProcessAnalyte(a));
                            }
                            else
                            {
                                logger.LogDebug(
                                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; TestCode: {TestCode}; Status: {Status}",
                                    "GenecysResults",
                                    "Skip",
                                    "Skipping analyte due to transmit status.",
                                    r.AccessionNbr,
                                    a.Analyte.Code,
                                    a.TransmitStatus
                                );
                            }
                        }

                        foreach (ReportAnalytePanel p in r.AnalytePanels.List)
                        {
                            if (ReadyToSend(p.TransmitStatus))
                            {
                                logger.LogInformation(
                                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; PanelId: {PanelId}",
                                    "GenecysResults",
                                    "Add",
                                    "Processing analyte panel ready to send.",
                                    r.AccessionNbr,
                                    p.Id
                                );

                                result.Results.AddRange(ProcessPanel(p));
                            }
                            else
                            {
                                logger.LogDebug(
                                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; PanelId: {PanelId}; Status: {Status}",
                                    "GenecysResults",
                                    "Skip",
                                    "Skipping panel due to transmit status.",
                                    r.AccessionNbr,
                                    p.Id,
                                    p.TransmitStatus
                                );
                            }
                        }

                        // ----------------------------
                        // Message production (not DB)
                        // ----------------------------
                        logger.LogDebug(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                            "GenecysResults",
                            "Produce",
                            "Producing Genecys message.",
                            r.AccessionNbr
                        );

                        await Task.Run(() => ProduceMessage(result));

                        // ----------------------------
                        // DB CALL #3 → Save Report
                        // ----------------------------
                        var saveWatch = Stopwatch.StartNew();
                        r.Save();
                        saveWatch.Stop();

                        logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ElapsedMs: {ElapsedMs}",
                            "Report",
                            "Save",
                            "Saved report.",
                            r.AccessionNbr,
                            saveWatch.ElapsedMilliseconds
                        );

                        r.Dispose();

                        logger.LogDebug(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}",
                            "GenecysResults",
                            "Process",
                            "Completed processing accession.",
                            r.AccessionNbr
                        );
                    }
                    catch (Exception exLoop)
                    {
                        logger.LogError(
                            exLoop,
                            "Entity: {Entity}; Event: {Event}; Message: {Message}; ReportId: {ReportId}",
                            "GenecysResults",
                            "Error",
                            "Error processing report. Continuing.",
                            ri.ReportId
                        );
                    }
                }

                isSuccess = true;

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "GenecysResultsJob",
                    "Execute",
                    "Genecys Results Job completed successfully."
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "GenecysResultsJob",
                    "Execute",
                    "Unhandled error occurred while processing Genecys Results."
                );
            }
            finally
            {
                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "GenecysResultsJob",
                    "Execute",
                    "Job execution ended."
                );
            }

            return isSuccess;
        }


        private bool ReadyToSend(transmitStatusType ts)
        {
            return ts == transmitStatusType.SentToReporting || ts == transmitStatusType.Released;
        }

        private List<GenecysTestResult> ProcessPanel(ReportAnalytePanel p)
        {
            logger.LogInformation($"Processing Genecys Panel: {p.Panel.Name}");
            p.MarkAsSentToReporting();
            p.MarkAsStatusSentToVertex();
            List<GenecysTestResult> results = new List<GenecysTestResult>();
            foreach (Bioreference.LIS.ReportAnalyte a in p.Analytes.List)
            {
                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Panel: {PanelName}; TestCode: {TestCode}",
                    "GenecysResults",
                    "Add",
                    "Processing Genecys Result for panel analyte.",
                    p.Panel.Name,
                    a.Analyte.Code
                ); results.Add(ProcessAnalyte(a));
            }
            return results;
        }

        private GenecysTestResult ProcessAnalyte(ReportAnalyte a)
        {
            a.MarkAsSentToReporting();
            a.MarkAsStatusSentToVertex();
            return new GenecysTestResult()
            {
                TestCode = a.Analyte.Code,
                TestName = a.Analyte.Name,
                Result = a.ResultValue,
                PerformingFacility = a.PerformingFacility,
                InstrumentId = a.InstrumentId
            };
        }

        public async Task ProduceMessage(GenecysResult message)
        {
            if (message == null) return;
            var messageWrapper = new Message<GenecysResult>
            {
                Payload = message,
                Key = StringExtensions.Get9Digit(message.AccessionNumber),
                MessageId = Guid.NewGuid().ToString(),
            };
            messageWrapper.AddHeader("log_key", message.AccessionNumber);
            MessagePartitionInfo result = await producer.ProduceAsync(messageWrapper);
            logger.LogDebug($"Produced Genecys Result Message for Accession: {message.AccessionNumber} to Partition: {result.Partition}, Offset: {result.Offset}");
        }
    }
}
