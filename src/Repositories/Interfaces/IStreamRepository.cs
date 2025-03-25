using codecrafters_redis.Models;

namespace codecrafters_redis.Repositories.Interfaces;

public interface IStreamRepository
{
    bool CheckIfStreamExists(string streamName);
    public List<string> GetIdsOfAStream(string streamName);
    string AddData(string keyName, StreamDataCell data);
    IEnumerable<StreamDataCell> GetDataOfStream(string streamName, string startKey, string endTime);
    IEnumerable<StreamDataCell> GetDataOfStreamExclusive(string streamName, string startTime);
}