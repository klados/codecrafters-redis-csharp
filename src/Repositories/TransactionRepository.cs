using System.Net.Sockets;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Repositories;

public class TransactionRepository : ITransactionRepository
{

    private static Dictionary<NetworkStream, List<string>> _transactionState = new();
    
    public TransactionRepository()
    {
        
    }

    public bool InitNewTransaction(NetworkStream stream)
    {
        return _transactionState.TryAdd(stream, new List<string>());
    }

    public void TryToAddToTransactionState(NetworkStream stream, string command)
    {
        _transactionState.TryGetValue(stream, out var list);
        list?.Add(command);
    }

    public bool CheckIfKeyExists(NetworkStream stream)
    {
        return _transactionState.ContainsKey(stream);
    }

    public List<string> GetTransactionsCommand(NetworkStream stream)
    {
        return _transactionState.TryGetValue(stream, out var transactions) ? transactions : new List<string>();
    }

    public void ClearStreamFromTransactionStateIfExists(NetworkStream stream)
    {
        _transactionState.Remove(stream);
    }
}