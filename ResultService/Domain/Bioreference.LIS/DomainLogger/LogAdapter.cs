using Microsoft.Extensions.Logging;

namespace Bioreference.LIS
{
    public class LogAdapter : ILog
    {
        private readonly ILogger _logger;

        public LogAdapter(ILogger logger)
        {
            _logger = logger;
        }
      
        public void Debug(object message)
            => _logger.LogDebug("{Message}", message?.ToString());

        public void Info(object message)
            => _logger.LogInformation("{Message}", message?.ToString());

        public void Warn(object message)
            => _logger.LogWarning("{Message}", message?.ToString());

        public void Error(object message)
            => _logger.LogError("{Message}", message?.ToString());

        public void Error(string message, Exception exception)
            => _logger.LogError(exception, "{Message}", message);
   
        public void DebugFormat(string format, params object[] args)
            => _logger.LogDebug(format, args);

        public void InfoFormat(string format, params object[] args)
            => _logger.LogInformation(format, args);

        public void WarnFormat(string format, params object[] args)
            => _logger.LogWarning(format, args);

        public void ErrorFormat(string format, params object[] args)
            => _logger.LogError(format, args);
    }
}
