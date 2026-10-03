using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Jobs
{
    public class OutboundCHMJob : Job
    {
        private readonly IBLISJob _outboundCHMJob;

        public OutboundCHMJob(IBLISJob outboundCHMJob, ILoggerFactory loggerFactory) : base(loggerFactory) 
        {

            _outboundCHMJob = outboundCHMJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {

            try
            {
                Logger.LogDebug("Executing Outbound CHM Job...");

                Task.Run(() => _outboundCHMJob.Execute(), cancellationToken).Wait(cancellationToken);

                return Task.FromResult(JobResult.Success("Outbound CHM Job"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Outbound CHM Job was canceled.");
                return Task.FromResult(JobResult.Failure("Outbound CHM Job canceled"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Outbound CHM Job failed.");
                return Task.FromResult(JobResult.Failure("Outbound CHM Job failed"));
            }
        }


    }
}
