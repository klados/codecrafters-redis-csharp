using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class Get
{
    private readonly IStoreRepository _storeRepository;

    public Get(IStoreRepository storeRepository) => _storeRepository = storeRepository;

    public string GetCommand(string? key)
    {
        if (key == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'get' command");
        }

        var res = _storeRepository.Get(key);
        return res == null ? $"$-1\r\n" : BuildResponse.Generate('$', res);
    }
}