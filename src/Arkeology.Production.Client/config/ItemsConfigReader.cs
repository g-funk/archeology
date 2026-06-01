using System;
using System.Collections.Generic;
using System.IO;

namespace Arkeology.Production.Client;

public class ItemsConfigReader : ConfigReader<IReadOnlyList<Item>>
{
    protected override IReadOnlyList<Item> ReadData(BinaryReader reader, ConfigHeader header)
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

        // Create leaf Item instances for all records
        var byId = new Dictionary<int, Item>(raws.Count);
        foreach (var (id, rarity, name, desc, _) in raws)
            byId[id] = new Item(id, name, desc, rarity);

        // Rebuild partial items with parts wired up
        var result = new List<Item>(raws.Count);
        foreach (var (id, rarity, name, desc, partIds) in raws)
        {
            if (partIds.Length == 0)
            {
                result.Add(byId[id]);
                continue;
            }

            var parts = new Item[partIds.Length];
            for (var i = 0; i < partIds.Length; i++)
            {
                if (!byId.TryGetValue(partIds[i], out var part))
                    throw new InvalidOperationException(
                        $"Item {id} references unknown part ID {partIds[i]}.");
                parts[i] = part;
            }
            result.Add(new Item(id, name, desc, rarity, parts));
        }

        return result;
    }
}
