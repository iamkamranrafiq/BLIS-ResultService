using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Microsoft.Extensions.Logging;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using System.Diagnostics;

namespace Bioreference.ResultService.Jobs
{
    public class ReviseReportJob : Job
    {
        private readonly IBLISJob _revisedReportJob;

        public ReviseReportJob(IBLISJob revisedReportJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _revisedReportJob = revisedReportJob;
        }

        protected override async Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Logger.LogDebug("Executing Revised Report Job...");

                await _revisedReportJob.Execute().ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "ReviseReport", "Revised Report Job completed successfully.", sw.ElapsedMilliseconds);
                return JobResult.Success("Revised Report Job");
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "ReviseReport", "Revised Report Job was canceled.", sw.ElapsedMilliseconds);
                return JobResult.Failure("Revised Report Job canceled.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "ReviseReport", "Error executing Revised Report Job.", sw.ElapsedMilliseconds);
                return JobResult.Failure("Revised Report Job failed.");
            }
        }
    }

}
