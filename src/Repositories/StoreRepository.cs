using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Repositories;

public class StoreRepository : IStoreRepository
{
    private ConcurrentDictionary<string, DataWithTtl> storedData = new();

    public StoreRepository()
    {
    }

    public void Add(string key, string data, DateTime? ttl = null)
    {
        if (storedData.TryAdd(key, new DataWithTtl()
            {
                strValue = data,
                ExpiredAt = ttl
            })) return;

        var currentValue = storedData[key];
        if (storedData.TryUpdate(key, new DataWithTtl()
            {
                strValue = data,
                ExpiredAt = ttl
            }, currentValue))
        {
            Console.WriteLine($"Failed to update stored data for key: {key}");
        }
    }

    public bool Remove(string key)
    {
        return storedData.TryRemove(key, out _);
    }

    public string? Get(string key)
    {
        storedData.TryGetValue(key, out var data);
        if (data == null) return null;
        if (!data.ExpiredAt.HasValue) return data.strValue;
        if (DateTime.Now > data.ExpiredAt)
        {
            Remove(key);
            return null;
        }

        return data.strValue;
    }

    public List<string> GetValidKeys(string searchString)
    {
        var keys = storedData.Where(x => x.Value.ExpiredAt == null || (x.Value.ExpiredAt > DateTime.Now)).ToDictionary()
            .Keys.ToList();
        return keys.Where(item => Regex.IsMatch(item, $@"^{searchString.Replace("*", ".*")}$")).ToList();
    }
}