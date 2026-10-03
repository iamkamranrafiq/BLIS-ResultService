using Bioreference.ResultService.DI.Interface;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IInboundProcessor
    {
        public Task OnORUMessage(ORUMessage message, string HL7String);
        public Task OnSSUMessage(SSUMessage message);
        public Task OnSTSMessage(STSMessage message);
        public Task OnORMMessage(ORMMessage message);
    }
}
