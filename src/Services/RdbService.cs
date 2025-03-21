using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Repositories.Interfaces;

namespace codecrafters_redis.Services;

public class RdbService
{
    private IStoreRepository _storeRepository;

    public RdbService(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
    }

    public void LoadDataFromFile()
    {
        var filePath = Path.Combine(Config.Dir, Config.DbFilename);

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File {filePath} does not exist!, no data loaded!");
            return;
        }

        try
        {
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(fs);
            ProcessFile(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File {filePath} could not be loaded: {ex.Message}");
        }
    }

    /// <summary>
    /// for more data read https://rdb.fnordig.de/file_format.html
    /// </summary>
    /// <param name="reader"></param>
    private void ProcessFile(BinaryReader reader)
    {
        Console.WriteLine("Reading RDB file...");

        reader.ReadChars(9); // redisNameAndVersion

        try
        {
            var opCode = reader.ReadByte();

            // skip Metadata section
            while(opCode != 0xFE) opCode = reader.ReadByte();

            reader.ReadByte(); // databaseSelector
            ReadDatabaseSection(reader);

            //ToDo read checksum
            return;
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void ReadDatabaseSection(BinaryReader reader)
    {
        try
        {
            var ds = reader.ReadByte();
            if (ds != 0xFB) throw new Exception("Invalid database section");

            var hashTableSize = reader.ReadByte();
            var keysWithExpireSize = reader.ReadByte();
            var typeOfData = reader.ReadByte();

            while (typeOfData != 0xFF)
            {
                // optional expire time
                DateTime? expireAt = null;
                if (typeOfData == 0xFC)
                {
                    var b = reader.ReadBytes(8);
                    expireAt = DateTime.UnixEpoch.AddMilliseconds(BitConverter.ToInt64(b, 0));
                    typeOfData = reader.ReadByte();
                }
                else if (typeOfData == 0xFD)
                {
                    var b = reader.ReadBytes(8);
                    expireAt = DateTime.UnixEpoch.AddSeconds(BitConverter.ToInt64(b, 0));
                    typeOfData = reader.ReadByte();
                }

                if (typeOfData == 0)
                {
                    var strList = ExtractListOfStrings(reader, 2);
                    if (strList.Count != 2) throw new Exception("Failed to retrieve string data");
                    _storeRepository.Add(strList[0], strList[1], expireAt);
                }
                else throw new Exception("Non supported data type");

                typeOfData = reader.ReadByte();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Extract strings and return an array of strings. The first string will be the key the rest the value(s)
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="numberOfStrings"></param>
    /// <returns></returns>
    private static List<string> ExtractListOfStrings(BinaryReader reader, int numberOfStrings)
    {
        try
        {
            var listOfStrings = new List<string>();
            for (var i = 0; i < numberOfStrings; i++)
            {
                var lenOfString = reader.ReadByte();
                listOfStrings.Add(Encoding.UTF8.GetString(reader.ReadBytes(lenOfString)));
            }

            return listOfStrings;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return [];
        }
    }
}