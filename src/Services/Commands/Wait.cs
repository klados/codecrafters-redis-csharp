using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Wait
{
    private static readonly string _replconf = $"3\r\n$8\r\nREPLCONF\r\n$6\r\nGETACK\r\n$1\r\n*\r\n";
    private static readonly Lock LockObject = new Lock();

    public async Task<string> WaitCommand(params string[] args)
    {
        var valid = int.TryParse(args[0], out var waitForReplicas);
        var valid2 = int.TryParse(args[2], out var waitForMilliseconds);
        if (!valid || !valid2)
        {
            return BuildResponse.Generate('-', "value is not an integer or out of range");
        }
        
        Config.SetIsWait(true);
        var responses = 0;
        var waitStartTime = Stopwatch.StartNew();

        foreach (var client in SyncHelper.GetConnectedSlaves())
        {
            if (client.Value.CommandsServed == 0)
            {
                lock (LockObject)
                {
                    responses++;
                }
                continue;
            }

            try
            {
                NetworkStream stream = client.Value.TcpClient.GetStream();
                await stream.WriteAsync(Encoding.ASCII.GetBytes(BuildResponse.Generate('*', _replconf)));
                //await stream.FlushAsync(); // Ensure data is flushed
            }catch (Exception e)
            {
                Console.WriteLine($"failed to wait: {e.Message}");
            }
        }

        if(SyncHelper.GetConnectedSlaves().Count != 0)
            System.Threading.Thread.Sleep(waitForMilliseconds);

        waitStartTime.Stop();
        responses += Config.GetAckCounter();
        Config.SetIsWait(false);
        Console.WriteLine($"responses {responses}");
        Config.ResetAckCounter();

        return BuildResponse.Generate(':',$"{responses}");
    }
}