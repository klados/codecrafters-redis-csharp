using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Helpers;
using codecrafters_redis.Models;

namespace codecrafters_redis.Commands;

public class Transactions
{
    private static readonly ConcurrentDictionary<NetworkStream, bool> transactionState = new ();
    public Transactions()
    {
        
    }

    public string MultiCommand(NetworkStream stream)
    {
        transactionState[stream] = true;
        return BuildResponse.Generate('+', "OK");
    }
    
    public string ExecCommand(NetworkStream stream)
    {
        return transactionState.TryGetValue(stream, out var value)
            ? BuildResponse.Generate('+', "OK")
            : BuildResponse.Generate('-', "EXEC without MULTI");
    }
}