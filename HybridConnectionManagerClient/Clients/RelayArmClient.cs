using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Relay;
using Azure.ResourceManager.Relay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridConnectionManager.Library
{
    public class RelayArmClient : ArmClient
    {
        public static string HybridConnectionResourceIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Relay/namespaces/{2}/hybridconnections/{3}";

        public RelayArmClient(TokenCredential tokenCredential) : base(tokenCredential) { }

        public string GetHybridConnectionPrimaryConnectionString(string subscription, string resourceGroup, string @namespace, string name)
        {
            string resourceId = String.Format(HybridConnectionResourceIdTemplate, subscription, resourceGroup, @namespace, name);
            return GetHybridConnectionPrimaryConnectionString(resourceId);
        }

        public string GetHybridConnectionPrimaryConnectionString(string resourceId)
        {
            ResourceIdentifier resourceIdentifier = new ResourceIdentifier(resourceId);
            RelayHybridConnectionResource hcResource = this.GetRelayHybridConnectionResource(resourceIdentifier);

            var authRules = hcResource.GetRelayHybridConnectionAuthorizationRules();
            RelayHybridConnectionAuthorizationRuleResource bestRule = null;

            foreach (var authRule in authRules)
            {
                if (authRule.Data.Rights != null && authRule.Data.Rights.Count == 1)
                {
                    if (authRule.Data.Rights.ElementAt(0).ToString() == RelayAccessRight.Listen)
                    {
                        bestRule = authRule;
                    }
                }
            }

            if (bestRule == null)
            {
                foreach (var authRule in authRules)
                {
                    if (authRule.Data.Rights != null && authRule.Data.Rights.Count > 0)
                    {
                        foreach (var right in authRule.Data.Rights)
                        {
                            if (right == RelayAccessRight.Listen)
                            {
                                bestRule = authRule;
                            }
                        }
                    }
                }
            }

            var keys = bestRule.GetKeys();
            return keys.Value.PrimaryConnectionString;
        }
    }
}
