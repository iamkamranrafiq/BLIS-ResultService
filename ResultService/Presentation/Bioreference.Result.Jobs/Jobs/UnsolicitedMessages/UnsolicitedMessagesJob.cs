using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Application.Jobs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bioreference.ResultService.Jobs
{
    public class UnsolicitedMessagesJob : Job
    {
        private readonly IBLISJob _unsolicitedMessagesJob;

        public UnsolicitedMessagesJob(IBLISJob unsolicitedMessagesJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _unsolicitedMessagesJob = unsolicitedMessagesJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                Logger.LogDebug("Executing UnsolicitedMessages Job...");

                Task.Run(() => _unsolicitedMessagesJob.Execute(), cancellationToken).Wait(cancellationToken);

                sw.Stop();
                Logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Job", "UnsolicitedMessages", "UnsolicitedMessages Job completed successfully.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Success("UnsolicitedMessages Job"));
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Job", "UnsolicitedMessages", "UnsolicitedMessages Job was canceled.", sw.ElapsedMilliseconds);
                return Task.FromResult(JobResult.Failure("UnsolicitedMessages Job canceled"));
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Job", "UnsolicitedMessages", "UnsolicitedMessages Job failed.", sw.ElapsedMilliseconds);
                return Task.FromResult(JobResult.Failure("UnsolicitedMessages Job failed"));
            }
        }
    }
}
