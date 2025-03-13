using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Models;

Dictionary<string, DataWithTtl> storedData = new Dictionary<string, DataWithTtl>();

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
while (true)
{
    var socket = server.AcceptSocket(); // wait for client
    Console.WriteLine("Client connected");
    Task.Run(() => HandleTask(socket));
}


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


string BuildResponse(char dataType, string response)
{
    return (dataType) switch
    {
        '+' => $"+{response}\r\n",
        '$' => $"{dataType}{response.Length}\r\n{response}\r\n",
        '-' => $"{dataType}Err {response}\r\n",
        _ => throw new NotImplementedException("Unknown response type"),
    };
}

string StoreToDictionary(string? key, string? data, string? ttl)
{
    if (key == null || data == null)
    {
        return BuildResponse('-', "wrong number of arguments for 'set' command");
    }
    
    storedData.Add(key, new DataWithTtl()
    {
        strValue = data,
        ExpiredAt = ttl == null ? null : DateTime.Now.AddMilliseconds(double.Parse(ttl))
    });
    return BuildResponse('+', "OK");
}

string RetrieveFromDictionary(string? key)
{
    if (key == null)
    {
        return BuildResponse('-', "wrong number of arguments for 'get' command");
    }

    if (!storedData.TryGetValue(key, out var data)) return $"$-1\r\n"; // null bulk string
    if (!data.ExpiredAt.HasValue) return BuildResponse('$', data.strValue);
    if (DateTime.Now > data.ExpiredAt)
    {
        storedData.Remove(key);
        return $"$-1\r\n";
    }

    return BuildResponse('$', data.strValue);
}

string ParseResp(byte[] bytes)
{
    var arrayStrings = Encoding.UTF8.GetString(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    var command = arrayStrings[2];
    var argumentForCommand = arrayStrings.ElementAtOrDefault(4);
    var ttl = arrayStrings.ElementAtOrDefault(8)?.ToUpper() == "PX" ? arrayStrings.ElementAtOrDefault(10) : null;
    
    return command.ToUpper() switch
    {
        "PING" => BuildResponse('+', "PONG"),
        "ECHO" => BuildResponse(argumentForCommand != null ? '$' : '-',
            argumentForCommand ?? "wrong number of arguments for 'echo' command"),
        "SET" => StoreToDictionary(argumentForCommand, arrayStrings.ElementAtOrDefault(6), ttl),
        "GET" => RetrieveFromDictionary(argumentForCommand),
        _ => BuildResponse('+', "UNKNOWN")
    };
}