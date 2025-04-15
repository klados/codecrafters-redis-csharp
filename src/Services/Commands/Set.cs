using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class Set
{
    private readonly IStoreRepository _storeRepository;
    private const int NumberOfStandardSetCharacters = 23;

    //private static readonly object _lockObject = new object();

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
        //lock (_lockObject)
        //{
            Console.WriteLine($"!set!! added {Config.GetCounter()} {data.Length + key.Length + NumberOfStandardSetCharacters + data.Length.ToString().Length + key.Length.ToString().Length}");
            Config.IncrementCounter(data.Trim().Length + key.Trim().Length + NumberOfStandardSetCharacters + data.Trim().Length.ToString().Length + key.Trim().Length.ToString().Length);
        //}
        _storeRepository.Add(key, data, ttl);
        if(!Config.IsReplicaOf) SyncHelper.SyncCommand($"set {key} {data} {ttl}");
        return BuildResponse.Generate('+', "OK");
    }
}