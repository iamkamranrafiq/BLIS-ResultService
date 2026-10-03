using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface ICreateManifestProcessor
    {
        public Task OnCreateManifestMessage(CreateManifest createManifest);
    }
}
