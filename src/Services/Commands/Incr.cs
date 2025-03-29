using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class Incr
{
    private readonly IStoreRepository _storeRepository;

    public Incr(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    public string IncrCommand(string? key)
    {
        if (key == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'incr' command");
        }

        var stringValue = _storeRepository.Get(key);

        if (stringValue == null)
        {
            _storeRepository.Add(key, "1");
            return BuildResponse.Generate(':', "1");
        }

        if (int.TryParse(stringValue, out var result))
        {
            var res = (result + 1).ToString();
            _storeRepository.Add(key, res);
            return BuildResponse.Generate(':', res);
        }
        
        return BuildResponse.Generate('-', "value is not an integer or out of range");
    }
}