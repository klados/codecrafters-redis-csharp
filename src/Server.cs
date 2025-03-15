using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories;
using codecrafters_redis.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;


var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<Config>();
serviceCollection.AddSingleton<Set>();
serviceCollection.AddSingleton<Get>();
serviceCollection.AddScoped<IStoreRepository, StoreRepository>();
var serviceProvider = serviceCollection.BuildServiceProvider();

serviceProvider.GetRequiredKeyedService<Config>(null).ParseCommandLineArgs(args);

TcpListener server = new TcpListener(IPAddress.Any, 6379);
Console.WriteLine("Server started on port 6379");
server.Start();

try
{
    while (true)
    {
        var socket = server.AcceptSocket(); // wait for client
        Console.WriteLine("Client connected");
        Task.Run(() => HandleTask(socket));
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


async Task HandleTask(Socket socket)
{
    try
    {
        while (socket.Connected)
        {
            var buffer = new byte[1024];
            var bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);

            if (bytesReceived == 0)
            {
                break;
            }

            var response = ParseResp(buffer);
            await socket.SendAsync(Encoding.ASCII.GetBytes(response), SocketFlags.None);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"An error occurred: {e.Message}");
    }
    finally
    {
        socket.Close();
        Console.WriteLine("Client connection closed");
    }
}

string ParseResp(byte[] bytes)
{
    var arrayStrings = Encoding.UTF8.GetString(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    var command = arrayStrings[2];
    var argumentForCommand = arrayStrings.ElementAtOrDefault(4);
    var ttl = arrayStrings.ElementAtOrDefault(8)?.ToUpper() == "PX" ? arrayStrings.ElementAtOrDefault(10) : null;
    
    return command.ToUpper() switch
    {
        "PING" => BuildResponse.Generate('+', "PONG"),
        "ECHO" => BuildResponse.Generate(argumentForCommand != null ? '$' : '-',
            argumentForCommand ?? "wrong number of arguments for 'echo' command"),
        "SET" => serviceProvider.GetRequiredKeyedService<Set>(null).SetCommand(argumentForCommand, arrayStrings.ElementAtOrDefault(6), ttl),
        "GET" => serviceProvider.GetRequiredKeyedService<Get>(null).GetCommand(argumentForCommand),
        "CONFIG" => serviceProvider.GetRequiredKeyedService<Config>(null).ConfigCmd(argumentForCommand??"",arrayStrings.ElementAtOrDefault(6)??""),
        _ => BuildResponse.Generate('+', "UNKNOWN")
    };
}