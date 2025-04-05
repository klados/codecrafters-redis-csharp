using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Info
{
    private readonly IDictionary<string, string> _infoData = new Dictionary<string, string>()
    {
        { "REPLICATION", $"role:{(Config.IsReplicaOf ? "slave" : "master")}" }
    };

    public string InfoCommand(params string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return BuildResponse.Generate('$', _infoData["REPLICATION"]);
        }

        if (arguments.FirstOrDefault()?.ToUpper() == "REPLICATION")
        {
            var replication = _infoData["REPLICATION"];
            return BuildResponse.Generate('$', replication);
        }

        return "$-1\r\n"; //nil 
    }
}