using HybridConnectionManager.Library;
using Microsoft.Azure.Relay;
using System.Net.Sockets;
using Serilog;
using System.Net.Http.Headers;

namespace HybridConnectionManager.Service
{
    public class HybridConnection : IDisposable
    {
        private const int OPEN_TIMEOUT_SECONDS = 21;
        private const int CLOSE_TIMEOUT_SECONDS = 5;

        private const int RETRY_OPEN_DELAY = 60000;

        private HybridConnectionListener _listener;

        public event EventHandler Online;

        private string _endpointHost;
        private int _endpointPort;
        private bool _isShuttingDown;

        private Serilog.ILogger _logger;

        private HybridConnectionInformation _hcInfo;

        private void SetupListenerHandlers()
        {
            _listener.Offline += ListenerOffline;
            _listener.Online += ListenerOnline;
            _listener.Connecting += ListenerConnecting;
        }

        private void CommonSetup()
        {

            SetupListenerHandlers();
            IsOpen = false;
            _isShuttingDown = false;

            RefreshConnectionInformation().GetAwaiter().GetResult();
        }

        public HybridConnection(HybridConnectionInformation information)
        {
            _logger = Log.Logger;
            _hcInfo = information;
            _listener = new(
                new(information.Uri),
                TokenProvider.CreateSharedAccessSignatureTokenProvider(information.KeyName, information.KeyValue));

            CommonSetup();
        }

        public HybridConnection(string connectionString)
        {
            _logger = Log.Logger;
            _listener = new HybridConnectionListener(connectionString);
            _hcInfo = Util.GetInformationFromConnectionString(connectionString);
            CommonSetup();
        }

        public async Task RefreshConnectionInformation()
        {
            HybridConnectionRuntimeInformation runtimeInfo = null;

            try
            {
                runtimeInfo = await _listener.GetRuntimeInformationAsync();
            }
            catch (EndpointNotFoundException ex)
            {
                _hcInfo.Status = "NotFound";
                _logger.Error("Unable to retrieve runtime details for connection {0}/{1}. Hybrid Connection resource may not exist on Azure anymore. Error: {2}", _hcInfo.Namespace, _hcInfo.Name, ex.Message);
                return;
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                if (error.Contains("Ip has been prevented to connect to the endpoint"))
                {
                    error = "Ip has been prevented to connect to the endpoint";
                }

                _hcInfo.Status = "Disconnected";
                _logger.Error("Unable to retrieve runtime details for connection {0}/{1}. Error: {2}", _hcInfo.Namespace, _hcInfo.Name, error);
                return;
            }

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
            if (!String.IsNullOrEmpty(_hcInfo.Status) && _hcInfo.Status == "NotFound")
            {
                return;
            }

            try
            {
                await _listener.OpenAsync(TimeSpan.FromSeconds(timeoutSeconds));
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                if (error.Contains("Ip has been prevented to connect to the endpoint"))
                {
                    error = "Ip has been prevented to connect to the endpoint";
                }

                _logger.Error("Could not register listener for {0}/{1} in Service Bus with error: {2}", this.Information.Namespace, this.Information.Name, error);

                Thread.Sleep(RETRY_OPEN_DELAY);
                Task.Factory.StartNew(() => RetryOpening());
                return;
            }

            IsOpen = true;
            Task.Factory.StartNew(() => AcceptAndRelay());
        }

        public async Task Close(int timeoutSeconds = CLOSE_TIMEOUT_SECONDS)
        {
            if (!IsOpen)
            {
                return;
            }

            _isShuttingDown = true;
            try
            {
                await _listener.CloseAsync(TimeSpan.FromSeconds(timeoutSeconds));
            }
            catch (Exception ex)
            {
                _logger.Error("Could not close listener for {0}/{1} in ServiceBus with error: {2}", this.Information.Namespace, this.Information.Name, ex.Message);
                return;

            }
            IsOpen = false;
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

        private async Task RetryOpening()
        {
            while (!_isShuttingDown && !IsOpen)
            {
                try
                {
                    _listener = new( new(_hcInfo.Uri), TokenProvider.CreateSharedAccessSignatureTokenProvider(_hcInfo.KeyName, _hcInfo.KeyValue));
                    SetupListenerHandlers();
                    await _listener.OpenAsync(TimeSpan.FromSeconds(OPEN_TIMEOUT_SECONDS));
                    IsOpen = true;
                    Task.Factory.StartNew(() => AcceptAndRelay());
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                    if (error.Contains("Ip has been prevented to connect to the endpoint"))
                    {
                        error = "Ip has been prevented to connect to the endpoint";
                    }

                    _logger.Error("Could not register listener for {0}/{1} in Service Bus with error: {2}. Retrying...", this.Information.Namespace, this.Information.Name, error);
                    Thread.Sleep(RETRY_OPEN_DELAY);
                }
            }
        }

        private async Task AcceptAndRelay()
        {
            while (!_isShuttingDown)
            {
                try
                {
                    var hcStream = await _listener.AcceptConnectionAsync();
                    if (hcStream == null)
                    {
                        continue;
                    }
                    Task.Factory.StartNew(() => ConnectToEndpointAndRelay(hcStream));
                }
                catch (Exception e)
                {
                    // TODO log
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private async Task ConnectToEndpointAndRelay(HybridConnectionStream hcStream)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(_endpointHost, _endpointPort);

                    Task sendTask = Util.PipeStream(hcStream, client.GetStream());
                    Task receiveTask = Util.PipeStream(client.GetStream(), hcStream);

                    await Task.WhenAll(sendTask, receiveTask);
                }
                catch (Exception ex)
                {
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
            _logger.Information("Listener connecting for connection {0}/{1}", _hcInfo.Namespace, _hcInfo.Name);
        }

        private void ListenerOnline(object sender, EventArgs e)
        {
            Online?.Invoke(sender, e);
            _logger.Information("Listener online for connection {0}/{1}", _hcInfo.Namespace, _hcInfo.Name);

            RefreshConnectionInformation().GetAwaiter().GetResult();
        }

        private void ListenerOffline(object sender, EventArgs e)
        {
            _logger.Information("Listener offline for connection {0}/{1}", _hcInfo.Namespace, _hcInfo.Name);

            if (!_isShuttingDown)
            {
                RefreshConnectionInformation().GetAwaiter().GetResult();
            }
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
