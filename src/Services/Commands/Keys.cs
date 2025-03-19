using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class Keys
{
    private readonly IStoreRepository _storeRepository;

    public Keys(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    public string GetKeys(string? searchString)
    {
        if(searchString == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'keys' command");
        }
        
        var keys = _storeRepository.GetValidKeys(searchString);
        
        if (keys.Count == 0) return BuildResponse.Generate('*', "0\r\n");
        
        return BuildResponse.Generate('*',ParseString.ParseArray(keys.ToArray()));
    }
}