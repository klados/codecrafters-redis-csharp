using System.Net.Sockets;
using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private static Dictionary<NetworkStream, TransactionQueueModel> _transactionState = new();

    public TransactionRepository()
    {
    }

    public bool InitNewTransaction(NetworkStream stream)
    {
        return _transactionState.TryAdd(stream, new TransactionQueueModel());
    }

    public void TryToAddToTransactionState(NetworkStream stream, string command)
    {
        _transactionState.TryGetValue(stream, out var list);
        list?.QueuedCommands.Add(command);
    }

    public bool CheckIfKeyExists(NetworkStream stream)
    {
        return _transactionState.ContainsKey(stream);
    }

    public List<string> GetTransactionsCommand(NetworkStream stream)
    {
        _transactionState.TryGetValue(stream, out var transactions);
        return transactions?.QueuedCommands ?? new List<string>();
    }

    public void ClearStreamFromTransactionStateIfExists(NetworkStream stream)
    {
        _transactionState.Remove(stream);
    }

    public void StartExecution(NetworkStream stream)
    {
        _transactionState.TryGetValue(stream, out var transactionQueueModel);
        transactionQueueModel.ExecuteTransaction = true;
    }

    public bool CheckIfCommandShouldBeAddedToTransactionQueue(NetworkStream stream)
    {
        return _transactionState.TryGetValue(stream, out var transactionQueueModel) && !transactionQueueModel.ExecuteTransaction;
    }
}