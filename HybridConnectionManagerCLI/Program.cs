﻿using HybridConnectionManager.Client;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Newtonsoft.Json;
using System.CommandLine;

public class Program
{
    public static JsonSerializerSettings HybridConnectionJsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new IgnorePropertiesResolver(new[] { "Error", "ErrorMessage" }) };
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

        var show = new Command("show", "Show a Hybrid Connection")
        {
            requiredNamespaceOption,
            requiredNameOption
        };
        show.SetHandler((string @namespace, string name) => ShowHandler(@namespace, name), requiredNamespaceOption, requiredNameOption);

        // Root
        var rootCommand = new RootCommand("Hybrid Connection Manager V2 CLI")
        {
            login,
            add,
            remove,
            show
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
}