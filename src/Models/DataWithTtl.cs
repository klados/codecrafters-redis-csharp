namespace codecrafters_redis.Models;

public class DataWithTtl
{
    public string strValue { get; set; }
    public DateTime? ExpiredAt { get; set; }
}