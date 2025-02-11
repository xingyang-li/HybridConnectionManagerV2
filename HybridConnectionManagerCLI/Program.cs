using HybridConnectionManager.Client;

public class Program
{
    public static void Main(string[] args)
    {
        HybridConnectionManagerClient client = new HybridConnectionManagerClient();
        var response = client.AddUsingConnectionString("Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;Shared");
        Console.WriteLine(response.Result);
    }
}