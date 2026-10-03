using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Payload
{
    public interface ISTSPayloadService
    {
        public Task<string> Message(string accessionNbr, DateTime dateServiced);
        public Task<List<string>> MessageComplete(string accessionNbr, DateTime dateServiced);
    }
}
