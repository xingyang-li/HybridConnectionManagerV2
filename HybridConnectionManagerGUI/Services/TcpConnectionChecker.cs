using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HybridConnectionManagerGUI.Services
{
    public class TcpConnectionChecker
    {
        private readonly ILogger<TcpConnectionChecker> _logger;

        public TcpConnectionChecker(ILogger<TcpConnectionChecker> logger)
        {
            _logger = logger;
        }

        public async Task<bool> CheckConnectionAsync(string host, int port, int timeoutMs = 1000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Create a task to try connecting with a timeout
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    // Wait for either connection or timeout
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    // If the connect task completed first and didn't throw an exception, we're connected
                    if (completedTask == connectTask && client.Connected)
                    {
                        return true;
                    }

                    // Either timed out or connection failed
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}