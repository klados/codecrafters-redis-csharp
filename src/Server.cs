using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
var socket = server.AcceptSocket(); // wait for client

while (true)
{
    var buffer = new byte[200];
    var bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);

    if (bytesReceived > 0)
    {
        await socket.SendAsync(Encoding.UTF8.GetBytes("+PONG\r\n"), SocketFlags.None);    
    }
}
