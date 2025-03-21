using System.Collections.Concurrent;
using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Repositories;

public class StreamRepository : IStreamRepository
{
    private readonly ConcurrentDictionary<string, List<StreamDataCell>> _storedData = new();

    public bool CheckIfStreamExists(string streamName)
    {
        return _storedData.TryGetValue(streamName, out var _);
    }

    public string AddData(string streamName, StreamDataCell data)
    {
        var newTimestamp = data.Id.Split('-');
        
        if (int.Parse(newTimestamp[0]) < 0 || int.Parse(newTimestamp[1]) < 0 ||
            (int.Parse(newTimestamp[0]) == 0 && int.Parse(newTimestamp[1]) == 0))
        {
            throw new Exception("The ID specified in XADD must be greater than 0-0");
        }

        _storedData.TryGetValue(streamName, out var storedData);
        if (storedData == null)
        {
            _storedData.TryAdd(streamName, new List<StreamDataCell> { data });
            return data.Id;
        }

        var lastAddedData = storedData.Last().Id;
        var oldTimestamp = lastAddedData.Split('-');
        if (int.Parse(oldTimestamp[0]) > int.Parse(newTimestamp[0]))
        {
            throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
        }

        if (int.Parse(oldTimestamp[1]) >= int.Parse(newTimestamp[1]) &&
            int.Parse(oldTimestamp[0]) == int.Parse(newTimestamp[0]))
        {
            throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
        }

        _storedData[streamName].Add(data);

        return data.Id;
    }
}