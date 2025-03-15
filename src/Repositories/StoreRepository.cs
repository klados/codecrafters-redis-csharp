using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Repositories;

public class StoreRepository: IStoreRepository
{
    private Dictionary<string, DataWithTtl> storedData = new();

    public StoreRepository()
    {
    }

    public void Add(string key, string data, string? ttl = null)
    {
        storedData.Add(key, new DataWithTtl()
        {
            strValue = data,
            ExpiredAt = ttl == null ? null : DateTime.Now.AddMilliseconds(double.Parse(ttl))
        });
    }

    public bool Remove(string key)
    {
        return storedData.Remove(key);
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
}