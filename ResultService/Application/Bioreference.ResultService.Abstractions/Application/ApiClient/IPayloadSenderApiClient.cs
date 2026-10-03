using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.ApiClient
{
    public interface IPayloadSenderApiClient
    {       
        Task<bool> SendPayloadAsync(string payload, string destination, CancellationToken cancellationToken = default);
    }
}
