using System;
using System.Collections.Generic;
using System.IO;

namespace Arkeology.Production.Client;

public class ItemsConfigReader : ConfigReader<IReadOnlyList<ItemConfig>>
{
    protected override IReadOnlyList<ItemConfig> ReadData(BinaryReader reader, ConfigHeader header)
    {
        var itemCount = reader.ReadUInt16();
        var result = new List<ItemConfig>(itemCount);

        for (int i = 0; i < itemCount; i++)
        {
            var id = reader.ReadUInt16();
            var rarity = (Rarity)reader.ReadByte();
            var partCount = reader.ReadByte();
            var partIds = new int[partCount];
            for (var p = 0; p < partCount; p++)
                partIds[p] = reader.ReadUInt16();
            var name = header.Strings.Resolve(reader.ReadUInt16());
            var desc = header.Strings.Resolve(reader.ReadUInt16());

            var cellCount = reader.ReadByte();
            var cells = new (int Dq, int Dr)[cellCount];
            for (int c = 0; c < cellCount; c++)
            {
                int dq = reader.ReadSByte();
                int dr = reader.ReadSByte();
                cells[c] = (dq, dr);
            }

            result.Add(new ItemConfig(id, name, desc, rarity,
                partIds.Length > 0 ? partIds : null,
                cellCount > 0 ? cells : null));
        }

        return result;
    }
}
