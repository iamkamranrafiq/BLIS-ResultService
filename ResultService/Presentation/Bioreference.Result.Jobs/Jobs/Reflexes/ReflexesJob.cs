using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Jobs
{
    public class ReflexesJob : Job
    {
        private readonly IBLISJob job;

        public ReflexesJob(IBLISJob job, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.job = job;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                Logger.LogDebug("Executing Reflexes Job...");

                Task.Run(() => job.Execute(), cancellationToken).Wait(cancellationToken);

                sw.Stop();
                Logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Job", "Reflexes", "Reflexes Job completed successfully.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Success("Reflexes Job"));
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Job", "Reflexes", "Reflexes Job was canceled.", sw.ElapsedMilliseconds);
                return Task.FromResult(JobResult.Failure("Reflexes Job canceled"));
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Job", "Reflexes", "Reflexes Job failed.", sw.ElapsedMilliseconds);
                return Task.FromResult(JobResult.Failure("Reflexes Job failed"));
            }
        }
    }
}
