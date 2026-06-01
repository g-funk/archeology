using System.IO;
using Xunit;
using Arkeology.Production.Client;

namespace Arkeology.Tests.Config;

public class ConfigReaderTests
{
    // Test double that discards the data section and returns only the parsed header
    private class HeaderOnly : ConfigReader<ConfigHeader>
    {
        protected override ConfigHeader ReadData(BinaryReader _, ConfigHeader header) => header;
    }

    private static ConfigHeader Parse(byte[] bytes)
        => new HeaderOnly().Read(new MemoryStream(bytes));

    // --- version ---

    [Fact]
    public void Read_ParsesVersionMajor()
    {
        var h = Parse(BinaryHelpers.Config(2, 0, 0L, [], [], []));
        Assert.Equal(2, h.VersionMajor);
    }

    [Fact]
    public void Read_ParsesVersionMinor()
    {
        var h = Parse(BinaryHelpers.Config(1, 7, 0L, [], [], []));
        Assert.Equal(7, h.VersionMinor);
    }

    // --- build time ---

    [Fact]
    public void Read_ParsesBuildTime()
    {
        var h = Parse(BinaryHelpers.Config(1, 0, 1_700_000_000L, [], [], []));
        Assert.Equal(1_700_000_000L, h.BuildTime);
    }

    // --- token table → StringTable ---

    [Fact]
    public void Read_SingleToken_ResolvesByUserTokenId()
    {
        var h = Parse(BinaryHelpers.Config(1, 0, 0L, ["sword"], [], []));
        Assert.Equal("sword", h.Strings.Resolve(2000));
    }

    [Fact]
    public void Read_MultipleTokens_ResolveCorrectIndex()
    {
        var h = Parse(BinaryHelpers.Config(1, 0, 0L, ["alpha", "beta", "gamma"], [], []));
        Assert.Equal("beta", h.Strings.Resolve(2001));
        Assert.Equal("gamma", h.Strings.Resolve(2002));
    }

    [Fact]
    public void Read_Utf8Token_PreservesEncoding()
    {
        var h = Parse(BinaryHelpers.Config(1, 0, 0L, ["épée"], [], []));
        Assert.Equal("épée", h.Strings.Resolve(2000));
    }

    // --- token list table → StringTable ---

    [Fact]
    public void Read_TokenList_ResolvesWithSpaceRule()
    {
        // "sword." — user token "sword" (2000) + predefined "." (3)
        var tokens = new[] { "sword" };
        var lists = new ushort[][] { [2000, 3] };
        var h = Parse(BinaryHelpers.Config(1, 0, 0L, tokens, lists, []));
        Assert.Equal("sword.", h.Strings.Resolve(20000));
    }
}
