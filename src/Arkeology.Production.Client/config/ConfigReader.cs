using System.IO;
using System.Text;

namespace Arkeology.Production.Client;

public abstract class ConfigReader<T>
{
    public T Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var header = ReadHeader(reader);
        return ReadData(reader, header);
    }

    protected abstract T ReadData(BinaryReader reader, ConfigHeader header);

    private static ConfigHeader ReadHeader(BinaryReader reader)
    {
        var versionMajor = reader.ReadByte();
        var versionMinor = reader.ReadByte();
        var buildTime = reader.ReadInt64();

        var tokenCount = reader.ReadUInt16();
        var userTokens = new string[tokenCount];
        for (var i = 0; i < tokenCount; i++)
        {
            var length = reader.ReadByte();
            userTokens[i] = Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        var listCount = reader.ReadUInt16();
        var tokenLists = new ushort[listCount][];
        for (var i = 0; i < listCount; i++)
        {
            var count = reader.ReadByte();
            tokenLists[i] = new ushort[count];
            for (var j = 0; j < count; j++)
                tokenLists[i][j] = reader.ReadUInt16();
        }

        return new ConfigHeader(versionMajor, versionMinor, buildTime, new StringTable(userTokens, tokenLists));
    }
}
