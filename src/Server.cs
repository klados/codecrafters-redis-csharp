using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Repositories;
using codecrafters_redis.Repositories.Interfaces;
using codecrafters_redis.Services;
using Microsoft.Extensions.DependencyInjection;


var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<Config>();
serviceCollection.AddSingleton<Set>();
serviceCollection.AddSingleton<Incr>();
serviceCollection.AddSingleton<Get>();
serviceCollection.AddSingleton<Keys>();
serviceCollection.AddSingleton<RedisType>();
serviceCollection.AddSingleton<RedisStream>();
serviceCollection.AddSingleton<SyncMasterSlave>();
serviceCollection.AddSingleton<Info>();
serviceCollection.AddScoped<Transactions>();
serviceCollection.AddScoped<IStoreRepository, StoreRepository>();
serviceCollection.AddScoped<IStreamRepository, StreamRepository>();
serviceCollection.AddScoped<ITransactionRepository, TransactionRepository>();
serviceCollection.AddScoped<RdbService>();
serviceCollection.AddSingleton<Ping>();
var serviceProvider = serviceCollection.BuildServiceProvider();

serviceProvider.GetRequiredKeyedService<Config>(null).ParseCommandLineArgs(args);
serviceProvider.GetRequiredKeyedService<RdbService>(null).LoadDataFromFile();

Task.Run(async () =>
{
    if (Config.IsReplicaOf)
    {
        serviceProvider.GetRequiredKeyedService<SyncMasterSlave>(null).SlaveSyncWithMaster(serviceProvider);
    }
});


TcpListener server = new TcpListener(IPAddress.Any, Config.Port);
Console.WriteLine($"Server started on port {Config.Port}");
server.Start();

try
{
    while (true)
    {
        // wait for client
        TcpClient client = server.AcceptTcpClient();
        Task.Run(() => HandleTask(client));
    }
}
catch (Exception e)
{
    Console.WriteLine($"Server error: {e.Message}");
}
finally
{
    server.Stop();
}

return;


async Task HandleTask(TcpClient client)
{
    Console.WriteLine($"client ip: {client.Client.RemoteEndPoint}");
    NetworkStream stream = client.GetStream();
    try
    {
        while (true)
        {
            var buffer = new byte[1024];
            var bytesReceived = await stream.ReadAsync(buffer);

            if (bytesReceived == 0)
            {
                break;
            }

            var response = ParseResp(buffer, client);
            Console.WriteLine($"response for the client: {response}, len: {response.Length}");
            if (response.Length == 0)
            {
                Console.WriteLine($"zero response");
                continue; // response = "+ON\r\n";
            }
            await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"An error occurred: {e}");
    }
    finally
    {
        Console.WriteLine("Client connection closed");
    }
}

string ParseResp(byte[] bytes, TcpClient client)
{
    var arrayStrings = Encoding.UTF8.GetString(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    return CommandService.ParseCommand(serviceProvider, client, arrayStrings);
}