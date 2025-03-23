using System.Text;
using codecrafters_redis.Models;

namespace codecrafters_redis.Helpers;

public static class ParseString
{
    /// <summary>
    /// prepare the string for response
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="data"></param>
    /// <returns></returns>
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

    public static string ParseArray(string[] data)
    {
        var sb = new StringBuilder();
        sb.Append($"{data.Length}\r\n");
        foreach (var d in data)
        {
            sb.Append($"${d.Length}\r\n{d}\r\n");
        }
        return sb.ToString();
    }

    public static string ParseStreamDataCellL(StreamDataCell data)
    {
        var sb = new StringBuilder();
        sb.Append("*2\r\n");
        sb.Append($"${data.Id.Length}\r\n{data.Id}\r\n");
        sb.Append($"*{data.Data.Count*2}\r\n");
        foreach (var d in data.Data)
        {
            sb.Append($"${d.Key.Length}\r\n{d.Key}\r\n");
            sb.Append($"${d.Value.Length}\r\n{d.Value}\r\n");
        }
        return sb.ToString();
    }
    
    public static string ParseStreamDataCellList(List<StreamDataCell> data)
    {
        var sb = new StringBuilder();
        sb.Append(data.Count + "\r\n");
        
        foreach (var d in data)
        {
            sb.Append(ParseStreamDataCellL(d));
        }
        
        return sb.ToString();
    }
}