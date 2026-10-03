using Bioreference.ResultService.DI.Interface;
using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IGenecysOrderProcessor
    {
        public Task OnGenecysOrderMessage(GenecysOrder genecysOrder);
    }
}
