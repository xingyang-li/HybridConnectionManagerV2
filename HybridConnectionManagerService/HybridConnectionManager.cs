using Azure.Core;
using HybridConnectionManager.Models;
using Serilog;
using System.Threading.Tasks;

namespace HybridConnectionManager.Service
{
    public class HybridConnectionManager
    {
        private static readonly Lazy<HybridConnectionManager> lazyInstance = new Lazy<HybridConnectionManager>();

        private TupleDictionary<string, string, HybridConnection> _hybridConnections;

        private object _readLock = new object();

        private AzureClient AzureClient;

        private Serilog.ILogger _logger;

        public static HybridConnectionManager Instance
        {
            get { return lazyInstance.Value; }
        }

        public HybridConnectionManager()
        {
            _logger = Log.Logger;
            AzureClient = new AzureClient();
            _hybridConnections = new TupleDictionary<string, string, HybridConnection>();
        }

        public void Initialize(List<HybridConnectionInformation> connectionInfos)
        {
            lock (_readLock)
            {
                foreach (var connectionInfo in connectionInfos)
                {
                    var hybridConnection = new HybridConnection(connectionInfo);
                    _hybridConnections.Add(connectionInfo.Namespace, connectionInfo.Name, hybridConnection);
                }
            }

            Task.Run(() => StartAll());
        }

        public async Task StartAll()
        {
            List<Task> tasks = new List<Task>();

            lock (_readLock)
            {
                foreach (var hybridConnection in _hybridConnections.Values)
                {
                    tasks.Add(hybridConnection.Open());
                }
            }

            await Task.WhenAll(tasks);
        }

        public void Shutdown()
        {
            lock (_readLock)
            {
                foreach (var hybridConnection in _hybridConnections.Values)
                {
                    hybridConnection.Dispose();
                }
            }
        }

        public async Task<HybridConnectionInformation> AddWithConnectionString(string connectionString)
        {
            HybridConnectionInformation hcInfo = null;

            if (String.IsNullOrEmpty(connectionString))
            {
                return hcInfo;
            }

            try
            {
                var hybridConnection = new HybridConnection(connectionString);
                await hybridConnection.Open();
                hcInfo = hybridConnection.Information;

                lock (_readLock)
                {
                    _hybridConnections[hcInfo.Namespace, hcInfo.Name] = hybridConnection;
                }

                UpdateConnectionsOnFileSystem();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }

            return hcInfo;
        }

        public async Task<HybridConnectionInformation> AddWithParameters(string subscription, string resourceGroup, string @namespace, string name)
        {
            string connectionString = await AzureHelper.GetConnectionStringFromAzure(AzureClient, subscription, resourceGroup, @namespace, name);

            return await AddWithConnectionString(connectionString);
        }

        public bool FindConnectionInformation(string @namespace, string name, out HybridConnectionInformation connectionInformation)
        {
            lock (_readLock)
            {
                connectionInformation = null;
                if (_hybridConnections.ContainsKeys(@namespace, name))
                {
                    connectionInformation = _hybridConnections[@namespace, name].Information;
                    return true;
                }
            }
            
            return false;
        }

        public bool FindConnectionInformation(string connectionString, out HybridConnectionInformation connectionInformation)
        {
            HybridConnectionInformation hcInfo = Util.GetInformationFromConnectionString(connectionString);
            return FindConnectionInformation(hcInfo.Namespace, hcInfo.Name, out connectionInformation);
        }

        public async Task<bool> RemoveConnection(string @namespace, string name)
        {
            bool removed = false;
            HybridConnection connectionToRemove = null;

            lock (_readLock)
            {
                if (!_hybridConnections.ContainsKeys(@namespace, name))
                {
                    return false;
                }

                connectionToRemove = _hybridConnections[@namespace, name];
                removed = _hybridConnections.Remove(@namespace, @name);
            }

            if (connectionToRemove != null)
            {
                await connectionToRemove.Close();
            }

            UpdateConnectionsOnFileSystem();

            return removed;
        }

        public List<HybridConnectionInformation> GetAllConnectionInformations()
        {
            List<HybridConnectionInformation> connectionInfos = new List<HybridConnectionInformation>();

            lock (_readLock)
            {
                foreach (var connection in _hybridConnections.Values)
                {
                    connectionInfos.Add(connection.Information);
                }
            }
            return connectionInfos;
        }

        public void UpdateConnectionsOnFileSystem()
        {
            List<HybridConnectionInformation> connectionInfos = new List<HybridConnectionInformation>();

            lock (_readLock)
            {
                foreach (var connection in _hybridConnections.Values)
                {
                    connectionInfos.Add(connection.Information);
                }
            }

            Util.UpdateAppDataFile(connectionInfos);
        }

        public async Task AuthenticateToAzure()
        {
            var token = await Auth.GetAuthTokenFromAzure();
            AzureClient.SetToken(token);
            _logger.Information(String.Format("Authenticated user to Azure with token expiring on {0}", token.ExpiresOn));
        }

        public AccessToken GetAuthToken()
        {
            return AzureClient.GetToken();
        }

        public bool IsValidToken()
        {
            return AzureClient.GetToken().ExpiresOn > DateTime.UtcNow;
        }
    }
}
