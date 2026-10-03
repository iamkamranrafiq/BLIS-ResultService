using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Jobs
{
    public class AuditJob : Job
    {
        private readonly IBLISJob _auditOutJob;

       public AuditJob(IBLISJob auditOutJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _auditOutJob = auditOutJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {

            try
            {
                Logger.LogDebug("Executing Audit Job...");

                Task.Run(() => _auditOutJob.Execute(), cancellationToken).Wait(cancellationToken);

                return Task.FromResult(JobResult.Success("Audit Job"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Audit Job was canceled.");
                return Task.FromResult(JobResult.Failure("Audit Job canceled"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Audit Job failed.");
                return Task.FromResult(JobResult.Failure("Audit Job failed"));
            }
        }

    }
}
