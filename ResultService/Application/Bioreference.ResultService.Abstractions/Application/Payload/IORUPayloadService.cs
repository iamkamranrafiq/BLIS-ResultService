using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Payload
{
    public interface IORUPayloadService
    {
        public Task<string> Message(string accessionNbr, DateTime dateServiced);
    }
}
