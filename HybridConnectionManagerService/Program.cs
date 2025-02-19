using Microsoft.Extensions.Logging;
using Serilog;

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

            var connections = Util.LoadConnectionsFromFilesystem();

            HybridConnectionManager.Instance.Initialize(connections);

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.WebHost.UseUrls("https://localhost:5001");
            builder.Services.AddGrpc();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<HybridConnectionManagerServiceController>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}