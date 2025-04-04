namespace codecrafters_redis.Models;

public class TransactionQueueModel
{
    public List<string> QueuedCommands = new();

    public bool ExecuteTransaction { get; set; } = false;
}