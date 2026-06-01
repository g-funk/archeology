using System;
using Xunit;
using Arkeology.Production.Client;

namespace Arkeology.Tests.Config;

public class StringTableTests
{
    private static StringTable Empty() => new([], []);
    private static StringTable WithTokens(string[] tokens, ushort[][]? lists = null) => new(tokens, lists ?? []);

    // --- single-token resolution ---

    [Fact]
    public void Resolve_UserToken_ReturnsString()
    {
        var t = WithTokens(["artifact"]);
        Assert.Equal("artifact", t.Resolve(2000));
    }

    [Fact]
    public void Resolve_SecondUserToken_UsesCorrectIndex()
    {
        var t = WithTokens(["first", "second"]);
        Assert.Equal("second", t.Resolve(2001));
    }

    [Fact]
    public void Resolve_PredefinedNoSpace_ReturnsPunctuation()
    {
        Assert.Equal(".", Empty().Resolve(3));
        Assert.Equal(",", Empty().Resolve(2));
    }

    [Fact]
    public void Resolve_PredefinedNormal_ReturnsWord()
    {
        Assert.Equal("The", Empty().Resolve(1000));
    }

    // --- token list resolution (space rule) ---

    [Fact]
    public void Resolve_TokenList_SpecExample()
    {
        // "This, is an example. The next part."
        // from CONFIG_STRINGS.md
        var tokens = new[] { "This", "is", "an", "example", "next", "part" };
        var lists = new ushort[][] { [2000, 2, 2001, 2002, 2003, 3, 1000, 2004, 2005, 3] };
        var t = new StringTable(tokens, lists);

        Assert.Equal("This, is an example. The next part.", t.Resolve(20000));
    }

    [Fact]
    public void Resolve_TokenList_FirstTokenGetsNoLeadingSpace()
    {
        var t = WithTokens(["word"], new ushort[][] { [2000] });
        Assert.Equal("word", t.Resolve(20000));
    }

    [Fact]
    public void Resolve_TokenList_NoSpaceTokenAttachesWithoutSpace()
    {
        // "end." — user token then no-space period
        var t = WithTokens(["end"], new ushort[][] { [2000, 3] });
        Assert.Equal("end.", t.Resolve(20000));
    }

    [Fact]
    public void Resolve_TokenList_NormalTokensGetSpaceBetweenThem()
    {
        var t = WithTokens(["foo", "bar"], new ushort[][] { [2000, 2001] });
        Assert.Equal("foo bar", t.Resolve(20000));
    }

    // --- error cases ---

    [Fact]
    public void Resolve_UnknownPredefinedToken_ThrowsWithId()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Empty().Resolve(999));
        Assert.Contains("999", ex.Message);
    }
}
