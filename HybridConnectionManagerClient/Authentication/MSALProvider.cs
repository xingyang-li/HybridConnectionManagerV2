using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Net;

namespace HybridConnectionManager.Library
{
    public class MSALProvider
    {
        public static DefaultAzureCredential TokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = true,
            ExcludeInteractiveBrowserCredential = false
        }); 

        private IPublicClientApplication app;

        private const string ClientId = "044ec379-f702-470c-b242-6f689d69d533";
        private const string TenantId = "common";
        private const string Authority = $"https://login.microsoftonline.com/{TenantId}";
        private static readonly string[] Scopes = { "https://management.azure.com/.default" };

        public MSALProvider() 
        {
            app = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(Authority)
                .WithDefaultRedirectUri()
                .Build();

            TokenCacheHelper.EnableSerialization(app.UserTokenCache);
        }

        public static async Task<AccessToken> GetManagementTokenAsync()
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            return await TokenCredential.GetTokenAsync(tokenRequestContext);
        }

        public async Task<string> GetTokenAsync()
        {
            AuthenticationResult authenticationResult;
            try
            {
                var accounts = await app.GetAccountsAsync();
                authenticationResult = await app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
                                 .ExecuteAsync();
                return authenticationResult.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    authenticationResult = await app.AcquireTokenInteractive(Scopes)
                                    .WithPrompt(Prompt.SelectAccount)
                                    .ExecuteAsync();
                    return authenticationResult.AccessToken;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Interactive login failed: {ex.Message}");
                    Console.WriteLine("Falling back to device code flow...");
                    authenticationResult = await app.AcquireTokenWithDeviceCode(Scopes, deviceCodeResult =>
                    {
                        Console.WriteLine(deviceCodeResult.Message);
                        return Task.FromResult(0);
                    }).ExecuteAsync();
                    return authenticationResult.AccessToken;
                }
            }
        }
    }
}
