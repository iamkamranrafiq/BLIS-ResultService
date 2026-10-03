using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Bioreference.ResultService.Application.Jobs
{
    public class AuditOutJob : IBLISJob
    {
        private ILogger<AuditOutJob> _logger;
        private readonly IMessageProducer<AuditOut> _auditOutProducer;
        private string connectionString = string.Empty;
        private readonly ISettingService settingsProvider;

        public AuditOutJob(ILogger<AuditOutJob> logger, ISettingService settingsProvider, IProducerProvider provider = null)
        {
            _logger = logger;
            _auditOutProducer = provider.GetMessageProducer<AuditOut>();
            this.settingsProvider = settingsProvider;

        }

        public async Task<bool> Execute()
        {
            bool isSuccess = false;

            _logger.LogInformation(
                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                "CocAuditJob",
                "Execute",
                "Execution started."
            );

            try
            {
                _logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "CocAuditJob",
                    "Execute",
                    "Audit Out Job started (debug)."
                );

                connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");

                // -------------------------------
                // DB CALL 1 → CocAudits.Fetch()
                // -------------------------------
                var fetchWatch = Stopwatch.StartNew();
                CocAudits coca = CocAudits.Fetch();
                fetchWatch.Stop();

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}; ElapsedMs: {ElapsedMs}",
                    "CocAudit",
                    "Fetch",
                    "Fetched CocAudit records.",
                    coca.CocAuditList.Count,
                    fetchWatch.ElapsedMilliseconds
                );

                var idList = new List<int>();
                var auditStringList = new List<string>();
                bool bypassFinalizing = false;

                int loopIndex = 0;

                // PROCESS CocAudit records (no DB calls inside)
                foreach (CocAudit o in coca.CocAuditList)
                {
                    loopIndex++;

                    ActivityHelper.SetLogKey("ReportId: " + o.ReportId);

                    _logger.LogDebug(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; Index: {Index}; Total: {Total}; CocAuditId: {CocAuditId}; ReportId: {ReportId}",
                        "CocAudit",
                        "Process",
                        "Processing CocAudit record.",
                        loopIndex,
                        coca.CocAuditList.Count,
                        o.CocAuditId,
                        o.ReportId
                    );

                    // Build audit string
                    var sb = new StringBuilder();
                    DateTime dt;
                    string by = "";
                    string msg = "";

                    if (!string.IsNullOrWhiteSpace(o.UpdatedBy))
                    {
                        dt = o.UpdatedDate;
                        by = o.UpdatedBy;

                        msg = o.IsBatchAccessionDeleted
                            ? $"Deleted from COC Batch: {o.CocBatchId}"
                            : $"Added to COC Batch: {o.CocBatchId}";
                    }
                    else
                    {
                        dt = o.CreatedDate;
                        by = o.CreatedBy;
                        msg = $"Added to COC Batch: {o.CocBatchId}";
                    }

                    sb.Append(dt.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture))
                      .Append("|0|")
                      .Append(o.ReportId).Append("|Bioreference.LIS.Report|")
                      .Append(by).Append("|")
                      .Append(msg)
                      .Append("||||0|0|0|");

                    auditStringList.Add(sb.ToString());
                    idList.Add(o.CocAuditId);

                    _logger.LogDebug(
                        "Entity: {Entity}; Event: {Event}; Message: {Message}; CocAuditId: {CocAuditId}",
                        "CocAudit",
                        "Add",
                        "Record processed.",
                        o.CocAuditId
                    );
                }

                string mergedAuditString = "";

                if (auditStringList.Count > 0)
                {
                    try
                    {
 
                        mergedAuditString = ConvertToSingleString(auditStringList);

                        _logger.LogInformation(
                            "Entity: {Entity}; Event: {Event}; Message: {Message}",
                            "AuditProducer",
                            "Produce",
                            "Producing audit message."
                        );

                        await Task.Run(() => ProduceAuditOutMessage(mergedAuditString));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Entity: {Entity}; Event: {Event}; Message: {Message}",
                            "AuditProducer",
                            "Produce",
                            "Unable to write audit."
                        );

                        bypassFinalizing = true;
                    }

                    // -------------------------------
                    // DB CALL 2 → coca.SetAsProcessed()
                    // -------------------------------
                    if (!bypassFinalizing)
                    {
                        try
                        {
                            var finalizeWatch = Stopwatch.StartNew();
                            coca.SetAsProcessed(idList);
                            finalizeWatch.Stop();

                            _logger.LogInformation(
                                "Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}; ElapsedMs: {ElapsedMs}",
                                "CocAudit",
                                "Set",
                                "Successfully marked CocAudits as processed.",
                                idList.Count,
                                finalizeWatch.ElapsedMilliseconds
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Entity: {Entity}; Event: {Event}; Message: {Message}",
                                "CocAudit",
                                "Finalize",
                                "Error marking audits as processed."
                            );
                        }
                    }
                }

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "CocAuditJob",
                    "Execute",
                    "--- Job COMPLETED ---"
                );

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "CocAuditJob",
                    "Error",
                    "Error retrieving CocAudits."
                );
            }

            return isSuccess;
        }

        private string ConvertToSingleString(List<string> lst)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string s in lst)
                sb.Append(s + Constants.vbCrLf);
            return sb.ToString();
        }

        public async Task ProduceAuditOutMessage(string message)
        {
          
            if (message != null)
            {
                AuditOut auditOut = new AuditOut();
                auditOut.Message = message;

                var messageWrapper = new Message<AuditOut>
                {
                    Payload = auditOut,
                    Key = Guid.NewGuid().ToString(),
                    MessageId = Guid.NewGuid().ToString(),
                };
                MessagePartitionInfo result = await _auditOutProducer.ProduceAsync(messageWrapper);
            }
        }
    }
}
