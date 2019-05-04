using NLog;

namespace DependencyInjectionWorkshop.Adapter
{
    public class LogAdapter
    {
        public void LogFailedCount(string message)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }
    }
}