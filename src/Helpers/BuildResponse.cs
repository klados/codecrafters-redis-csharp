namespace codecrafters_redis.Helpers;

public static class BuildResponse
{
    public static string Generate(char dataType, string response)
    {
        return (dataType) switch
        {
            '+' => $"+{response}\r\n",
            '$' => $"{dataType}{response.Length}\r\n{response}\r\n",
            '-' => $"{dataType}ERR {response}\r\n",
            '*' => $"{dataType}{response}",
            _ => throw new NotImplementedException("Unknown response type"),
        };
    }
}