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

    public List<string> GetIdsOfAStream(string streamName)
    {
        _storedData.TryGetValue(streamName, out var stream);

        if (stream == null) return new List<string>();

        return stream.Select(x => x.Id).ToList();
    }

    /// <summary>
    /// validate that the provided id is correct
    /// then add to ConcurrentDictionary
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string AddData(string streamName, StreamDataCell data)
    {
        var newTimestamp = data.Id.Split('-');

        if (float.Parse(newTimestamp[0]) < 0 || float.Parse(newTimestamp[1]) < 0 ||
            (float.Parse(newTimestamp[0]) == 0 && float.Parse(newTimestamp[1]) == 0))
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
        if (float.Parse(oldTimestamp[0]) > float.Parse(newTimestamp[0]))
        {
            throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
        }

        if (float.Parse(oldTimestamp[1]) >= float.Parse(newTimestamp[1]) &&
            float.Parse(oldTimestamp[0]) == float.Parse(newTimestamp[0]))
        {
            throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
        }

        _storedData[streamName].Add(data);

        return data.Id;
    }

    public string GetLatestTimestampOfAStream(string streamName)
    {
        _storedData.TryGetValue(streamName, out var streams);
        return streams?.LastOrDefault()?.Id ?? "0-0";
    }
    
    public IEnumerable<StreamDataCell> GetDataOfStream(string streamName, string startTime, string endTime)
    {
        _storedData.TryGetValue(streamName, out var storedData);

        if (storedData == null) return new List<StreamDataCell>();
        if (endTime == "+")
        {
            return storedData.Where(x => FromSpecificTimestampUntilTheLastInsertedData(x.Id, startTime));
        }

        if (startTime == "-")
        {
            return storedData.Where(x => FromTheStartUntilSpecifiedTimestamp(x.Id, endTime));
        }

        return storedData.Where(x => TimestampRangeCompare(x.Id, startTime, endTime));
    }

    public IEnumerable<StreamDataCell> GetDataOfStreamExclusive(string streamName, string startTime)
    {
        _storedData.TryGetValue(streamName, out var storedData);

        if (storedData == null) return new List<StreamDataCell>();

        return storedData.Where(x => FromSpecificTimestampUntilTheLastInsertedDataExclusive(x.Id, startTime));
    }

    /// <summary>
    /// return data the provided datetime until the last added record
    /// </summary>
    /// <param name="storedValueTime"></param>
    /// <param name="startTime"></param>
    /// <returns></returns>
    private static bool FromSpecificTimestampUntilTheLastInsertedData(string storedValueTime, string startTime)
    {
        var storedValueTimeTimestamp = storedValueTime.Split('-')[0];
        var storedValueTimeAutoIncrement = storedValueTime.Split('-')[1];

        var startTimeTimestamp = startTime.Split('-')[0];
        var startTimeAutoIncrement = startTime.Split('-')[1];

        return long.Parse(storedValueTimeTimestamp) >= long.Parse(startTimeTimestamp) &&
               long.Parse(storedValueTimeAutoIncrement) >= long.Parse(startTimeAutoIncrement);
    }

    /// <summary>
    /// return data the provided datetime until the last added record
    /// </summary>
    /// <param name="storedValueTime"></param>
    /// <param name="startTime"></param>
    /// <returns></returns>
    private static bool FromSpecificTimestampUntilTheLastInsertedDataExclusive(string storedValueTime, string startTime)
    {
        var storedValueTimeTimestamp = storedValueTime.Split('-')[0];
        var storedValueTimeAutoIncrement = storedValueTime.Split('-')[1];

        var startTimeTimestamp = startTime.Split('-')[0];
        var startTimeAutoIncrement = startTime.Split('-')[1];

        return long.Parse(storedValueTimeTimestamp) >= long.Parse(startTimeTimestamp) &&
               long.Parse(storedValueTimeAutoIncrement) > long.Parse(startTimeAutoIncrement);
    }
    
    /// <summary>
    /// return data from the first stored data to the datetime of the provided variable
    /// </summary>
    /// <param name="storedValueTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    private static bool FromTheStartUntilSpecifiedTimestamp(string storedValueTime, string endTime)
    {
        var storedValueTimeTimestamp = storedValueTime.Split('-')[0];
        var storedValueTimeAutoIncrement = storedValueTime.Split('-')[1];

        var endTimeTimestamp = endTime.Split('-')[0];
        var endTimeAutoIncrement = endTime.Split('-')[1];

        return long.Parse(storedValueTimeTimestamp) <= long.Parse(endTimeTimestamp) &&
               long.Parse(storedValueTimeAutoIncrement) <= long.Parse(endTimeAutoIncrement);
    }

    /// <summary>
    /// returns true is the provided value is inside the provided timestamp range
    /// </summary>
    /// <returns></returns>
    private static bool TimestampRangeCompare(string storedValueTime, string startTime, string endTime)
    {
        var storedValueTimeTimestamp = storedValueTime.Split('-')[0];
        var storedValueTimeAutoIncrement = storedValueTime.Split('-')[1];

        var startTimeTimestamp = startTime.Split('-')[0];
        var startTimeAutoIncrement = startTime.Split('-')[1];

        var endTimeTimestamp = endTime.Split('-')[0];
        var endTimeAutoIncrement = endTime.Split('-')[1];

        return long.Parse(storedValueTimeTimestamp) >= long.Parse(startTimeTimestamp) &&
               long.Parse(storedValueTimeAutoIncrement) >= long.Parse(startTimeAutoIncrement) &&
               long.Parse(storedValueTimeTimestamp) <= long.Parse(endTimeTimestamp) &&
               long.Parse(storedValueTimeAutoIncrement) <= long.Parse(endTimeAutoIncrement);
    }
}