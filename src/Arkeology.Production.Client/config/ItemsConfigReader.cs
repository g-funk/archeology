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

            var shapeW = reader.ReadByte();
            var shapeH = reader.ReadByte();
            bool[] cells = Array.Empty<bool>();
            if (shapeW * shapeH > 0)
            {
                int bitCount = shapeW * shapeH;
                int byteCount = (bitCount + 7) / 8;
                var bitmapBytes = reader.ReadBytes(byteCount);
                cells = new bool[bitCount];
                for (int b = 0; b < bitCount; b++)
                    cells[b] = (bitmapBytes[b / 8] & (0x80 >> (b % 8))) != 0;
            }

            result.Add(new ItemConfig(id, name, desc, rarity,
                partIds.Length > 0 ? partIds : null, shapeW, shapeH, cells));
        }

        return result;
    }
}
