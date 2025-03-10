using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
while (true)
{
    Console.WriteLine("Client connected");
    var socket = server.AcceptSocket(); // wait for client
    Task.Run(() => handleTask(socket));
}


static async Task handleTask(Socket socket)
{
    try
    {
        while (true)
        {
            var buffer = new byte[1024];
            var bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);

            if (bytesReceived == 0)
            {
                break;
            }

            byte[] pongResponse = Encoding.ASCII.GetBytes("+PONG\r\n");

            await socket.SendAsync(pongResponse, SocketFlags.None);

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
