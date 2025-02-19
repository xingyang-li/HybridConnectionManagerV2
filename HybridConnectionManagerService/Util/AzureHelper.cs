using Serilog;
using System.Linq.Expressions;

namespace HybridConnectionManager.Service
{
    public static class AzureHelper
    {
        public async static Task<string> GetConnectionStringFromAzure(AzureClient azureClient, string subscription, string resourceGroup, string @namespace, string name)
        {
            try
            {
                string suitableAuthorizationRuleId = await GetSuitableAuthorizationRuleId(azureClient, subscription, resourceGroup, @namespace, name);

                if (!TryParseAuthorizationRuleId(suitableAuthorizationRuleId, out string _, out name, out string ruleName))
                {
                    return null;
                }

                if (name == null)
                {
                    var keys = await azureClient.GetNamespaceAuthorizationRuleKeys(subscription, resourceGroup, @namespace, ruleName);
                    return keys.PrimaryConnectionString;
                }
                else
                {
                    var keys = await azureClient.GetHybridConnectionAuthorizationRuleKeys(subscription, resourceGroup, @namespace, name, ruleName);
                    return keys.PrimaryConnectionString;

                }
            }
            catch (AzureClientException ex)
            {
                Log.Logger.Error("Could not get connection string for Hybrid Connection with namespace {0} and name {1} due to error: {2}", @namespace, name, ex.Content);
                return null;
            }
        }
        
        public async static Task<string> GetSuitableAuthorizationRuleId(AzureClient azureClient, string subscription, string resourceGroup, string @namespace, string name)
        {
            string suitableRuleId = await GetSuitableAuthorizationRuleIdFromUri(azureClient, subscription, resourceGroup, @namespace, name);

            if (suitableRuleId == null)
            {
                // We have no suitable key. If we are looking at relay, look at namespace instead. Otherwise, fail.
                suitableRuleId = await GetSuitableAuthorizationRuleIdFromUri(azureClient, subscription, resourceGroup, @namespace);

            }

            return suitableRuleId;
        }

        public async static Task<string> GetSuitableAuthorizationRuleIdFromUri(AzureClient azureClient, string subscription, string resourceGroup, string @namespace, string name = null)
        {
            string suitableRuleId = null;
            AzureListResponseEnvelope<AuthorizationRules> azureListAuthRulesResponse = new AzureListResponseEnvelope<AuthorizationRules>();

            if (name != null)
            {
                azureListAuthRulesResponse = await azureClient.GetHybridConnectionAuthorizationRules(subscription, resourceGroup, @namespace, name);
            }
            else
            {
                azureListAuthRulesResponse = await azureClient.GetNamespaceAuthorizationRules(subscription, resourceGroup, @namespace);
            }
            

            if (azureListAuthRulesResponse != null &&  azureListAuthRulesResponse.Value != null)
            {
                // Look for a rule that ONLY has the 'Listen' right 
                foreach (var authRulesResponse in azureListAuthRulesResponse.Value)
                {
                    if (authRulesResponse.Properties != null && authRulesResponse.Properties.Rights != null && authRulesResponse.Properties.Rights.Count == 1)
                    {
                        if (authRulesResponse.Properties.Rights[0].Equals("Listen", StringComparison.OrdinalIgnoreCase))
                        {
                            suitableRuleId = authRulesResponse.Id;
                        }
                    }
                }

                // Look for any rule that has the 'Listen' right
                if (suitableRuleId == null)
                {
                    foreach (var authRulesResponse in azureListAuthRulesResponse.Value)
                    {
                        if (authRulesResponse.Properties != null && authRulesResponse.Properties.Rights != null)
                        {
                            foreach (var right in authRulesResponse.Properties.Rights)
                            {
                                if (right.Equals("Listen", StringComparison.OrdinalIgnoreCase))
                                {
                                    suitableRuleId = authRulesResponse.Id;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return suitableRuleId;
        }

        public static bool TryParseAuthorizationRuleId(string armId, out string @namespace, out string name, out string ruleName)
        {
            @namespace = null;
            name = null;
            ruleName = null;

            var uriElements = armId.Split('/');

            try
            {
                @namespace = uriElements[8];
                if (uriElements.Length == 13)
                {
                    name = uriElements[10];
                    ruleName = uriElements[12];
                }
                else
                {
                    name = null;
                    ruleName = uriElements[10];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
