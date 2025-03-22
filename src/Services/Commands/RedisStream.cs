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

    private string GenerateKeyIfNeeded(string streamName, string key)
    {
        var time = key.Split('-');
        var timestamp = time.ElementAtOrDefault(0);
        var autoIncrement = time.ElementAtOrDefault(1);
        if (timestamp == null || autoIncrement == null || (timestamp == "*" && autoIncrement == "*"))
        {
            throw new Exception("Invalid stream ID specified as stream command argument");
        }

        var newAutoIncrement = "";
        var ids = _streamRepository.GetIdsOfAStream(streamName);
        if (autoIncrement == "*")
        {
            if (ids.Count == 0)
            {
                newAutoIncrement = timestamp != "0" ? "0" : "1";
            }
            else
            {
                var lastTimestamp = ids.Last().Split('-')[0];
                var lastAutoIncrement = ids.Last().Split('-')[1];

                newAutoIncrement = (timestamp == lastTimestamp)
                    ? (int.Parse(lastAutoIncrement) + 1).ToString()
                    : "0";
            }
        }
        else
        {
            newAutoIncrement = autoIncrement;
        }

        return $"{timestamp}-{newAutoIncrement}";
    }

    public string XADD(params string[] arguments)
    {
        var streamName = arguments.ElementAtOrDefault(0);
        var keyId = arguments.ElementAtOrDefault(2);

        if (arguments.Length % 2 != 0 || keyId is null || streamName is null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'xadd' command");
        }

        var streamData = new StreamDataCell();
        keyId = GenerateKeyIfNeeded(streamName, keyId);
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
            keyId = _streamRepository.AddData(streamName, streamData);
        }
        catch (Exception e)
        {
            return BuildResponse.Generate('-', e.Message);
        }

        return BuildResponse.Generate('$', keyId);
    }
}