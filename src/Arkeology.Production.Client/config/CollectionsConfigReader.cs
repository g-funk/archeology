using System.Collections.Generic;
using System.IO;

namespace Arkeology.Production.Client;

public class CollectionsConfigReader : ConfigReader<IReadOnlyList<CollectionConfig>>
{
    protected override IReadOnlyList<CollectionConfig> ReadData(BinaryReader reader, ConfigHeader header)
    {
        var count = reader.ReadUInt16();
        var result = new List<CollectionConfig>(count);

        for (int i = 0; i < count; i++)
        {
            var id         = reader.ReadUInt16();
            var name       = header.Strings.Resolve(reader.ReadUInt16());
            var difficulty = reader.ReadByte();
            var shelfCount = reader.ReadByte();

            var shelves = new List<ShelfConfig>(shelfCount);
            for (int s = 0; s < shelfCount; s++)
            {
                var itemCount = reader.ReadByte();
                var itemIds   = new int[itemCount];
                for (int j = 0; j < itemCount; j++)
                    itemIds[j] = reader.ReadInt32();
                shelves.Add(new ShelfConfig(itemIds));
            }

            result.Add(new CollectionConfig(id, name, difficulty, shelves));
        }

        return result;
    }
}
