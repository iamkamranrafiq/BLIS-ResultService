using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Jobs
{
    public class GenecysResultsJob : Job
    {
        private readonly IBLISJob _genecysResultsJob;

        public GenecysResultsJob(IBLISJob genecysResultsJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _genecysResultsJob = genecysResultsJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {

            try
            {
                Logger.LogDebug("Executing GenecysResults Job...");

                Task.Run(() => _genecysResultsJob.Execute(), cancellationToken).Wait(cancellationToken);

                return Task.FromResult(JobResult.Success("GenecysResults Job"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("GenecysResults Job was canceled.");
                return Task.FromResult(JobResult.Failure("GenecysResults Job canceled"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GenecysResults Job failed.");
                return Task.FromResult(JobResult.Failure("GenecysResults Job failed"));
            }
        }

    }
}
