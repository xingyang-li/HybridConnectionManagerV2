using HybridConnectionManager.Client;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using System.CommandLine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Options
        var requiredNamespaceOption = new Option<string>(new[] { "--namespace", "-ns" })
        {
            IsRequired = true,
            Description = "Namespace",
        };
        var requiredNameOption = new Option<string>(new[] { "--name", "-n" })
        {
            IsRequired = true,
            Description = "Name",
        };

        // Arguments
        var connectionStringArg = new Argument<string>("connection-string", "Hybrid Connection Connection String")
        {
            Arity = ArgumentArity.ExactlyOne,
        };

        // Commands + Handlers
        var login = new Command("login", "Login to Azure");
        login.SetHandler(() => LoginHandler());

        var add  = new Command("add", "Add a new Hybrid Connection")
        {
            connectionStringArg
        };
        add.SetHandler((string connectionString) => AddHandler(connectionString), connectionStringArg);

        var remove = new Command("remove", "Remove a Hybrid Connection from the local machine")
        {
            requiredNamespaceOption,
            requiredNameOption
        };
        remove.SetHandler((string @namespace, string name) => RemoveHandler(@namespace, name), requiredNamespaceOption, requiredNameOption);

        // Root
        var rootCommand = new RootCommand("Hybrid Connection Manager V2 CLI")
        {
            login,
            add,
            remove
        };

        return await rootCommand.InvokeAsync(args);
    }

    public static async Task LoginHandler()
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.AuthenticateUser();
        Console.WriteLine(response);
    }

    public static async Task AddHandler(string connectionString)
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.AddUsingConnectionString(connectionString);
        Console.WriteLine(response);
    }

    public static async Task RemoveHandler(string @namespace, string name)
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.RemoveConnection(@namespace, name);
        Console.WriteLine(response);
    }
}