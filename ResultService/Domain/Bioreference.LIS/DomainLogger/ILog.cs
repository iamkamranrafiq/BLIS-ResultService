namespace Bioreference.LIS
{
    public interface ILog
    {
        void Debug(object message);
        void DebugFormat(string format, params object[] args);

        void Info(object message);
        void InfoFormat(string format, params object[] args);

        void Warn(object message);
        void WarnFormat(string format, params object[] args);

        void Error(object message);
        void Error(string message, Exception exception);
        void ErrorFormat(string format, params object[] args);
    }
}
