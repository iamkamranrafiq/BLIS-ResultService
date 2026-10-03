using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Jobs
{
    public class InstrumentMessagesJob : Job
    {
        private readonly IBLISJob _instrumentMessagesJob;

        public InstrumentMessagesJob(IBLISJob instrumentMessagesJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _instrumentMessagesJob = instrumentMessagesJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {

            try
            {
                Logger.LogDebug("Executing InstrumentMessages Job...");

                Task.Run(() => _instrumentMessagesJob.Execute(), cancellationToken).Wait(cancellationToken);

                return Task.FromResult(JobResult.Success("InstrumentMessages Job"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("InstrumentMessages Job was canceled.");
                return Task.FromResult(JobResult.Failure("InstrumentMessages Job canceled"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "InstrumentMessages Job failed.");
                return Task.FromResult(JobResult.Failure("InstrumentMessages Job failed"));
            }
        }

    }
}
