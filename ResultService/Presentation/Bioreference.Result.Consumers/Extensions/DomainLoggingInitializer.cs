using Bioreference.LIS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Consumers.Extensions
{
    public class DomainLoggingInitializer : IHostedService
    {
        private readonly ILoggerFactory _loggerFactory;

        public DomainLoggingInitializer(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            LogManager.LoggerFactory = _loggerFactory;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
