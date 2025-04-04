using System.Net.Sockets;

namespace codecrafters_redis.Repositories.Interfaces;

public interface ITransactionRepository
{
    bool InitNewTransaction(NetworkStream stream);
    void TryToAddToTransactionState(NetworkStream stream, string command);
    bool CheckIfKeyExists(NetworkStream stream);
    List<string> GetTransactionsCommand(NetworkStream stream);
    void ClearStreamFromTransactionStateIfExists(NetworkStream stream);
    public void StartExecution(NetworkStream stream);
    bool CheckIfCommandShouldBeAddedToTransactionQueue(NetworkStream stream);
}