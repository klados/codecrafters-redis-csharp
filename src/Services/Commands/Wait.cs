using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Wait
{
    public string WaitCommand()
    {
        return BuildResponse.Generate(':', SyncHelper.GetSlavesCount().ToString());
    }
}