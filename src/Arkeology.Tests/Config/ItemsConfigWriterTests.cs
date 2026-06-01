using System.Collections.Generic;
using System.IO;
using Xunit;
using Arkeology.Production.Client;

namespace Arkeology.Tests.Config;

public class ItemsConfigWriterTests
{
    private static IReadOnlyList<ItemConfig> RoundTrip(ItemsConfigWriter writer)
    {
        var bytes = writer.Build();
        return new ItemsConfigReader().Read(new MemoryStream(bytes));
    }

    // --- empty ---

    [Fact]
    public void Build_NoItems_CanBeReadAsEmptyList()
    {
        var items = RoundTrip(new ItemsConfigWriter());
        Assert.Empty(items);
    }

    // --- single item fields ---

    [Fact]
    public void Build_SingleItem_RoundTripsId()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1234, "Sword", "A blade.", Rarity.Common));
        Assert.Equal(1234, RoundTrip(writer)[0].Id);
    }

    [Fact]
    public void Build_SingleItem_RoundTripsName()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "Ancient Vase", "A fragile pot.", Rarity.Rare));
        Assert.Equal("Ancient Vase", RoundTrip(writer)[0].Name);
    }

    [Fact]
    public void Build_SingleItem_RoundTripsDescription()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "Sword", "A very sharp blade.", Rarity.Common));
        Assert.Equal("A very sharp blade.", RoundTrip(writer)[0].Description);
    }

    [Theory]
    [InlineData(Rarity.Common)]
    [InlineData(Rarity.Uncommon)]
    [InlineData(Rarity.Rare)]
    [InlineData(Rarity.Epic)]
    [InlineData(Rarity.Legendary)]
    public void Build_SingleItem_RoundTripsRarity(Rarity rarity)
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "X", "Y", rarity));
        Assert.Equal(rarity, RoundTrip(writer)[0].Rarity);
    }

    // --- multiple items ---

    [Fact]
    public void Build_MultipleItems_RoundTripsAll()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "Sword", "A blade.", Rarity.Common));
        writer.Add(new ItemConfig(1001, "Shield", "A buckler.", Rarity.Uncommon));
        writer.Add(new ItemConfig(1002, "Helm", "A helmet.", Rarity.Rare));
        var items = RoundTrip(writer);
        Assert.Equal(3, items.Count);
        Assert.Equal(1000, items[0].Id);
        Assert.Equal(1001, items[1].Id);
        Assert.Equal(1002, items[2].Id);
    }

    // --- partial items ---

    [Fact]
    public void Build_PartialItem_RoundTripsIsPartial()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "Part A", "desc", Rarity.Common));
        writer.Add(new ItemConfig(1001, "Part B", "desc", Rarity.Common));
        writer.Add(new ItemConfig(10000, "Composite", "A whole.", Rarity.Rare, [1000, 1001]));
        Assert.True(RoundTrip(writer)[2].IsPartial);
    }

    [Fact]
    public void Build_PartialItem_RoundTripsPartIds()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "Part A", "desc", Rarity.Common));
        writer.Add(new ItemConfig(1001, "Part B", "desc", Rarity.Common));
        writer.Add(new ItemConfig(10000, "Composite", "A whole.", Rarity.Rare, [1000, 1001]));
        var composite = RoundTrip(writer)[2];
        Assert.Equal(2, composite.Parts!.Count);
        Assert.Equal(1000, composite.Parts[0]);
        Assert.Equal(1001, composite.Parts[1]);
    }

    // --- string tokenization round-trip ---

    // Helpers that encode/decode a single description string end-to-end.
    private static string RoundTripString(string s)
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1, "X", s, Rarity.Common));
        return RoundTrip(writer)[0].Description;
    }

    [Fact]
    public void Tokenize_ComplexString_RoundTrips()
    {
        // Adapted from the design example; spaces before no-space tokens (,  :) are
        // normalized away on encode — "back , similar : yes" becomes "back, similar: yes".
        const string s = "This, a test string: This.should return back, similar: yes (test + test2) \" -- hello";
        Assert.Equal(s, RoundTripString(s));
    }

    [Fact]
    public void Tokenize_EmbeddedDot_StaysAsOneToken()
    {
        // "This.should" has a dot between letters — kept as a single user token, not split.
        const string s = "This.should round-trip intact";
        Assert.Equal(s, RoundTripString(s));
    }

    [Fact]
    public void Tokenize_PredefinedArticle_RoundTrips()
    {
        // "The" and "a" hit the predefined normal token table rather than becoming user tokens.
        const string s = "The ancient artifact, a relic.";
        Assert.Equal(s, RoundTripString(s));
    }

    // --- string deduplication ---

    [Fact]
    public void Build_SharedString_ProducesOneToken()
    {
        var writer = new ItemsConfigWriter();
        writer.Add(new ItemConfig(1000, "Sword", "shared", Rarity.Common));
        writer.Add(new ItemConfig(1001, "Shield", "shared", Rarity.Common));
        var items = RoundTrip(writer);
        Assert.Equal("shared", items[0].Description);
        Assert.Equal("shared", items[1].Description);
    }
}
