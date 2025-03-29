using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Multi
{

    public Multi()
    {
        
    }

    public string MultiCommand(params string[] arguments)
    {
        return BuildResponse.Generate('+', "OK");
    }
}