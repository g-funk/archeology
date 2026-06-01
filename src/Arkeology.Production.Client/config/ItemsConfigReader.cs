using System.Collections.Generic;
using System.IO;

namespace Arkeology.Production.Client;

public class ItemsConfigReader : ConfigReader<IReadOnlyList<ItemConfig>>
{
    protected override IReadOnlyList<ItemConfig> ReadData(BinaryReader reader, ConfigHeader header)
    {
        var result =  new List<ItemConfig>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var id = reader.ReadUInt16();
            var rarity = (Rarity)reader.ReadByte();
            var partCount = reader.ReadByte();
            var partIds = new int[partCount];
            for (var i = 0; i < partCount; i++)
                partIds[i] = reader.ReadUInt16();
            var name = header.Strings.Resolve(reader.ReadUInt16());
            var desc = header.Strings.Resolve(reader.ReadUInt16());
            result.Add(new (id, name, desc, rarity, partIds));
        }

        return result;
    }
}
