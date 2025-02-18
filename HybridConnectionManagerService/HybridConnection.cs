using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HybridConnectionManager.Models;
using Microsoft.Azure.Relay;

namespace HybridConnectionManager.Service
{
    public class HybridConnection : IDisposable
    {
        private const int OPEN_TIMEOUT_SECONDS = 21;
        private const int CLOSE_TIMEOUT_SECONDS = 5;

        private HybridConnectionListener _listener;

        public event EventHandler Online;

        private string _endpointHost;
        private int _endpointPort;
        private bool _isShuttingDown;

        private HybridConnectionInformation _hcInfo;

        private void CommonSetup()
        {
            _listener.Offline += ListenerOffline;
            _listener.Online += ListenerOnline;
            _listener.Connecting += ListenerConnecting;

            IsOpen = false;
            _isShuttingDown = false;

            Task.Run(() => RefreshConnectionInformation()).GetAwaiter().GetResult();
        }

        public HybridConnection(HybridConnectionInformation information)
        {
            _hcInfo = information;
            _listener = new(
                new(information.Uri),
                TokenProvider.CreateSharedAccessSignatureTokenProvider(information.KeyName, information.KeyValue));

            CommonSetup();
        }

        public HybridConnection(string connectionString)
        {
            _listener = new HybridConnectionListener(connectionString);
            _hcInfo = Util.GetInformationFromConnectionString(connectionString);
            CommonSetup();
        }

        public async Task RefreshConnectionInformation()
        {
            var runtimeInfo = await _listener.GetRuntimeInformationAsync();

            if (runtimeInfo != null)
            {
                var endpoint = Util.GetEndpointFromUserMetadata(runtimeInfo.UserMetadata);

                if (endpoint == null)
                {
                    throw new InvalidDataException("UserMetadata must contain an endpoint composed of a host and port.");
                }

                _hcInfo.EndpointHost = endpoint.Item1;
                _hcInfo.EndpointPort = endpoint.Item2;
                _hcInfo.CreatedOn = runtimeInfo.CreatedAt.ToString();
                _hcInfo.LastUpdated = runtimeInfo.UpdatedAt.ToString();
                _hcInfo.NumberOfListeners = runtimeInfo.ListenerCount;
                _hcInfo.Status = IsOnline ? "Connected": "Disconnected";

                _endpointHost = endpoint.Item1;
                _endpointPort = endpoint.Item2;
            }
            else
            {
                throw new InvalidOperationException("Unable to start relay without endpoint information.");
            }
        }

        public async Task Open(int timeoutSeconds = OPEN_TIMEOUT_SECONDS)
        {
            await _listener.OpenAsync(TimeSpan.FromSeconds(timeoutSeconds));
            IsOpen = true;
            AcceptAndRelay();
        }

        public async Task Close(int timeoutSeconds = CLOSE_TIMEOUT_SECONDS)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException();
            }
            _isShuttingDown = true;
            try
            {
                IsOpen = false;
                await _listener.CloseAsync(TimeSpan.FromSeconds(timeoutSeconds));
                Console.WriteLine(String.Format("Closed connection with namespace: {0} name: {1}", _hcInfo.Namespace, _hcInfo.Name));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Dispose()
        {
            Close().Wait();
        }

        public bool IsOnline
        {
            get
            {
                return _listener.IsOnline;
            }
        }

        public bool IsOpen
        {
            get; private set;
        }

        private async void AcceptAndRelay()
        {
            while (!_isShuttingDown)
            {
                Console.WriteLine("Listening to service bus..");
                try
                {
                    var hcStream = await _listener.AcceptConnectionAsync();
                    if (hcStream == null)
                    {
                        continue;
                    }
                    ConnectToEndpointAndRelay(hcStream);
                }
                catch (Exception e)
                {
                    // TODO log
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private async void ConnectToEndpointAndRelay(HybridConnectionStream hcStream)
        {
            Console.WriteLine("Recieved and relaying buffer..");
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(_endpointHost, _endpointPort);
                }
                catch (Exception ex)
                {
                    // TODO log
                    Console.WriteLine("Failed to connect to endpoint");

                    try
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                        {
                            await hcStream.CloseAsync(cts.Token);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    return;
                }

                Task sendTask = Util.PipeStream(hcStream, client.GetStream());
                Task receiveTask = Util.PipeStream(client.GetStream(), hcStream);

                await Task.WhenAll(sendTask, receiveTask);
            }

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    await hcStream.CloseAsync(cts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ListenerConnecting(object sender, EventArgs e)
        {
            // TODO log somewhere?
            Console.WriteLine("Listener is connecting");
        }

        private void ListenerOnline(object sender, EventArgs e)
        {
            Online?.Invoke(sender, e);
            // TODO log
            RefreshConnectionInformation();
            Console.WriteLine("Listener is online");
        }
        private void ListenerOffline(object sender, EventArgs e)
        {
            // TODO log
            Console.WriteLine("Listener is offline");
        }

        public HybridConnectionInformation Information
        {
            get
            {
                return _hcInfo;
            }
        }
    }
}
