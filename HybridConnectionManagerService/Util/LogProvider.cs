using Serilog;

namespace HybridConnectionManager.Service
{
    public static class LogProvider
    {
        private static Serilog.ILogger _logger = Log.Logger;

        public static void LogOperation(string operation, string @namespace, string name)
        {
            _logger.Information("[OP: {0}] Operation started for connection {1}/{2}", operation, @namespace, name);
        }

        public static void LogTestEvent(string endpoint, string message)
        {
            _logger.Information("[TEST: {0}] {1}", endpoint, message);
        }

        public static void LogInfo(string message)
        {
            _logger.Information(message);
        }

        public static void LogError(string message)
        {
            _logger.Error(message);
        }
    }
}
