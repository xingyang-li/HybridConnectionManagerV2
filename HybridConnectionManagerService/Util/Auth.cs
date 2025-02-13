using Azure.Core;
using Azure.Identity;

namespace HybridConnectionManager.Service
{
    public class Auth
    {
        public static async Task<AccessToken> GetAuthTokenFromAzure()
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeAzureCliCredential = true,
                ExcludeInteractiveBrowserCredential = false
            });

            var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext);

            return token;
        }
    }
}
