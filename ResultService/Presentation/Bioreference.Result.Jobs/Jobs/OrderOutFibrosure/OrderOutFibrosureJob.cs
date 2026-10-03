using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Jobs
{
    public class OrderOutFibrosureJob :Job
    {
        private readonly IBLISJob _orderOutFibrosureJob;

        public OrderOutFibrosureJob(IBLISJob orderOutFibrosureJob, ILoggerFactory loggerFactory) : base(loggerFactory)
        {

            _orderOutFibrosureJob = orderOutFibrosureJob;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {

            try
            {
                Logger.LogDebug("Executing Order Out Fibrosure Job...");

                Task.Run(() => _orderOutFibrosureJob.Execute(), cancellationToken).Wait(cancellationToken);

                return Task.FromResult(JobResult.Success("Order Out Fibrosure Job"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Order Out Fibrosure Job was canceled");
                return Task.FromResult(JobResult.Failure("Order Out Fibrosure Job canceled"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Order Out Fibrosure Job failed");
                return Task.FromResult(JobResult.Failure("Order Out Fibrosure Job failed"));
            }
        }

    }
}
