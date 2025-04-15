using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Helpers;

public static class SyncHelper
{
    private static readonly HashSet<TcpClient> SlavesStream = [];

    public static int GetSlavesCount()
    {
        return SlavesStream.Count;
    }
    
    public static void SlaveConnected(TcpClient client)
    {
        SlavesStream.Add(client);
    }

    public static void SyncCommand(string command)
    {
        var respArray = ParseString.ParseArray(command.TrimEnd().Split(' '));
        foreach (var slave in SlavesStream)
        {
            try
            {
                NetworkStream stream = slave.GetStream();
                Console.WriteLine($"##MASTER sync command: {command}");
                stream.Write(Encoding.ASCII.GetBytes(BuildResponse.Generate('*', respArray)));
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed to sync with slave: { e.Message }");
                SlavesStream.Remove(slave);
            }
        }
    }
}