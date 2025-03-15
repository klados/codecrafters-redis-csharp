using System.Text;

namespace codecrafters_redis.Helpers;

public static class ParseString
{
    public static string ParseKeyValueArray(string[] keys, string[] data)
    {
        var len = keys.Length < data.Length ? keys.Length : data.Length;
        var sb = new StringBuilder();
        sb.Append($"{len*2}\r\n");

        for (var i = 0; i < len; i++)
        {
            sb.Append($"${keys[i].Length}\r\n{keys[i]}\r\n${data[i].Length}\r\n{data[i]}\r\n");
        }

        return sb.ToString();
    }
}