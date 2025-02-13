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

        public async Task Start()
        {
            List<Task> tasks = new List<Task>();

            foreach (var hybridConnection in _hybridConnections.Values)
            {
                tasks.Add(hybridConnection.Open());
            }

            await Task.WhenAll(tasks);
        }

        public async Task AddConnectionWithConnectionStringAsync(string connectionString)
        {
            var hybridConnection = new HybridConnection(connectionString);
            var hcInfo = Util.GetInformationFromConnectionString(connectionString);
            await hybridConnection.Open();

            _hybridConnections.Add(hcInfo.Namespace, hcInfo.Name, hybridConnection);
        }

        public HybridConnection FindConnection(string @namespace, string name){
            if (_hybridConnections.ContainsKeys(@namespace, name))
            {
                return _hybridConnections[@namespace, name];
            }

            return null;
        }

        public async Task RemoveConnection(string @namespace, string name)
        {
            if (!_hybridConnections.ContainsKeys(@namespace, name))
            {
                return;
            }

            var connectionToRemove = _hybridConnections[@namespace, name];
            await connectionToRemove.Close();

            _hybridConnections.Remove(@namespace, @name);
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
