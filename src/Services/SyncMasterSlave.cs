using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Services;

public class SyncMasterSlave
{
    public string REPLCONFCommand(TcpClient client, params string[] arguments)
    {
        try
        {
            SyncHelper.SlaveConnected(client);
            return BuildResponse.Generate('+', "OK");
        }
        catch (Exception e)
        {
            return BuildResponse.Generate('+', "OK");
        }
    }
    
    public string MasterPsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        
        var res = BuildResponse.Generate('+', $"FULLRESYNC 091465c549348f7cf6f0c7792e33e7e1fbb5ae74 0");
        stream.Write(Encoding.ASCII.GetBytes(res), 0, res?.Length ?? 0);

        byte[] rdbFileBytes = Convert.FromBase64String(
            "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==");
        string rdbHeader = $"${rdbFileBytes.Length}\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(rdbHeader);
        stream.Write(headerBytes);
        stream.Write(rdbFileBytes);
        return "";
    }

    public void SlaveSyncWithMaster(ServiceProvider serviceProvider)
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
            var client = new TcpClient(masterRedisNodeHost, masterRedisNodePort);
            NetworkStream stream = client.GetStream();

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
            Console.WriteLine($"##master replyconf2 response: {responseData}");

            string psync = "3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";
            byte[] psyncData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', psync));
            stream.Write(psyncData, 0, psyncData.Length);

            bytesRead = stream.Read(receivedData, 0, receivedData.Length);
            responseData = System.Text.Encoding.ASCII.GetString(receivedData, 0, bytesRead);
            Console.WriteLine($"master replyconf2 response: {responseData}");

            var rdbFile = stream.Read(receivedData, 0, receivedData.Length);
            responseData = System.Text.Encoding.ASCII.GetString(receivedData, 0, rdbFile);
            Console.WriteLine($"##rdb file received: {responseData}");

            if (responseData[0] == '*')
            {
                Console.WriteLine($"responseData: {responseData}");
                foreach (var command in responseData.Split("*")[1..])
                {
                    Console.WriteLine($"## command to insert: {command}");
                    CommandService.ParseCommand(serviceProvider, client, $"*{command}".Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
                }
            }

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    var propagatedCommandsBytes = stream.Read(receivedData, 0, receivedData.Length);
                    if (propagatedCommandsBytes == 0) break;

                    var propagatedCommands = System.Text.Encoding.ASCII.GetString(receivedData, 0, propagatedCommandsBytes);
                    foreach (var command in propagatedCommands.Split("*")[1..])
                    {
                        CommandService.ParseCommand(serviceProvider, client, $"*{command}".Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during sync with master redis node : {e.Message}");
        }
    }
}