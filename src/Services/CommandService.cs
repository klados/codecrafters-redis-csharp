using System.Net.Sockets;
using codecrafters_redis.Commands;
using codecrafters_redis.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Services;

public static class CommandService 
{
    public static string ParseCommand(ServiceProvider serviceProvider, TcpClient client, string[] arrayStrings)
    {
        Console.WriteLine($"command to be parsed {string.Join(" ",arrayStrings)}, {client.Client.RemoteEndPoint} | is slave:{Config.IsReplicaOf}");
        NetworkStream stream = client.GetStream();

        var command = arrayStrings.ElementAtOrDefault(2);
        if (command == null) return "";
        var argumentForCommand = arrayStrings.ElementAtOrDefault(4);

        DateTime? ttl = null;
        var time = arrayStrings.ElementAtOrDefault(10);
        switch (arrayStrings.ElementAtOrDefault(8)?.ToUpper())
        {
            case "PX":
                ttl = time == null ? null : DateTime.Now.AddMilliseconds(double.Parse(time));
                break;
            case "EX":
                ttl = time == null ? null : DateTime.Now.AddSeconds(double.Parse(time));
                break;
        }

        // check if for the current NetworkStream we are running a transaction
        if (serviceProvider.GetRequiredKeyedService<Transactions>(null).CheckIfCommandShouldBeAddedToTransactionQueue(stream) &&
            !command.Equals("EXEC", StringComparison.CurrentCultureIgnoreCase) &&
            !command.Equals("DISCARD", StringComparison.CurrentCultureIgnoreCase) &&
            !command.Equals("MULTI", StringComparison.CurrentCultureIgnoreCase))
        {
            serviceProvider.GetRequiredKeyedService<Transactions>(null)
                .TryToAddToTransactionState(stream, string.Join(",", arrayStrings));
            return BuildResponse.Generate('+', "QUEUED");
        }

        return command.ToUpper() switch
        {
            "PING" => serviceProvider.GetRequiredKeyedService<Ping>(null).PingCommand(),
            "ECHO" => BuildResponse.Generate(argumentForCommand != null ? '$' : '-',
                argumentForCommand ?? "wrong number of arguments for 'echo' command"),
            "SET" => serviceProvider.GetRequiredKeyedService<Set>(null).SetCommand(argumentForCommand, arrayStrings.ElementAtOrDefault(6), ttl),
            "GET" => serviceProvider.GetRequiredKeyedService<Get>(null).GetCommand(argumentForCommand),
            "CONFIG" => serviceProvider.GetRequiredKeyedService<Config>(null).ConfigCmd(argumentForCommand ?? "", arrayStrings.ElementAtOrDefault(6) ?? ""),
            "KEYS" => serviceProvider.GetRequiredKeyedService<Keys>(null).GetKeys(argumentForCommand),
            "TYPE" => serviceProvider.GetRequiredKeyedService<RedisType>(null).GetType(argumentForCommand),
            "XADD" => serviceProvider.GetRequiredKeyedService<RedisStream>(null).XADD(arrayStrings[4..]),
            "XRANGE" => serviceProvider.GetRequiredKeyedService<RedisStream>(null).XRANGE(arrayStrings[4..]),
            "XREAD" => serviceProvider.GetRequiredKeyedService<RedisStream>(null).XREAD(arrayStrings[4..]),
            "INCR" => serviceProvider.GetRequiredKeyedService<Incr>(null).IncrCommand(argumentForCommand),
            "MULTI" => serviceProvider.GetRequiredKeyedService<Transactions>(null).MultiCommand(stream),
            "EXEC" => serviceProvider.GetRequiredKeyedService<Transactions>(null).ExecCommand(serviceProvider, client),
            "DISCARD" => serviceProvider.GetRequiredKeyedService<Transactions>(null).DiscardCommand(stream),
            "INFO" => serviceProvider.GetRequiredKeyedService<Info>(null).InfoCommand(arrayStrings[4..]),
            "REPLCONF" => serviceProvider.GetRequiredKeyedService<SyncMasterSlave>(null).REPLCONFCommand(client,arrayStrings[4..]),
            "PSYNC" => serviceProvider.GetRequiredKeyedService<SyncMasterSlave>(null).MasterPsync(client),
            "WAIT" => serviceProvider.GetRequiredKeyedService<Wait>(null).WaitCommand(client, arrayStrings[4..]),
            _ => BuildResponse.Generate('+', "UNKNOWN")
        };
    }
}