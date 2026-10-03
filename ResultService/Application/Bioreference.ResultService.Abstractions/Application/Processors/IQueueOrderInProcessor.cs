using Bioreference.ResultService.Common;

namespace Bioreference.ResultService.Abstractions.Application.Processors
{
    public interface IQueueOrderInProcessor
    {
        public Task<bool> ProcessMessage(Contracts.Order.Order order, ProcessOrderConfiguration processOrderConfig);
        public Task<bool> QueueOrderInStatus(long orderHistoryId, int status);
    }
}
