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

        public async Task<HybridConnectionInformationResponse> AddUsingParameters(string subscription, string resourceGroup, string @namespace, string name)
        {
            var request = new HybridConnectionRequest
            {
                SubscriptionId = subscription,
                ResourceGroup = resourceGroup,
                Namespace = @namespace,
                Name = name,
            };

            return await _client.AddUsingParametersAsync(request);
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