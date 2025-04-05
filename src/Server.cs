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
serviceCollection.AddScoped<Transactions>();
serviceCollection.AddScoped<IStoreRepository, StoreRepository>();
serviceCollection.AddScoped<IStreamRepository, StreamRepository>();
serviceCollection.AddScoped<ITransactionRepository, TransactionRepository>();
serviceCollection.AddScoped<RdbService>();
var serviceProvider = serviceCollection.BuildServiceProvider();

serviceProvider.GetRequiredKeyedService<Config>(null).ParseCommandLineArgs(args);
serviceProvider.GetRequiredKeyedService<RdbService>(null).LoadDataFromFile();

TcpListener server = new TcpListener(IPAddress.Any, Config.Port);
Console.WriteLine($"Server started on port {Config.Port}");
server.Start();

try
{
    while (true)
    {
        // var socket = server.AcceptSocket(); // wait for client
        TcpClient client = server.AcceptTcpClient();
        Console.WriteLine("Client connected");
        
        NetworkStream stream = client.GetStream();
        Task.Run(() => HandleTask(stream));
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


async Task HandleTask(NetworkStream stream)
{
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

            var response = ParseResp(buffer, stream);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"An error occurred: {e.Message}");
    }
    finally
    {
        stream.Close();
        Console.WriteLine("Client connection closed");
    }
}

string ParseResp(byte[] bytes, NetworkStream stream)
{
    var arrayStrings = Encoding.UTF8.GetString(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    return CommandService.ParseCommand(serviceProvider, stream, arrayStrings);
}