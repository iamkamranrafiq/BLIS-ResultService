using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Payload
{
    public interface ISSUPayloadService
    {
        public Task<string[]> Message(string accessionNbr, DateTime dateServiced, bool? withOBX = true);
    }
}
