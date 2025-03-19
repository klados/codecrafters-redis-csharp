using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class Set
{
    private readonly IStoreRepository _storeRepository;

    public Set(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    public string SetCommand(string? key, string? data, DateTime? ttl)
    {
        if (key == null || data == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'set' command");
        }

        _storeRepository.Add(key, data, ttl);
        return BuildResponse.Generate('+', "OK");
    }
}