using codecrafters_redis.Helpers;
using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class RedisStream
{
    private readonly IStreamRepository _streamRepository;

    public RedisStream(IStreamRepository streamRepository)
    {
        _streamRepository = streamRepository ?? throw new ArgumentNullException(nameof(streamRepository));
    }

    public string XADD(params string[] arguments)
    {
        var keyName = arguments.ElementAtOrDefault(0);
        var keyId = arguments.ElementAtOrDefault(2);

        if (arguments.Length % 2 != 0 || keyId is null || keyName is null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'xadd' command");
        }

        var streamData = new StreamDataCell();
        //ToDo if keyId contains * generate a new id
        streamData.Id = keyId;
        for (var i = 4; i < arguments.Length; i += 4)
        {
            if (arguments.ElementAtOrDefault(i) == null || arguments.ElementAtOrDefault(i + 2) == null)
            {
                return BuildResponse.Generate('-', "wrong number of arguments for 'xadd' command");
            }

            streamData.Data.Add(new KeyValuePair<string, string>(arguments.ElementAtOrDefault(i),
                arguments.ElementAtOrDefault(i + 2)));
        }

        try
        {
            keyId = _streamRepository.AddData(keyName, streamData);
        }
        catch (Exception e)
        {
            return BuildResponse.Generate('-', e.Message);
        }
        return BuildResponse.Generate('$', keyId);
    }
}