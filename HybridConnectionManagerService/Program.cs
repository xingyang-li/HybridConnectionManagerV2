using HybridConnectionManager.Library;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

namespace HybridConnectionManager.Service
{
    class Program
    {
        public static void Main(string[] args)
        {
            Util.SetupFileDependencies();

            Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Util.AppDataLogFileTemplate, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 104857600, shared: true)
            .CreateLogger();

            LogProvider.LogInfo("Starting up Hybrid Connection Manager V2 Service with saved connections.");

            var connections = Util.LoadConnectionsFromFilesystem();

            HybridConnectionManager.Instance.Initialize(connections);

            CreatHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreatHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        }).UseWindowsService().UseSystemd();
    }
}