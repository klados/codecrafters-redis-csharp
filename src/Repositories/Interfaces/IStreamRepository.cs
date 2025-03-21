using codecrafters_redis.Models;

namespace codecrafters_redis.Repositories.Interfaces;

public interface IStreamRepository
{
    bool CheckIfStreamExists(string streamName);
    string AddData(string keyName, StreamDataCell data);
}