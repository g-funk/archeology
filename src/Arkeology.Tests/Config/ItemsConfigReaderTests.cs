using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Arkeology.Production.Client;

namespace Arkeology.Tests.Config;

public class ItemsConfigReaderTests
{
    private static IReadOnlyList<Item> Parse(byte[] bytes)
        => new ItemsConfigReader().Read(new MemoryStream(bytes));

    // token index 0 → ID 2000 (name), index 1 → ID 2001 (desc)
    private static byte[] OneItem(
        ushort id = 1000, byte rarity = 0,
        ushort namePtr = 2000, ushort descPtr = 2001,
        ushort[]? partIds = null)
        => BinaryHelpers.Config(1, 0, 0L,
            ["Sword", "A sharp blade."],
            [],
            BinaryHelpers.Item(id, rarity, namePtr, descPtr, partIds));

    // --- basic parsing ---

    [Fact]
    public void Read_EmptyData_ReturnsEmptyList()
    {
        var items = Parse(BinaryHelpers.EmptyConfig());
        Assert.Empty(items);
    }

    [Fact]
    public void Read_SingleItem_ParsesId()
    {
        var items = Parse(OneItem(id: 1234));
        Assert.Equal(1234, items[0].Id);
    }

    [Fact]
    public void Read_SingleItem_ResolvesName()
    {
        var items = Parse(OneItem());
        Assert.Equal("Sword", items[0].Name);
    }

    [Fact]
    public void Read_SingleItem_ResolvesDescription()
    {
        var items = Parse(OneItem());
        Assert.Equal("A sharp blade.", items[0].Description);
    }

    // --- rarity ---

    [Theory]
    [InlineData(0, Rarity.Common)]
    [InlineData(1, Rarity.Uncommon)]
    [InlineData(2, Rarity.Rare)]
    [InlineData(3, Rarity.Epic)]
    [InlineData(4, Rarity.Legendary)]
    public void Read_SingleItem_MapsRarityCorrectly(byte rarityByte, Rarity expected)
    {
        var items = Parse(OneItem(rarity: rarityByte));
        Assert.Equal(expected, items[0].Rarity);
    }

    // --- multiple items ---

    [Fact]
    public void Read_MultipleItems_ParsesAll()
    {
        var data = BinaryHelpers.Concat(
            BinaryHelpers.Item(1000, 0, 2000, 2001),
            BinaryHelpers.Item(1001, 1, 2001, 2000));
        var bytes = BinaryHelpers.Config(1, 0, 0L, ["first", "second"], [], data);
        var items = Parse(bytes);
        Assert.Equal(2, items.Count);
        Assert.Equal(1000, items[0].Id);
        Assert.Equal(1001, items[1].Id);
    }

    // --- partial items ---

    [Fact]
    public void Read_PartialItem_IsPartial()
    {
        var data = BinaryHelpers.Concat(
            BinaryHelpers.Item(1000, 0, 2000, 2001),           // part A
            BinaryHelpers.Item(1001, 0, 2001, 2000),           // part B
            BinaryHelpers.Item(10000, 1, 2000, 2001, [1000, 1001])); // composite
        var bytes = BinaryHelpers.Config(1, 0, 0L, ["name", "desc"], [], data);
        var items = Parse(bytes);
        var composite = items[2];
        Assert.True(composite.IsPartial);
    }

    [Fact]
    public void Read_PartialItem_WiresPartsByReference()
    {
        var data = BinaryHelpers.Concat(
            BinaryHelpers.Item(1000, 0, 2000, 2001),
            BinaryHelpers.Item(1001, 0, 2001, 2000),
            BinaryHelpers.Item(10000, 1, 2000, 2001, [1000, 1001]));
        var bytes = BinaryHelpers.Config(1, 0, 0L, ["name", "desc"], [], data);
        var items = Parse(bytes);
        var composite = items[2];
        Assert.Equal(2, composite.Parts!.Count);
        Assert.Equal(1000, composite.Parts[0].Id);
        Assert.Equal(1001, composite.Parts[1].Id);
    }

    [Fact]
    public void Read_PartialItemWithMissingPart_Throws()
    {
        var data = BinaryHelpers.Item(10000, 0, 2000, 2001, [9999]); // 9999 not in file
        var bytes = BinaryHelpers.Config(1, 0, 0L, ["name", "desc"], [], data);
        Assert.Throws<InvalidOperationException>(() => Parse(bytes));
    }

    // --- string resolution from token list ---

    [Fact]
    public void Read_ItemWithTokenListName_ResolvesFullString()
    {
        // name points to token list 20000: "Ancient" + "." → "Ancient."
        var tokens = new[] { "Ancient", "desc" };
        var lists = new ushort[][] { [2000, 3] }; // "Ancient."
        var data = BinaryHelpers.Item(1000, 0, 20000, 2001);
        var bytes = BinaryHelpers.Config(1, 0, 0L, tokens, lists, data);
        var items = Parse(bytes);
        Assert.Equal("Ancient.", items[0].Name);
    }
}
