using System.Diagnostics;
using System.Net.Sockets;
using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Wait
{
    public string WaitCommand(TcpClient client, params string[] args)
    {
        var valid = int.TryParse(args[0], out var waitForReplicas);
        var valid2 = int.TryParse(args[2], out var waitForMilliseconds);
        if (!valid || !valid2)
        {
            return BuildResponse.Generate('-', "value is not an integer or out of range");
        }

        var res = "";
        Stopwatch stopwatch = Stopwatch.StartNew();
        SyncHelper.StartWaitRunning();
        while (true)
        {
            Console.WriteLine($"========== {SyncHelper.GetWaitAckCounter()}");
            if (SyncHelper.GetWaitAckCounter() >= waitForReplicas )
            {
                res = SyncHelper.GetWaitAckCounter().ToString();
                break;
            }
            double elapsedSeconds = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsedSeconds >= waitForMilliseconds)
            {
                res = SyncHelper.GetWaitAckCounter().ToString();
                break;
            }
            Thread.Sleep(50);
        }
        stopwatch.Stop();
        SyncHelper.AckCounterInitToZero();
        return BuildResponse.Generate(':', res);
    }
}