using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using HybridConnectionManager.Models;

namespace HybridConnectionManager.Service
{
    public class HybridConnectionManager
    {
        private static readonly Lazy<HybridConnectionManager> lazyInstance = new Lazy<HybridConnectionManager>();

        private TupleDictionary<string, string, HybridConnection> _hybridConnections;

        private AzureClient AzureClient;

        public static HybridConnectionManager Instance
        {
            get { return lazyInstance.Value; }
        }

        public HybridConnectionManager()
        {
            AzureClient = new AzureClient();
            _hybridConnections = new TupleDictionary<string, string, HybridConnection>();
        }

        public void Initialize(List<HybridConnectionInformation> connectionInfos)
        {
            foreach (var connectionInfo in connectionInfos)
            {
                _hybridConnections.Add(connectionInfo.Namespace, connectionInfo.Name, new HybridConnection(connectionInfo));
            }

            Task.Run(() => StartAll());
        }

        public async Task StartAll()
        {
            List<Task> tasks = new List<Task>();

            foreach (var hybridConnection in _hybridConnections.Values)
            {
                tasks.Add(hybridConnection.Open());
            }

            await Task.WhenAll(tasks);
        }

        public async Task<HybridConnectionInformation> AddWithConnectionString(string connectionString)
        {
            HybridConnectionInformation hcInfo = null;

            try
            {
                var hybridConnection = new HybridConnection(connectionString);
                await hybridConnection.Open();
                hcInfo = hybridConnection.Information;
                _hybridConnections.Add(hcInfo.Namespace, hcInfo.Name, hybridConnection);

                UpdateConnectionsOnFileSystem();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return hcInfo;
        }

        public bool FindConnectionInformation(string @namespace, string name, out HybridConnectionInformation connectionInformation)
        {
            connectionInformation = null;
            if (_hybridConnections.ContainsKeys(@namespace, name))
            {
                connectionInformation = _hybridConnections[@namespace, name].Information;
                return true;
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
            if (!_hybridConnections.ContainsKeys(@namespace, name))
            {
                return false;
            }

            var connectionToRemove = _hybridConnections[@namespace, name];
            await connectionToRemove.Close();

            bool removed =  _hybridConnections.Remove(@namespace, @name);

            UpdateConnectionsOnFileSystem();

            return removed;
        }

        public void UpdateConnectionsOnFileSystem()
        {
            List<HybridConnectionInformation> connectionInfos = new List<HybridConnectionInformation>();

            foreach (var connection in _hybridConnections.Values)
            {
                connectionInfos.Add(connection.Information);
            }

            Util.UpdateAppDataFile(connectionInfos);
        }

        public async Task AuthenticateToAzure()
        {
            var token = await Auth.GetAuthTokenFromAzure();
            AzureClient.SetToken(token);

            var subscriptions = AzureClient.GetSubscriptions();
        }

        public AccessToken GetAuthToken()
        {
            return AzureClient.GetToken();
        }
    }
}
