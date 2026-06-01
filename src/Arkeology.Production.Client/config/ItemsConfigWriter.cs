using System.IO;

namespace Arkeology.Production.Client;

public class ItemsConfigWriter : ConfigWriter<ItemConfig>
{
    protected override void WriteItem(BinaryWriter writer, StringTableBuilder strings, ItemConfig item)
    {
        writer.Write((ushort)item.Id);
        writer.Write((byte)item.Rarity);
        var parts = item.Parts;
        writer.Write((byte)(parts?.Count ?? 0));
        if (parts != null)
            foreach (var pid in parts)
                writer.Write((ushort)pid);
        writer.Write(strings.Add(item.Name));
        writer.Write(strings.Add(item.Description));
    }
}
