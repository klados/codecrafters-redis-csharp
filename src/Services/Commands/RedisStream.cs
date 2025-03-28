using System.Globalization;
using codecrafters_redis.Helpers;
using codecrafters_redis.Models;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Commands;

public class RedisStream
{
    private readonly IStreamRepository _streamRepository;

    public RedisStream(IStreamRepository streamRepository)
    {
        _streamRepository = streamRepository ?? throw new ArgumentNullException(nameof(streamRepository));
    }

    private string GenerateKeyIfNeeded(string streamName, string key)
    {
        var time = key.Split('-');
        var timestamp = time.ElementAtOrDefault(0);
        var autoIncrement = time.ElementAtOrDefault(1);

        // validate time input 
        if (timestamp == null ||
            (timestamp != null && timestamp != "*" && autoIncrement == null) ||
            (timestamp == "*" && autoIncrement == null && time.Contains("-")) ||
            (timestamp == "*" && autoIncrement != null))
        {
            throw new Exception("Invalid stream ID specified as stream command argument");
        }

        var newAutoIncrement = "";
        var ids = _streamRepository.GetIdsOfAStream(streamName);
        if (timestamp == "*")
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            newAutoIncrement = "0";
        }
        else if (autoIncrement == "*")
        {
            if (ids.Count == 0)
            {
                newAutoIncrement = timestamp != "0" ? "0" : "1";
            }
            else
            {
                var lastTimestamp = ids.Last().Split('-')[0];
                var lastAutoIncrement = ids.Last().Split('-')[1];

                newAutoIncrement = (timestamp == lastTimestamp)
                    ? (float.Parse(lastAutoIncrement) + 1).ToString(CultureInfo.InvariantCulture)
                    : "0";
            }
        }
        else
        {
            newAutoIncrement = autoIncrement;
        }

        return $"{timestamp}-{newAutoIncrement}";
    }

    public string XADD(params string[] arguments)
    {
        var streamName = arguments.ElementAtOrDefault(0);
        var keyId = arguments.ElementAtOrDefault(2);

        if (arguments.Length % 2 != 0 || keyId is null || streamName is null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'xadd' command");
        }

        var streamData = new StreamDataCell();
        keyId = GenerateKeyIfNeeded(streamName, keyId);
        streamData.Id = keyId;
        for (var i = 4; i < arguments.Length; i += 4)
        {
            if (arguments.ElementAtOrDefault(i) == null || arguments.ElementAtOrDefault(i + 2) == null)
            {
                return BuildResponse.Generate('-', "wrong number of arguments for 'xadd' command");
            }

            streamData.Data.Add(new KeyValuePair<string, string>(arguments.ElementAtOrDefault(i),
                arguments.ElementAtOrDefault(i + 2)));
        }

        try
        {
            keyId = _streamRepository.AddData(streamName, streamData);
        }
        catch (Exception e)
        {
            return BuildResponse.Generate('-', e.Message);
        }

        return BuildResponse.Generate('$', keyId);
    }

    public string XRANGE(params string[] arguments)
    {
        var streamName = arguments.ElementAtOrDefault(0);
        var startTime = arguments.ElementAtOrDefault(2);
        var endTime = arguments.ElementAtOrDefault(4);

        if (streamName == null || startTime == null || endTime == null)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'xrange' command");
        }

        var data = _streamRepository.GetDataOfStream(streamName, startTime, endTime);

        return BuildResponse.Generate('*', ParseString.ParseStreamDataCellList(data.ToList()));
    }

    public string XREAD(params string[] arguments)
    {
        var streamNames = new List<string>();
        var startTimes = new List<string>();
        var startIndex = 2;
        var block = false;
        var blockDurationStr = arguments.ElementAtOrDefault(2);
        
        if (arguments.ElementAtOrDefault(0)?.ToUpper() == "BLOCK")
        {
            startIndex = 6;
            block = true;
        }
        
        var endOfNames = false;
        for (var i = startIndex; i < arguments.Length; i += 2)
        {
            if (!endOfNames && arguments[i].Split('-').Length == 2)
            {
                endOfNames = true;
            }

            if (!endOfNames)
            {
                streamNames.Add(arguments[i]);
            }
            else
            {
                startTimes.Add(arguments[i]);
            }
        }

        if (streamNames.Count == 0 || startTimes.Count == 0)
        {
            return BuildResponse.Generate('-', "wrong number of arguments for 'xread' command");
        }

        if (streamNames.Count != startTimes.Count)
        {
            return BuildResponse.Generate('-',
                "Unbalanced XREAD list of streams: for each stream key an ID or '$' must be specified.");
        }

        if (block && blockDurationStr == null)
        {
            return BuildResponse.Generate('-',"timeout is not an integer or out of range");
        }
        
        var data = new List<(string, IEnumerable<StreamDataCell>)>();

        if (block)
        {
            var blockDurationCast =  float.TryParse(blockDurationStr, out var blockDuration);

            if (!blockDurationCast)
            {
                return BuildResponse.Generate('-', "timeout is not an integer or out of range");
            }
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                System.Threading.Thread.Sleep(30);
                data.Clear();
                if (sw.ElapsedMilliseconds >= blockDuration && blockDuration > 0)
                {
                    return "$-1\r\n"; // return nil
                }
                
                for (var i = 0; i < streamNames.Count; i++)
                {
                    var dataForSpecificStream = _streamRepository.GetDataOfStreamExclusive(streamNames[i], startTimes[i]).ToList();
                    if(dataForSpecificStream.Count > 0) data.Add((streamNames[i], dataForSpecificStream));
                }
                
                if(data.Count != 0) return BuildResponse.Generate('*', ParseString.ParseStreamDataCellListWithStreamNames(data));
            } 
        }
        
        for (var i = 0; i < streamNames.Count; i++)
        {
            var dataForSpecificStream = _streamRepository.GetDataOfStreamExclusive(streamNames[i], startTimes[i]).ToList();
            if(dataForSpecificStream.Count > 0) data.Add((streamNames[i], dataForSpecificStream));
        }

        return data.Count == 0
            ? "$-1\r\n" // return nil
            : BuildResponse.Generate('*', ParseString.ParseStreamDataCellListWithStreamNames(data));
    }
}