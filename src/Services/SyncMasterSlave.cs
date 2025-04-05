using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Helpers;

namespace codecrafters_redis.Services;

public class SyncMasterSlave
{
    public void SlaveSyncWithMaster()
    {
        var masterRedisNodeHost = Config.MasterRedisNode.Split(" ")[0];
        var masterRedisNodePort = int.Parse(Config.MasterRedisNode.Split(" ")[1]);

        if (masterRedisNodeHost.Length == 0 || masterRedisNodePort == 0)
        {
            Console.WriteLine("Master redis node host or port is empty");
            return;
        }
        
        try
        {
            using (var client = new TcpClient(masterRedisNodeHost, masterRedisNodePort))
            {
                using (NetworkStream stream = client.GetStream())
                {
                    string pingMessage = "1\r\n$4\r\nPING\r\n";
                    byte[] data = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', pingMessage));
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine("Ping message sent");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during sync with master redis node : {e.Message}");
        }
    }
}