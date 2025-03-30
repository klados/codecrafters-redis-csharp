using System.Net.Sockets;

namespace codecrafters_redis.Repositories.Interfaces;

public interface ITransactionRepository
{
    bool TryToAddToTransactionState(NetworkStream stream);
    bool CheckIfKeyExists(NetworkStream stream);
    List<string> GetTransactionsCommand(NetworkStream stream);
    void ClearStreamFromTransactionStateIfExists(NetworkStream stream);
}