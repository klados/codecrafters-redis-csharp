using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Config
{
    public static string Dir = "";
    public static string DbFilename = "";
    public static int Port = 6379;
    public static bool IsReplicaOf = false;
    public static string MasterRedisNode = "";
    
    private static readonly object _lockObject = new object();
    public static int BytesSent = 0;
    public static bool IsSyncHandshakeActive = false;
    
    public static int GetCounter()
    {
        lock (_lockObject)
        {
            return BytesSent;
        }
    }

    public static void IncrementCounter(int increment)
    {
        lock (_lockObject)
        {
            BytesSent += increment;
        }
    }
    
    public void ParseCommandLineArgs(string?[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--dir" && i + 1 < args.Length)
            {
                Dir = args[i + 1] ?? "";
                i++; // Skip the value
            }
            else if (args[i] == "--dbfilename" && i + 1 < args.Length)
            {
                DbFilename = args[i + 1] ?? "";
                i++; // Skip the value
            }
            else if (args[i] == "--port" && i + 1 < args.Length)
            {
                Port = Convert.ToInt32(args[i + 1] ?? "6379");
                i++;
            }
            else if (args[i] == "--replicaof")
            {
                IsReplicaOf = true;
                MasterRedisNode = args[i + 1] ?? "";
                i++;
            }
        }
    }

    public string ConfigCmd(string subcommand, string targetArg)
    {
        if (!subcommand.ToUpper().Equals("GET"))
        {
            return BuildResponse.Generate('-',
                $"Unknown subcommand or wrong number of arguments for '{subcommand.ToUpper()}'. Try CONFIG HELP.");
        }

        switch (targetArg.ToUpper())
        {
            case "DIR":
            {
                var bulkString = ParseString.ParseKeyValueArray(new[] { targetArg }, new[] { Dir });
                return BuildResponse.Generate('*', bulkString);
            }
            case "DBFILENAME":
            {
                var bulkString = ParseString.ParseKeyValueArray(new[] { targetArg }, new[] { DbFilename });
                return BuildResponse.Generate('*', bulkString);
            }
            default:
                return BuildResponse.Generate('*', "-1\r\n"); //empty array
        }
    }
}