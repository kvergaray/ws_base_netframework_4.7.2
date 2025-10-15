using System;


namespace WindowsService.Infrastructure.Helpers
{    public static class LogHelper
    {
        public static void Info(string message)
        {
            log4net.LogManager.GetLogger("AppLogger").Info(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            var logger = log4net.LogManager.GetLogger("AppLogger");
            logger.Error(message);
            if (ex != null) logger.Error(ex);
        }

        public static void Warn(string message)
        {
            log4net.LogManager.GetLogger("AppLogger").Warn(message);
        }
    }
}
