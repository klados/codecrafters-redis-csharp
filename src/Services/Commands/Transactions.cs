using System.Net.Sockets;
using codecrafters_redis.Helpers;
using codecrafters_redis.Repositories.Interfaces;
using codecrafters_redis.Services;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Commands;

public class Transactions
{
    private readonly ITransactionRepository _transactionRepository;

    public Transactions(ITransactionRepository transactionRepository)
    {
        _transactionRepository =
            transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public string MultiCommand(NetworkStream stream)
    {
        return _transactionRepository.InitNewTransaction(stream)
            ? BuildResponse.Generate('+', "OK")
            : BuildResponse.Generate('-', "MULTI calls can not be nested");
    }

    public string ExecCommand(ServiceProvider serviceProvider, TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        if (!_transactionRepository.CheckIfKeyExists(stream))
        {
            return BuildResponse.Generate('-', "EXEC without MULTI");
        }

        var commandsToExecute = _transactionRepository.GetTransactionsCommand(stream);

        if (commandsToExecute.Count == 0)
        {
            _transactionRepository.ClearStreamFromTransactionStateIfExists(stream);
            return BuildResponse.Generate('*', "0\r\n");
        }
        _transactionRepository.StartExecution(stream);
        
        var commandsResult = new List<string>();
        foreach (var command in commandsToExecute)
        {
            commandsResult.Add(CommandService.ParseCommand(serviceProvider, client, command.Split(',')));
        }

        _transactionRepository.ClearStreamFromTransactionStateIfExists(stream);
        return BuildResponse.Generate('*', ParseString.ParseArrayOfCommands(commandsResult.ToArray())); // fix it in the future
    }

    public string DiscardCommand(NetworkStream stream)
    {
        if (!_transactionRepository.CheckIfKeyExists(stream))
        {
            return BuildResponse.Generate('-', "DISCARD without MULTI");
        }
        _transactionRepository.ClearStreamFromTransactionStateIfExists(stream);
        return BuildResponse.Generate('+', "OK");
    }
    
    public bool CheckIfCommandShouldBeAddedToTransactionQueue(NetworkStream stream)
    {
        return _transactionRepository.CheckIfCommandShouldBeAddedToTransactionQueue(stream);
    }

    public void TryToAddToTransactionState(NetworkStream stream, string command)
    {
        _transactionRepository.TryToAddToTransactionState(stream, command);
    }
}