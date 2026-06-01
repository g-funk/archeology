using System.IO;
using System.Text;

namespace Arkeology.Tests.Config;

static class BinaryHelpers
{
    internal static byte[] Config(
        byte versionMajor, byte versionMinor, long buildTime,
        string[] tokens, ushort[][] tokenLists, byte[] data)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        w.Write(versionMajor);
        w.Write(versionMinor);
        w.Write(buildTime);

        w.Write((ushort)tokens.Length);
        foreach (var token in tokens)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            w.Write((byte)bytes.Length);
            w.Write(bytes);
        }

        w.Write((ushort)tokenLists.Length);
        foreach (var list in tokenLists)
        {
            w.Write((byte)list.Length);
            foreach (var ptr in list)
                w.Write(ptr);
        }

        w.Write(data);
        return ms.ToArray();
    }

    internal static byte[] EmptyConfig(byte major = 1, byte minor = 0, long buildTime = 0)
        => Config(major, minor, buildTime, [], [], []);

    internal static byte[] Item(ushort id, byte rarity, ushort namePtr, ushort descPtr, ushort[]? partIds = null)
    {
        partIds ??= [];
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(id);
        w.Write(rarity);
        w.Write((byte)partIds.Length);
        foreach (var pid in partIds)
            w.Write(pid);
        w.Write(namePtr);
        w.Write(descPtr);

        return ms.ToArray();
    }

    internal static byte[] Concat(params byte[][] arrays)
    {
        var result = new byte[Sum(arrays)];
        var offset = 0;
        foreach (var a in arrays) { a.CopyTo(result, offset); offset += a.Length; }
        return result;
    }

    private static int Sum(byte[][] arrays) { var n = 0; foreach (var a in arrays) n += a.Length; return n; }
}
