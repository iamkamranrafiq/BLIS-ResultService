using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IReflexProcessor
    {
        public Task OnReflexMessage(Reflex reflex);
    }
}
