using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Jobs
{
    public class OrderOutJob :Job
    {
        private readonly IBLISJob _orderOutJob;

        public OrderOutJob(IBLISJob orderOutJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {

            _orderOutJob = orderOutJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {

            try
            {
                Logger.LogDebug("Executing OrderOut Job...");

                Task.Run(() => _orderOutJob.Execute(), cancellationToken).Wait(cancellationToken);

                return Task.FromResult(JobResult.Success("OrderOut Job"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("OrderOut Job was canceled");
                return Task.FromResult(JobResult.Failure("OrderOut Job canceled"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OrderOut Job failed");
                return Task.FromResult(JobResult.Failure("OrderOut Job failed"));
            }
        }

    }
}
