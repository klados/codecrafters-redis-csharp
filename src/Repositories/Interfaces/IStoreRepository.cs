namespace codecrafters_redis.Repositories.Interfaces;

public interface IStoreRepository
{
    void Add(string key, string data, string? ttl = null);
    bool Remove(string key);
    public string? Get(string key);
}