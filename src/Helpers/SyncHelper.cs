using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Helpers;

public static class SyncHelper
{
    private static readonly HashSet<TcpClient> SlavesStream = [];
    private static int _waitAckCounter = 0;
    private static bool _waitRunning = false;
    private static readonly object _lockObject = new object();

    public static void StartWaitRunning()
    {
        _waitRunning = true;
    }
    
    public static void WaitAckCounterIncr()
    {
        lock (_lockObject)
        {
            if (_waitRunning)
            {
                _waitAckCounter++;
            }
        }
    }

    public static int GetWaitAckCounter()
    {
        lock (_lockObject)
        {
            return _waitAckCounter;
        }
    }

    public static void AckCounterInitToZero()
    {
        lock (_lockObject)
        {
            _waitRunning = false;
            _waitAckCounter = 0;
        }
    }
    
    public static int GetSlavesCount()
    {
        return SlavesStream.Count;
    }
    
    public static void SlaveConnected(TcpClient client)
    {
        Console.WriteLine("slave connected");
        SlavesStream.Add(client);
    }

    public static void SyncCommand(string command)
    {
        var respArray = ParseString.ParseArray(command.TrimEnd().Split(' '));
        Console.WriteLine($"##MASTER sync command: {command}, count {SlavesStream.Count}");
        foreach (var slave in SlavesStream)
        {
            try
            {
                NetworkStream stream = slave.GetStream();
                stream.Write(Encoding.ASCII.GetBytes(BuildResponse.Generate('*', respArray)));
                var replconf = $"3\r\n$8\r\nREPLCONF\r\n$6\r\nGETACK\r\n$1\r\n*\r\n";
                stream.Write(Encoding.ASCII.GetBytes(BuildResponse.Generate('*', replconf)));
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed to sync with slave: { e.Message }");
                SlavesStream.Remove(slave);
            }
        }
    }
}