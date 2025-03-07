using System.CommandLine;

namespace HybridConnectionManager.CLI
{
    public class Program
    {
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
            login.SetHandler(() => CommandHandlers.LoginHandler());

            var logs = new Command("logs", "View local logs for Hybrid Connection Manager Service");
            logs.SetHandler(() => CommandHandlers.LogsHandler());

            var add = new Command("add", "Add a new Hybrid Connection")
            {
                connectionStringArg,
                SubscriptionOption,
                ResourceGroupOption,
                NamespaceOption,
                NameOption,
            };

            add.SetHandler((string connectionString, string subscription, string resourceGroup, string @namespace, string name) => CommandHandlers.AddHandler(connectionString, subscription, resourceGroup, @namespace, name),
                connectionStringArg, SubscriptionOption, ResourceGroupOption, NamespaceOption, NameOption);

            var remove = new Command("remove", "Remove a Hybrid Connection from the local machine")
            {
                requiredNamespaceOption,
                requiredNameOption
            };
            remove.SetHandler((string @namespace, string name) => CommandHandlers.RemoveHandler(@namespace, name), requiredNamespaceOption, requiredNameOption);

            var show = new Command("show", "Show a Hybrid Connection")
            {
                requiredNamespaceOption,
                requiredNameOption
            };
            show.SetHandler((string @namespace, string name) => CommandHandlers.ShowHandler(@namespace, name), requiredNamespaceOption, requiredNameOption);

            var list = new Command("list", "List all Hybrid Connections");
            list.SetHandler(() => CommandHandlers.ListHandler());

            var test = new Command("test", "Test the endpoint for a given Hybrid Connection")
            {
                endpointStringArg
            };
            test.SetHandler((string endpoint) => CommandHandlers.TestHandler(endpoint), endpointStringArg);

            // Root
            var rootCommand = new RootCommand("Hybrid Connection Manager V2 CLI")
            {
                login,
                add,
                remove,
                show,
                list,
                test,
                logs
            };

            return await rootCommand.InvokeAsync(args);
        }
    }
}