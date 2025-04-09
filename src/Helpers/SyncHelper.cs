using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Helpers;

public static class SyncHelper
{
    private static readonly List<Stream> _slavesStream = new();

    public static void SlaveConnected(NetworkStream stream)
    {
        Console.WriteLine("slave connected");
        _slavesStream.Add(stream);
    }

    public static void SyncCommand(string command)
    {
        foreach (var stream in _slavesStream)
        {
            var respArray = ParseString.ParseArray(command.TrimEnd().Split(' '));
            stream.Write(Encoding.ASCII.GetBytes(BuildResponse.Generate('*', respArray)));
        }
    }
}