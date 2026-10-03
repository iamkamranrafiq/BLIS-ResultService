using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Jobs
{
    public class RequisitionStatusJob : Job
    {
        private readonly IBLISJob _requisitionStatusJob;

        public RequisitionStatusJob(IBLISJob requisitionStatusJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _requisitionStatusJob = requisitionStatusJob;
        }

        protected override async Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Logger.LogDebug("Executing Requisition Status Job...");

                await _requisitionStatusJob.Execute().ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "RequisitionStatus", "Requisition Status Job completed successfully.", sw.ElapsedMilliseconds);
                return JobResult.Success("Requisition Status Job");
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "RequisitionStatus", "Requisition Status Job was canceled.", sw.ElapsedMilliseconds);
                return JobResult.Failure("Requisition Status Job canceled.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "RequisitionStatus", "Error executing Requisition Status Job.", sw.ElapsedMilliseconds);
                return JobResult.Failure("Requisition Status Job failed.");
            }
        }
    }

}
