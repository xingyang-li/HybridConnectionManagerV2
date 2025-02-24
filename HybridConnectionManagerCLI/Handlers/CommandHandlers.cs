using Azure.Identity;
using Azure.ResourceManager.Relay;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using HcManProto;
using HybridConnectionManager.Library;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Runtime.Serialization;
using Grpc.Core;
using Azure.Core;

namespace HybridConnectionManager.CLI
{
    public static class CommandHandlers
    {
        public static JsonSerializerSettings HybridConnectionJsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new IgnorePropertiesResolver(new[] { "Error", "ErrorMessage" }) };
        public static string EndpointRegexPattern = "^([a-zA-Z0-9.-]+):(\\d{1,5})$";
        public static string HcConnectionStringRegexPattern = @"^Endpoint=sb:\/\/[a-zA-Z0-9-]+\.servicebus\.windows\.net\/;SharedAccessKeyName=[a-zA-Z0-9-]+;SharedAccessKey=[a-zA-Z0-9+\/=]+;EntityPath=[a-zA-Z0-9-]+$";
        public static string RootConnectionStringRegexPattern = @"^Endpoint=sb:\/\/[a-zA-Z0-9-]+\.servicebus\.windows\.net\/;SharedAccessKeyName=[a-zA-Z0-9-]+;SharedAccessKey=[a-zA-Z0-9+\/=]+$";

        public static MSALProvider MSALProvider = new MSALProvider();

        public static async Task LoginHandler()
        {
            /*
            try
            {
                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = await client.AuthenticateUser();
                Console.WriteLine(response);
            }
            catch (RpcException ex)
            {
                Console.WriteLine("Could not reach Hybrid Connection Manager V2. Please ensure HybridConnectionManagerV2 is running and https://localhost:5001 is reachable.");
                return;
            }*/

            /*
            var result = await MSALProvider.GetTokenAsync();
            Console.WriteLine($"Access Token: {result}");
            */

            var credential = MSALProvider.TokenCredential;

            var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext);
            Console.WriteLine($"Access Token retrieved successfully. Expires on: {token.ExpiresOn}");

        }

        public static async Task AddHandler(string connectionString, string subscription, string resourceGroup, string @namespace, string name)
        {
            try
            {
                bool connectionStringSupplied = true;
                bool interactiveMode = false;

                if (string.IsNullOrEmpty(connectionString))
                {
                    if (string.IsNullOrEmpty(subscription) && string.IsNullOrEmpty(resourceGroup) && string.IsNullOrEmpty(@namespace) && string.IsNullOrEmpty(name))
                    {
                        interactiveMode = true;
                    }
                    else if (string.IsNullOrEmpty(subscription) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(name))
                    {
                        Console.WriteLine("Must specify either <connection-string> or ALL of { --subscription, --resource-group, --namespace, --name }");
                        return;
                    }
                    connectionStringSupplied = false;
                }
                else
                {
                    if (!string.IsNullOrEmpty(subscription) || !string.IsNullOrEmpty(resourceGroup) || !string.IsNullOrEmpty(@namespace) || !string.IsNullOrEmpty(name))
                    {
                        Console.WriteLine("Must specify either <connection-string> or ALL of { --subscription, --resource-group, --namespace, --name }");
                        return;
                    }

                    if (!Regex.IsMatch(connectionString, HcConnectionStringRegexPattern) && !Regex.IsMatch(connectionString, RootConnectionStringRegexPattern))
                    {
                        Console.WriteLine(String.Format("Connection string {0} is invalid. Connection string must be of form: Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<keyName>;SharedAccessKey=<keyValue>;EntityPath=<name>", connectionString));
                        return;
                    }
                }

                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                RelayArmClient relayArmClient = null;

                //string accessToken = MSALProvider.GetTokenAsync().Result;
                //var credential = new BearerTokenCredential(accessToken);

                // TODO: figure out auth, using default azure credential for now
                var credential = MSALProvider.TokenCredential;

                if (interactiveMode)
                {
                    Console.WriteLine("\nInteractive Mode");
                    Console.WriteLine("-----------------");
                    Console.WriteLine("\nLogging in to Azure..\n");

                    relayArmClient = new RelayArmClient(credential);
                    if (!StartInteractiveMode(relayArmClient, out connectionString))
                    {
                        return;
                    }
                }

                if (!connectionStringSupplied && !interactiveMode)
                {
                    relayArmClient = new RelayArmClient(credential);
                    connectionString = relayArmClient.GetHybridConnectionPrimaryConnectionString(subscription, resourceGroup, @namespace, name);
                }

                var response = await client.AddUsingConnectionString(connectionString);

                if (response.Error)
                {
                    Console.WriteLine(response.ErrorMessage);
                }
                else
                {
                    var responseString = JsonConvert.SerializeObject(response, Formatting.Indented, HybridConnectionJsonSettings);
                    Console.WriteLine(responseString);
                    Console.WriteLine("Connection added successfully");
                }
            }
            catch (RpcException ex)
            {
                Console.WriteLine("Could not reach Hybrid Connection Manager V2. Please ensure HybridConnectionManagerV2 is running and https://localhost:5001 is reachable.");
                return;
            }
        }

        public static async Task RemoveHandler(string @namespace, string name)
        {
            try
            {
                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = await client.RemoveConnection(@namespace, name);
                Console.WriteLine(response);
            }
            catch (RpcException ex)
            {
                Console.WriteLine("Could not reach Hybrid Connection Manager V2. Please ensure HybridConnectionManagerV2 is running and https://localhost:5001 is reachable.");
                return;
            }
        }

        public static async Task ShowHandler(string @namespace, string name)
        {
            try
            {
                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = await client.ShowConnection(@namespace, name);

                if (response.Error)
                {
                    Console.WriteLine(response.ErrorMessage);
                }
                else
                {
                    var responseString = JsonConvert.SerializeObject(response, Formatting.Indented, HybridConnectionJsonSettings);
                    Console.WriteLine(responseString);
                }
            }
            catch (RpcException ex)
            {
                Console.WriteLine("Could not reach Hybrid Connection Manager V2. Please ensure HybridConnectionManagerV2 is running and https://localhost:5001 is reachable.");
                return;
            }
        }

        public static async Task ListHandler()
        {
            try
            {
                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = await client.ListConnections();
                var responseString = JsonConvert.SerializeObject(response, Formatting.Indented, HybridConnectionJsonSettings);
                Console.WriteLine(responseString);
            }
            catch (RpcException ex)
            {
                Console.WriteLine("Could not reach Hybrid Connection Manager V2. Please ensure HybridConnectionManagerV2 is running and https://localhost:5001 is reachable.");
                return;
            }
        }

        public static async Task TestHandler(string endpoint)
        {
            try
            {
                if (!Regex.IsMatch(endpoint, EndpointRegexPattern))
                {
                    Console.WriteLine(String.Format("Endpoint {0} is invalid. Endpoint must be of form: <host>:<port>", endpoint));
                    return;
                }

                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = await client.TestEndpointForConnection(endpoint);
                Console.WriteLine(response);
            }
            catch (RpcException ex)
            {
                Console.WriteLine("Could not reach Hybrid Connection Manager V2. Please ensure HybridConnectionManagerV2 is running and https://localhost:5001 is reachable.");
                return;
            }
        }

        public static bool StartInteractiveMode(RelayArmClient relayArmClient, out string connectionString)
        {
            List<SubscriptionResource> subscriptionResourcesList = new List<SubscriptionResource>();
            List<RelayHybridConnectionResource> hybridConnectionResourcesList = new List<RelayHybridConnectionResource>();
            int index = 0;

            connectionString = null;

            var subscriptionsResourceCollection = relayArmClient.GetSubscriptions();

            foreach (var subscriptionResource in subscriptionsResourceCollection)
            {
                Console.WriteLine(String.Format("[{0}] {1} ({2})", index, subscriptionResource.Data.DisplayName, subscriptionResource.Data.SubscriptionId));
                index++;
                subscriptionResourcesList.Add(subscriptionResource);
            }

            Console.Write("\nSelect a subscription [Id]: ");
            var subscriptionIndex = Console.ReadLine();

            if (!int.TryParse(subscriptionIndex, out int subIndex) || subIndex < 0 || subIndex >= subscriptionResourcesList.Count)
            {
                Console.WriteLine("Invalid choice.");
                return false;
            }

            var chosenSub = subscriptionResourcesList[subIndex];

            Console.WriteLine(string.Format("\nRetrieving Hybrid Connections for {0}..\n", chosenSub.Data.DisplayName));

            var resourceGroupsCollection = chosenSub.GetResourceGroups();
            index = 0;

            StringWriter stringWriter = new StringWriter();
            stringWriter.WriteLine("Id    Name        Region         Namespace       Endpoint");
            stringWriter.WriteLine("---   -----       ------         ---------       --------");

            foreach (var resourceGroupResource in resourceGroupsCollection)
            {
                var relayNamespacesCollection = resourceGroupResource.GetRelayNamespaces();
                foreach (var relayNamespaceResource in relayNamespacesCollection)
                {
                    var hybridConnectionsCollection = relayNamespaceResource.GetRelayHybridConnections();
                    foreach (var hybridConnectionResource in hybridConnectionsCollection)
                    {
                        Tuple<string, int> endpoint = GetEndpointFromUserMetadata(hybridConnectionResource.Data.UserMetadata);
                        stringWriter.WriteLine(String.Format("[{0}]   {1}   {2}   {3}  {4}:{5}", index, hybridConnectionResource.Data.Name, hybridConnectionResource.Data.Location, relayNamespaceResource.Data.Name, endpoint.Item1, endpoint.Item2));
                        index++;
                        hybridConnectionResourcesList.Add(hybridConnectionResource);
                    }
                }
            }

            if (hybridConnectionResourcesList.Count == 0)
            {
                Console.WriteLine("No Hybrid Connection resources exist for this subscription. Exiting..");
                return false;
            }

            string result = stringWriter.ToString();
            Console.WriteLine(stringWriter);

            Console.Write("\nSelect a Hybrid Connection [Id]: ");
            var hybridConnectionIndex = Console.ReadLine();
            Console.WriteLine();

            if (!int.TryParse(hybridConnectionIndex, out int hcIndex) || hcIndex < 0 || hcIndex >= hybridConnectionResourcesList.Count)
            {
                Console.WriteLine("Invalid choice.");
                return false;
            }

            var chosenHybridConnection = hybridConnectionResourcesList[hcIndex];
            var hybridConnectionId = chosenHybridConnection.Data.Id;

            connectionString = relayArmClient.GetHybridConnectionPrimaryConnectionString(hybridConnectionId);

            return true;
        }


        // TODO: below is duplicated helper code
        [DataContract]
        private class KeyValuePair
        {
            [DataMember]
            public string key { get; set; }

            [DataMember]
            public string value { get; set; }
        }

        public static string GetEndpointStringFromUserMetadata(string metadata)
        {
            try
            {
                var stream = new MemoryStream(Encoding.Unicode.GetBytes(metadata.ToLower()));

                var serializer = new DataContractJsonSerializer(typeof(List<KeyValuePair>),
                    new DataContractJsonSerializerSettings() { });
                List<KeyValuePair> keyValuePairs = (List<KeyValuePair>)serializer.ReadObject(stream);

                foreach (var pair in keyValuePairs)
                {
                    if (string.Equals(pair.key, "endpoint", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return pair.value;
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: log
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        public static Tuple<string, int> GetEndpointFromUserMetadata(string metadata)
        {
            try
            {
                string endpoint = GetEndpointStringFromUserMetadata(metadata);

                if (endpoint == null)
                {
                    return null;
                }

                string[] parts = endpoint.Split(':');
                if (parts.Length != 2)
                {
                    return null;
                }

                var result = new Tuple<string, int>(parts[0], int.Parse(parts[1]));
                return result;
            }
            catch (Exception e)
            {
                // TODO: log?
                Console.WriteLine(e.ToString());
            }

            return null;
        }
    }
}
