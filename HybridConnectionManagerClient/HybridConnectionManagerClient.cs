using Grpc.Core;
using Grpc.Net.Client;
using HcManProto;

namespace HybridConnectionManager.Client
{
    public class HybridConnectionManagerClient
    {
        public HcMan.HcManClient _client;
        public HybridConnectionManagerClient()
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            _client = new HcMan.HcManClient(channel);
        }

        public async Task<HybridConnectionInformationResponse> AddUsingConnectionString(string connectionString)
        {
            var request = new HybridConnectionRequest { ConnectionString = connectionString };
            return await _client.AddUsingConnectionStringAsync(request);
        }

        public async Task<HybridConnectionInformationResponse> AddUsingHybridConnectionInfo(string @namespace, string name, string resourceGroup, string subscriptionId, string keyName, string keyValue)
        {
            var request = new HybridConnectionRequest
            {
                Namespace = @namespace,
                Name = name,
                SubscriptionId = subscriptionId,
                ResourceGroup = resourceGroup,
                KeyName = keyName,
                KeyValue = keyValue
            };

            return await _client.AddUsingHybridConnectionInfoAsync(request);
        }

        public async Task<HybridConnectionInformationResponse> ShowConnection(string @namespace, string name)
        {
            var request = new HybridConnectionRequest { Namespace = @namespace, Name = name };
            var response = await _client.ShowConnectionAsync(request);
            return response;
        }

        public async Task<IEnumerable<HybridConnectionInformationResponse>> ListConnections()
        {
            var request = new EmptyRequest();
            var response = await _client.ListConnectionsAsync(request);
            return response.ConnectionInformations;
        }

        public async Task<string> RemoveConnection(string @namespace, string name)
        {
            var request = new HybridConnectionRequest { Namespace = @namespace, Name = name };
            var response = await _client.RemoveConnectionAsync(request);
            return response.Content;
        }
        public async Task<string> TestEndpointForConnection(string endpoint)
        {
            var request = new EndpointRequest { Endpoint = endpoint };
            var response = await _client.TestEndpointForConnectionAsync(request);
            return response.Content;
        }

        public async Task<string> AuthenticateUser()
        {
            var request = new EmptyRequest();
            var response = await _client.AuthenticateUserAsync(request);
            return response.Content;
        }
    }
}