using Bioreference.ResultService.DI.Interface;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IInboundReportingProcessor
    {
        public Task ProcessORUMessage(ORUMessage message);
    }
}
