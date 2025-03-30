using System.Net.Sockets;
using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class Transactions
{
    private ITransactionRepository _transactionRepository;

    public Transactions(ITransactionRepository transactionRepository)
    {
        _transactionRepository =
            transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public string MultiCommand(NetworkStream stream)
    {
        _transactionRepository.TryToAddToTransactionState(stream);
        return BuildResponse.Generate('+', "OK");
    }

    public string ExecCommand(NetworkStream stream)
    {
        if (!_transactionRepository.CheckIfKeyExists(stream))
        {
            return BuildResponse.Generate('-', "EXEC without MULTI");
        }

        var res = _transactionRepository.GetTransactionsCommand(stream);

        if (res.Count == 0)
        {
            _transactionRepository.ClearStreamFromTransactionStateIfExists(stream);
            return BuildResponse.Generate('*', "0\r\n");
        }
        
        return BuildResponse.Generate('+', "OK"); // fix it in the future
    }
}