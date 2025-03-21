using System.Collections.Concurrent;
using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Repositories;

public class StreamRepository : IStreamRepository
{
    private readonly ConcurrentDictionary<string, StreamDataCell> _storedData = new();

    public bool CheckIfStreamExists(string streamName)
    {
        return _storedData.TryGetValue(streamName, out var _);
    }
    
    public string AddData(string keyName, StreamDataCell data)
    {
        _storedData.AddOrUpdate(keyName, data, (k, v) => data);
        return data.Id;
    }
}