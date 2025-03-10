using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
var socket = server.AcceptSocket(); // wait for client

var buffer = new byte[8];
var bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);

if (bytesReceived > 0)
{
    await socket.SendAsync(Encoding.UTF8.GetBytes("+PONG\r\n"), SocketFlags.None);    
}
