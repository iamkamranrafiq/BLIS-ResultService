using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Jobs
{
    public class ReportsOutNonCumJob : Job
    {
        private readonly IBLISJob job;
        public ReportsOutNonCumJob(IBLISJob job, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.job = job;
        }
        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                Logger.LogDebug("Executing ReportsOutNonCum Job...");

                Task.Run(() => job.Execute(), cancellationToken).Wait(cancellationToken);

                sw.Stop();
                Logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Report", "ReportsOutNonCum", "ReportsOutNonCum Job executed successfully.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Success("ReportsOutNonCum Job"));
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Report", "ReportsOutNonCum", "ReportsOutNonCum Job was canceled.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Failure("ReportsOutNonCum Job canceled"));
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Report", "ReportsOutNonCum", "ReportsOutNonCum Job failed.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Failure("ReportsOutNonCum Job failed"));
            }
        }

    }
}
