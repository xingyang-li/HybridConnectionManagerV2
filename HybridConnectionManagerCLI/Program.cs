using HybridConnectionManager.Client;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using System.CommandLine;
using System.Net.Http;
using System.Net.Http.Headers;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var login = new Command("login", "Login to Azure");

        login.SetHandler(() => LoginHandler());

        var rootCommand = new RootCommand("Hybrid Connection Manager V2 CLI")
        {
            login
        };

        return await rootCommand.InvokeAsync(args);
    }

    public static async Task LoginHandler()
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = await client.AuthenticateUser();
        Console.WriteLine(response);
    }
}