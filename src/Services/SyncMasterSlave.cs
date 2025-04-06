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
                    var receivedData = new Byte[256];
                    
                    string pingMessage = "1\r\n$4\r\nPING\r\n";
                    byte[] pingData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', pingMessage));
                    stream.Write(pingData, 0, pingData.Length);
                    Console.WriteLine("Ping message sent");
                    
                    var bytesRead = stream.Read(receivedData, 0, receivedData.Length);
                    var responseData = System.Text.Encoding.ASCII.GetString(receivedData, 0, bytesRead);
                    Console.WriteLine($"master ping response: {responseData}");
                    
                    string listeningPort = $"3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n$4\r\n{Config.Port}\r\n";
                    byte[] listeningPortData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', listeningPort));
                    stream.Write(listeningPortData, 0, listeningPortData.Length);

                    bytesRead = stream.Read(receivedData, 0, receivedData.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(receivedData, 0, bytesRead);
                    Console.WriteLine($"master replyconf response: {responseData}");

                    string capa = "3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";
                    byte[] capaData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', capa));
                    stream.Write(capaData, 0, capaData.Length);
                    
                    bytesRead = stream.Read(receivedData, 0, receivedData.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(receivedData, 0, bytesRead);
                    Console.WriteLine($"master replyconf2 response: {responseData}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during sync with master redis node : {e.Message}");
        }
    }
}