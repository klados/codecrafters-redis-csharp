using System.Net.Sockets;

namespace codecrafters_redis.Models;

public class ConnectedSlavesData
{
    public TcpClient TcpClient { get; set; }
    public int DeriveBytesServed { get; set; }
    public int CommandsServed { get; set; }
}