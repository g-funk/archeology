using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arkeology.Production.Client;

public class ItemsConfigReader : ConfigReader<IReadOnlyList<ItemConfig>>
{
    protected override IReadOnlyList<ItemConfig> ReadData(BinaryReader reader, ConfigHeader header)
    {
        var raws = new List<(int id, Rarity rarity, string name, string desc, ushort[] partIds)>();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var id = reader.ReadUInt16();
            var rarity = (Rarity)reader.ReadByte();
            var partCount = reader.ReadByte();
            var partIds = new ushort[partCount];
            for (var i = 0; i < partCount; i++)
                partIds[i] = reader.ReadUInt16();
            var name = header.Strings.Resolve(reader.ReadUInt16());
            var desc = header.Strings.Resolve(reader.ReadUInt16());
            raws.Add((id, rarity, name, desc, partIds));
        }

        var allIds = new HashSet<int>(raws.Select(r => r.id));

        var result = new List<ItemConfig>(raws.Count);
        foreach (var (id, rarity, name, desc, partIds) in raws)
        {
            if (partIds.Length == 0)
            {
                result.Add(new ItemConfig(id, name, desc, rarity));
                continue;
            }

            var parts = new int[partIds.Length];
            for (var i = 0; i < partIds.Length; i++)
            {
                if (!allIds.Contains(partIds[i]))
                    throw new InvalidOperationException(
                        $"Item {id} references unknown part ID {partIds[i]}.");
                parts[i] = partIds[i];
            }
            result.Add(new ItemConfig(id, name, desc, rarity, parts));
        }

        return result;
    }
}
