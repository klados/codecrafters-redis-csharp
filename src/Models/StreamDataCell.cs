namespace codecrafters_redis.Models;

public class StreamDataCell
{
    public string Id { get; set; }
    public List<KeyValuePair<string,string>> Data { get; set; } = new();
}