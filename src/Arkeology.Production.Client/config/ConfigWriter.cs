using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Arkeology.Production.Client;

public abstract class ConfigWriter<T>
{
    private readonly List<T> _items = [];

    public ConfigWriter<T> Add(T item) { _items.Add(item); return this; }

    public byte[] Build()
    {
        var strings = new StringTableBuilder();
        var itemData = BuildItemData(strings);

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        w.Write((byte)1);  // versionMajor
        w.Write((byte)0);  // versionMinor
        w.Write(0L);       // buildTime

        var userTokens = strings.UserTokens;
        w.Write((ushort)userTokens.Length);
        foreach (var token in userTokens)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            w.Write((byte)bytes.Length);
            w.Write(bytes);
        }

        var tokenLists = strings.TokenLists;
        w.Write((ushort)tokenLists.Length);
        foreach (var list in tokenLists)
        {
            w.Write((byte)list.Length);
            foreach (var tokenId in list)
                w.Write(tokenId);
        }

        w.Write(itemData);

        return ms.ToArray();
    }

    protected abstract void WriteItem(BinaryWriter writer, StringTableBuilder strings, T item);

    private byte[] BuildItemData(StringTableBuilder strings)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        foreach (var item in _items)
            WriteItem(w, strings, item);
        return ms.ToArray();
    }
}
