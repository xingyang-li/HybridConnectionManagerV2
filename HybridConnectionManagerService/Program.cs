using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using HybridConnectionManager.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace HybridConnectionManagerV2.Service
{
    class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.WebHost.UseUrls("https://localhost:5001");
            builder.Services.AddGrpc();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<HybridConnectionManagerService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}