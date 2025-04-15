using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Ping
{
    public string PingCommand()
    {
        Console.WriteLine($"PING {Config.IsSyncHandshakeActive}");

        if (Config.IsSyncHandshakeActive)
        {
            Console.WriteLine($"!!ping!! added {Config.GetCounter()} (14)");
            Config.IncrementCounter(14);
            return "";
        }

        return BuildResponse.Generate('+', "PONG");
    }
}