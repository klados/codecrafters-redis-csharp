using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class RedisType
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStreamRepository _streamRepository;
    
    public RedisType(IStoreRepository storeRepository, IStreamRepository streamRepository)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _streamRepository = streamRepository ?? throw new ArgumentNullException(nameof(streamRepository));
    }

    public string GetType(string? key)
    {
        if (key == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'type' command"); //Todo fix it
        }

        if (_storeRepository.Get(key) != null)
        {
            return BuildResponse.Generate('+', "string");
        }
        if (_streamRepository.CheckIfStreamExists(key))
        {
            return BuildResponse.Generate('+', "stream");
        }
        
        return BuildResponse.Generate('+', "none");
    }
}