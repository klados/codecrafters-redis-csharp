using System.Net;
using System.Net.Sockets;
using System.Text;

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

            var response = ParseRESP(buffer);
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
        _ => throw new NotImplementedException("Unknown response type"),
    };
}

string ParseRESP(byte[] bytes)
{
   var arrayStrings = Encoding.UTF8.GetString(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
   var command = arrayStrings[2];

   return command.ToUpper() switch
   {
       "PING" => BuildResponse('+', "PONG"),
       "ECHO" => BuildResponse('$',  arrayStrings[4]),
       _ => BuildResponse('+', "UNKNOWN")
   };
}