using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace HybridConnectionManager.Service
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (!File.Exists(Util.AppDataFilePath))
            {
                Util.CreateAppDataFile();
            }

            Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Util.AppDataLogPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

            Log.Logger.Information("Starting up Hybrid Connection Manager V2 Service with saved connections.");

            var connections = Util.LoadConnectionsFromFilesystem();

            HybridConnectionManager.Instance.Initialize(connections);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("https://localhost:5001");
        });
    }
}