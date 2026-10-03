using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bioreference.LIS
{
    public static class LogManager
    {
        public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

        public static ILog GetLogger(string name)
        {
            var msLogger = LoggerFactory.CreateLogger(name);
            return new LogAdapter(msLogger);
        }

        public static ILog GetLogger<T>()
        {
            var msLogger = LoggerFactory.CreateLogger<T>();
            return new LogAdapter(msLogger);
        }
        public static ILog GetLogger(Type type)
        {
            var logger = LoggerFactory.CreateLogger(type);
            return new LogAdapter(logger);
        }
    }
}
