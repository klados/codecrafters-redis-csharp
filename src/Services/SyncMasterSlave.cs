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
        if (arguments[0].ToUpper().Equals("GETACK", StringComparison.CurrentCultureIgnoreCase))
        {
            string res;
            var counterValue = Config.GetCounter();
            res =
                $"3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${counterValue.ToString().Length}\r\n{counterValue}\r\n";
            var tmp = 35 + counterValue.ToString().Length + counterValue.ToString().Length.ToString().Length;
            Console.WriteLine($"GETACK 2 !!!: {counterValue} ({tmp + counterValue})");
            Config.IncrementCounter(tmp);
            
            return BuildResponse.Generate('*', res);
        }

        try
        {
            SyncHelper.SlaveConnected(client);
            SyncHelper.WaitAckCounterIncr();
            Console.WriteLine($"========== increase! {SyncHelper.GetWaitAckCounter()}");
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
        var GETACKOnese = false;
        
        if (masterRedisNodeHost.Length == 0 || masterRedisNodePort == 0)
        {
            Console.WriteLine("Master redis node host or port is empty");
            return;
        }

        try
        {
            var client = new TcpClient(masterRedisNodeHost, masterRedisNodePort);
            NetworkStream stream = client.GetStream();

            var receivedData = new Byte[1024];

            string pingMessage = "1\r\n$4\r\nPING\r\n";
            byte[] pingData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', pingMessage));
            stream.Write(pingData, 0, pingData.Length);

            bool psyncSend = false;
            while (true)
            {
                var bytesRead = stream.Read(receivedData, 0, receivedData.Length);
                if (bytesRead == 0) break;
                var responseData = System.Text.Encoding.ASCII.GetString(receivedData, 0, bytesRead);
                Console.WriteLine($"responseData: {responseData}");

                if (responseData.ToUpper().Contains("PONG"))
                {
                    var listeningPort = $"3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n$4\r\n{Config.Port}\r\n";
                    var listeningPortData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', listeningPort));
                    Config.IsSyncHandshakeActive = true;
                    stream.Write(listeningPortData, 0, listeningPortData.Length);
                } 
                if (responseData[0] == '*')
                {
                    foreach (var command in responseData.Split("*")[1..])
                    {
                        if (command.ToUpper().Contains("REPLCONF")) continue;
                        Console.WriteLine($"    command: {command}");
                        CommandService.ParseCommand(serviceProvider, client,
                            $"*{command}".Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                if (responseData.Contains("GETACK", StringComparison.OrdinalIgnoreCase))
                {
                    System.Threading.Thread.Sleep(300);   
                    byte[] ackMessage; 
                    var counterValue = Config.GetCounter();
                    ackMessage = Encoding.ASCII.GetBytes(BuildResponse.Generate('*',
                        $"3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${counterValue.ToString().Length}\r\n{counterValue}\r\n"));
                    Console.WriteLine(
                        $"!GETACK!!! added {counterValue} ({35 + counterValue.ToString().Length + counterValue.ToString().Length.ToString().Length})");
                    Config.IncrementCounter(37);
                    stream.Write(ackMessage, 0, ackMessage.Length);
                }
                else if (responseData.ToUpper().Contains("OK") && !psyncSend)
                {
                    psyncSend = true;
                    var capa = "3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";
                    var capaData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', capa));
                    stream.Write(capaData, 0, capaData.Length);

                    var psync = "3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";
                    var psyncData = Encoding.ASCII.GetBytes(BuildResponse.Generate('*', psync));
                    stream.Write(psyncData, 0, psyncData.Length);
                }

            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during sync with master redis node : {e.Message}");
        }
        finally
        {
            // Config.IsSyncHandshakeActive = false;
        }
    }
}