using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class RedisType
{
    private readonly IStoreRepository _storeRepository;

    public RedisType(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    public string GetType(string? key)
    {
        if (key == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'type' command"); //Todo fix it
        }

        var val = _storeRepository.Get(key);
        if (val == null)
        {
            return BuildResponse.Generate('+', "none");
        }

        return BuildResponse.Generate('+', "string");
    }
}