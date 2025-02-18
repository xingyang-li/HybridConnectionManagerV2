using HybridConnectionManager.Client;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Relay;
using Newtonsoft.Json;
using System.CommandLine;
using System.Text.RegularExpressions;
using HcManProto;
using Azure.ResourceManager.Resources;

public class Program
{
    public static JsonSerializerSettings HybridConnectionJsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new IgnorePropertiesResolver(new[] { "Error", "ErrorMessage" }) };
    public static string EndpointRegexString = "^([a-zA-Z0-9.-]+):(\\d{1,5})$";

    public static async Task<int> Main(string[] args)
    {
        // Options
        var requiredSubscriptionOption = new Option<string>(new[] { "--subscription", "-s" })
        {
            IsRequired = true,
            Description = "Hybrid Connection Subscription",
        };
        var requiredResourceGroupOption = new Option<string>(new[] { "--resource-group", "-rg" })
        {
            IsRequired = true,
            Description = "Hybrid Connection Resource Group",
        };
        var requiredNamespaceOption = new Option<string>(new[] { "--namespace", "-ns" })
        {
            IsRequired = true,
            Description = "Hybrid Connection Namespace",
        };
        var requiredNameOption = new Option<string>(new[] { "--name", "-n" })
        {
            IsRequired = true,
            Description = "Hybrid Connection Name",
        };

        var SubscriptionOption = new Option<string>(new[] { "--subscription", "-s" })
        {
            Description = "Hybrid Connection Subscription",
        };
        var ResourceGroupOption = new Option<string>(new[] { "--resource-group", "-rg" })
        {
            Description = "Hybrid Connection Resource Group",
        };
        var NamespaceOption = new Option<string>(new[] { "--namespace", "-ns" })
        {
            Description = "Hybrid Connection Namespace",
        };
        var NameOption = new Option<string>(new[] { "--name", "-n" })
        {
            Description = "Hybrid Connection Name",
        };

        // Arguments
        var connectionStringArg = new Argument<string>("connection-string", "Hybrid Connection Connection String")
        {
            Arity = ArgumentArity.ZeroOrOne,
        };

        var endpointStringArg = new Argument<string>("endpoint", "host:port")
        {
            Arity = ArgumentArity.ExactlyOne,
        };

        // Commands + Handlers
        var login = new Command("login", "Login to Azure");
        login.SetHandler(() => LoginHandler());

        var add  = new Command("add", "Add a new Hybrid Connection")
        {
            connectionStringArg,
            SubscriptionOption,
            ResourceGroupOption,
            NamespaceOption,
            NameOption,
        };

        add.SetHandler((string connectionString, string subscription, string resourceGroup, string @namespace, string name) => AddHandler(connectionString, subscription, resourceGroup, @namespace, name), 
            connectionStringArg, SubscriptionOption, ResourceGroupOption, NamespaceOption, NameOption);

        var remove = new Command("remove", "Remove a Hybrid Connection from the local machine")
        {
            requiredNamespaceOption,
            requiredNameOption
        };
        remove.SetHandler((string @namespace, string name) => RemoveHandler(@namespace, name), requiredNamespaceOption, requiredNameOption);

        var show = new Command("show", "Show a Hybrid Connection")
        {
            requiredNamespaceOption,
            requiredNameOption
        };
        show.SetHandler((string @namespace, string name) => ShowHandler(@namespace, name), requiredNamespaceOption, requiredNameOption);

        var list = new Command("list", "List all Hybrid Connections");
        list.SetHandler(() => ListHandler());

        var test = new Command("test", "Test the endpoint for a given Hybrid Connection")
        {
            endpointStringArg
        };
        test.SetHandler((string endpoint) => TestHandler(endpoint), endpointStringArg);

        // Root
        var rootCommand = new RootCommand("Hybrid Connection Manager V2 CLI")
        {
            login,
            add,
            remove,
            show,
            list,
            test
        };

        return await rootCommand.InvokeAsync(args);
    }

    public static async Task LoginHandler()
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.AuthenticateUser();
        Console.WriteLine(response);
    }

    public static async Task AddHandler(string connectionString, string subscription, string resourceGroup, string @namespace, string name)
    {
        bool useConnectionString = true;
        bool interactiveMode = false;

        if (string.IsNullOrEmpty(connectionString))
        {
            if (string.IsNullOrEmpty(subscription) && string.IsNullOrEmpty(resourceGroup) && string.IsNullOrEmpty(@namespace) && string.IsNullOrEmpty(name))
            {
                interactiveMode = true;
            }
            else if (string.IsNullOrEmpty(subscription) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(name)){
                Console.WriteLine("Must specify either <connection-string> or ALL of { --subscription, --resource-group, --namespace, --name }");
                return;
            }
            useConnectionString = false;
        }
        else
        {
            if (!string.IsNullOrEmpty(subscription) || !string.IsNullOrEmpty(resourceGroup) || !string.IsNullOrEmpty(@namespace) || !string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Must specify either <connection-string> or ALL of { --subscription, --resource-group, --namespace, --name }");
                return;
            }
        }

        HybridConnectionManagerClient client = new HybridConnectionManagerClient();

        if (interactiveMode)
        {
            Console.WriteLine("\n---Interactive Mode---");
            Console.WriteLine("\nLogging in to Azure..\n");
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

            ArmClient armClient = new ArmClient(credential);
            var subscriptionsResourceCollection = armClient.GetSubscriptions();
            int index = 0;
            List<SubscriptionResource> subscriptionResourcesList = new List<SubscriptionResource>();
            foreach (var subscriptionResource in subscriptionsResourceCollection)
            {
                Console.WriteLine(String.Format("[{0}] {1} ({2})", index, subscriptionResource.Data.DisplayName, subscriptionResource.Data.SubscriptionId));
                index++;
                subscriptionResourcesList.Add(subscriptionResource);
            }
            Console.Write("\nSelect a subscription [Id]: ");
            var subscriptionIndex = Console.ReadLine();
            Console.WriteLine();

            if (!int.TryParse(subscriptionIndex, out int subIndex) || subIndex < 0 || subIndex >= subscriptionResourcesList.Count)
            {
                Console.WriteLine("Invalid choice.");
                return;
            }

            var chosenSub = subscriptionResourcesList[subIndex];
            subscription = chosenSub.Data.DisplayName;
            var resourceGroupsCollection = chosenSub.GetResourceGroups();
            index = 0;
            List<RelayHybridConnectionResource> hybridConnectionResourcesList = new List<RelayHybridConnectionResource>();
            foreach (var resourceGroupResource in resourceGroupsCollection)
            {
                var relayNamespacesCollection = resourceGroupResource.GetRelayNamespaces();
                foreach (var relayNamespaceResource in relayNamespacesCollection)
                {
                    var hybridConnectionsCollection = relayNamespaceResource.GetRelayHybridConnections();
                    foreach (var hybridConnectionResource in hybridConnectionsCollection)
                    {
                        Console.WriteLine(String.Format("[{0}] {1} {2} {3}", index, hybridConnectionResource.Data.Name, hybridConnectionResource.Data.Location, relayNamespaceResource.Data.Name, hybridConnectionResource.Data.UserMetadata.ToString()));
                        index++;
                        hybridConnectionResourcesList.Add(hybridConnectionResource);
                    }
                }
            }

            Console.Write("\nSelect a Hybrid Connection [Id]: ");
            var hybridConnectionIndex = Console.ReadLine();
            Console.WriteLine();

            if (!int.TryParse(hybridConnectionIndex, out int hcIndex) || hcIndex < 0 || hcIndex >= hybridConnectionResourcesList.Count)
            {
                Console.WriteLine("Invalid choice.");
                return;
            }

            var chosenHybridConnection = hybridConnectionResourcesList[hcIndex];
            return;

            // TODO: set either connection string or sub+rg+namespace+name params for gRPC api
        }

        var response = new HybridConnectionInformationResponse();
        if (useConnectionString)
        {
            response = await client.AddUsingConnectionString(connectionString);
        }
        else
        {
            response = await client.AddUsingParameters(subscription, resourceGroup, @namespace, name);
        }

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

    public static async Task RemoveHandler(string @namespace, string name)
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.RemoveConnection(@namespace, name);
        Console.WriteLine(response);
    }

    public static async Task ShowHandler(string @namespace, string name)
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.ShowConnection(@namespace, name);

        if (response.Error)
        {
            Console.WriteLine(response.ErrorMessage);
        }
        else {
            var responseString = JsonConvert.SerializeObject(response, Formatting.Indented, HybridConnectionJsonSettings);
            Console.WriteLine(responseString);
        }
    }

    public static async Task ListHandler()
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.ListConnections();
        var responseString = JsonConvert.SerializeObject(response, Formatting.Indented, HybridConnectionJsonSettings);
        Console.WriteLine(responseString);
    }

    public static async Task TestHandler(string endpoint)
    {
        if (!Regex.IsMatch(endpoint, EndpointRegexString))
        {
            Console.WriteLine("Invalid endpoint format. Expected format: <host>:<port>");
            return;
        }

        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.TestEndpointForConnection(endpoint);
        Console.WriteLine(response);
    }
}