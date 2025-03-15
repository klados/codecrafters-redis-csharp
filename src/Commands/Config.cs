using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Config
{
    private static string _dir = "";
    private static string _dbFilename = "";

    public void ParseCommandLineArgs(string?[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--dir" && i + 1 < args.Length)
            {
                _dir = args[i + 1] ?? "";
                i++; // Skip the value
            }
            else if (args[i] == "--dbfilename" && i + 1 < args.Length)
            {
                _dbFilename = args[i + 1] ?? "";
                i++; // Skip the value
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
                var bulkString = ParseString.ParseKeyValueArray(new[] { targetArg }, new[] { _dir });
                return BuildResponse.Generate('*', bulkString);
            }
            case "DBFILENAME":
            {
                var bulkString = ParseString.ParseKeyValueArray(new[] { targetArg }, new[] { _dbFilename });
                return BuildResponse.Generate('*', bulkString);
            }
            default:
                return BuildResponse.Generate('*', "-1\r\n"); //empty array
        }
    }
}