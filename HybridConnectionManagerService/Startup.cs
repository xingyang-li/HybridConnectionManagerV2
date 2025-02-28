using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

namespace HybridConnectionManager.Service
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<HybridConnectionManagerServiceController>();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });

            var applicationLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        public void OnShutdown()
        {
            Log.Logger.Information("Shutting down Hybrid Connection Manager V2 Service.");
            HybridConnectionManager.Instance.Shutdown();
            Log.Logger.Information("Shut down Hybrid Connection Manager V2 Service.");
        }
    }
}
