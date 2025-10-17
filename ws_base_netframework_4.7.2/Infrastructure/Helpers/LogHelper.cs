using System;
using log4net;

namespace WindowsService.Infrastructure.Helpers
{
    public static class LogHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger("AppLogger");

        public static void Info(string message)
        {
            Logger.Info(message);
        }

        public static void Warn(string message)
        {
            Logger.Warn(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            Logger.Error(message);
            if (ex != null)
            {
                Logger.Error(ex);
            }
        }
    }
}
