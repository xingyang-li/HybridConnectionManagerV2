using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;

namespace HybridConnectionManagerService
{
    public class HybridConnectionManager
    {
        private static readonly Lazy<HybridConnectionManager> lazyInstance = new Lazy<HybridConnectionManager>();

        private AzureClient AzureClient;

        public static HybridConnectionManager Instance
        {
            get { return lazyInstance.Value; }
        }

        public HybridConnectionManager()
        {
            AzureClient = new AzureClient();
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
