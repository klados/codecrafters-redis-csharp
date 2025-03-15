using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Helpers;
using codecrafters_redis.Models;
using Microsoft.Extensions.DependencyInjection;

Dictionary<string, DataWithTtl> storedData = new Dictionary<string, DataWithTtl>();

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<Config>();
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


string StoreToDictionary(string? key, string? data, string? ttl)
{
    if (key == null || data == null)
    {
        return BuildResponse.Generate('-', "wrong number of arguments for 'set' command");
    }
    
    storedData.Add(key, new DataWithTtl()
    {
        strValue = data,
        ExpiredAt = ttl == null ? null : DateTime.Now.AddMilliseconds(double.Parse(ttl))
    });
    return BuildResponse.Generate('+', "OK");
}

string RetrieveFromDictionary(string? key)
{
    if (key == null)
    {
        return BuildResponse.Generate('-', "wrong number of arguments for 'get' command");
    }

    if (!storedData.TryGetValue(key, out var data)) return $"$-1\r\n"; // null bulk string
    if (!data.ExpiredAt.HasValue) return BuildResponse.Generate('$', data.strValue);
    if (DateTime.Now > data.ExpiredAt)
    {
        storedData.Remove(key);
        return $"$-1\r\n";
    }

    return BuildResponse.Generate('$', data.strValue);
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
        "SET" => StoreToDictionary(argumentForCommand, arrayStrings.ElementAtOrDefault(6), ttl),
        "GET" => RetrieveFromDictionary(argumentForCommand),
        "CONFIG" => serviceProvider.GetRequiredKeyedService<Config>(null).ConfigCmd(argumentForCommand??"",arrayStrings.ElementAtOrDefault(6)??""),
        _ => BuildResponse.Generate('+', "UNKNOWN")
    };
}