

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IInboundCompletedProcessor
    {
        public Task ProcessMessage(string flatWire);
    }
}
