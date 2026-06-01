using System;
using System.Collections.Generic;
using System.Text;

namespace Arkeology.Production.Client;

public class StringTable
{
    // Versioned contract between encoder and decoder — never written to binary.
    // Changing any entry requires a version bump.
    private static readonly Dictionary<ushort, string> PredefinedTokens = new()
    {
        // no-space (0–999): punctuation
        [2] = ",",
        [3] = ".",
        [4] = "!",
        [5] = "?",
        [6] = ":",
        [7] = ";",
        [8] = ")",
        // normal (1000–1999): common words
        [1000] = "The",
        [1001] = "the",
        [1002] = "A",
        [1003] = "a",
        [1004] = "An",
        [1005] = "an",
        [1006] = "\n",
    };

    private readonly string[] _userTokens;
    private readonly ushort[][] _tokenLists;

    public StringTable(string[] userTokens, ushort[][] tokenLists)
    {
        _userTokens = userTokens;
        _tokenLists = tokenLists;
    }

    public string Resolve(ushort ptr)
    {
        if (ptr < 2000)
            return ResolvePredefined(ptr);
        if (ptr < 20000)
            return _userTokens[ptr - 2000];
        return ResolveList(ptr - 20000);
    }

    private static string ResolvePredefined(ushort id)
    {
        if (!PredefinedTokens.TryGetValue(id, out var token))
            throw new InvalidOperationException(
                $"Unknown predefined token ID {id}. " +
                "Predefined token tables are version-locked — ensure encoder and decoder versions match.");
        return token;
    }

    private string ResolveList(int index)
    {
        var ids = _tokenLists[index];
        var sb = new StringBuilder();
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (i > 0 && id >= 1000)
                sb.Append(' ');
            sb.Append(Resolve(id));
        }
        return sb.ToString();
    }
}
