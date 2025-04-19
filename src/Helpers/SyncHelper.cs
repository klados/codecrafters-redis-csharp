using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Models;

namespace codecrafters_redis.Helpers;

public static class SyncHelper
{
    // private static readonly HashSet<TcpClient> SlavesStream = [];
    private static readonly ConcurrentDictionary<string, ConnectedSlavesData> SlavesStream = new();
    // private static bool _waitRunning = false;
    public static bool FinishFullSync = false;
    
    // public static bool IsWaitRunning()
    // {
    //     return _waitRunning;
    // }

    public static  ConcurrentDictionary<string, ConnectedSlavesData> GetConnectedSlaves()
    {
        return SlavesStream;
    }
    
    // public static void StartWaitRunning()
    // {
    //     _waitRunning = true;
    // }
    
    public static void SlaveConnected(TcpClient client)
    {
        Console.WriteLine("slave connected");
        SlavesStream.TryAdd(client.Client.RemoteEndPoint.ToString(), new ConnectedSlavesData(){TcpClient = client});
    }

    public static void SyncCommand(string command)
    {
        var respArray = ParseString.ParseArray(command.TrimEnd().Split(' '));
        Console.WriteLine($"##MASTER sync command: {command}, count {SlavesStream.Count}");
        foreach (var slave in SlavesStream)
        {
            try
            {
                NetworkStream stream = slave.Value.TcpClient.GetStream();
                stream.Write(Encoding.ASCII.GetBytes(BuildResponse.Generate('*', respArray)));
                SlavesStream[slave.Key].CommandsServed = slave.Value.CommandsServed + 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed to sync with slave: {e.Message}");
                SlavesStream.Remove(slave.Key, out _);
            }
        }
    }
}