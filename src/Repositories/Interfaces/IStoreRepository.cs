namespace codecrafters_redis.Repositories.Interfaces;

public interface IStoreRepository
{
    void Add(string key, string data, DateTime? ttl = null);
    bool Remove(string key);
    string? Get(string key);
    List<string> GetValidKeys(string searchString);
}