using System.Collections.Generic;
using System.Linq;

namespace Arkeology.Production.Client;

public class StringTableBuilder
{
    // Versioned contract — must stay in sync with StringTable.PredefinedTokens in the decoder.
    // No-space (0–999): attach to preceding token without a leading space.
    private static readonly Dictionary<string, ushort> NoSpaceTokens = new()
    {
        { ",", 2 }, { ".", 3 }, { "!", 4 }, { "?", 5 }, { ":", 6 }, { ";", 7 }, { ")", 8 }
    };

    // Normal (1000–1999): rendered with a leading space.
    private static readonly Dictionary<string, ushort> NormalTokens = new()
    {
        { "The", 1000 }, { "the", 1001 }, { "A", 1002 }, { "a", 1003 },
        { "An", 1004 }, { "an", 1005 }, { "\n", 1006 }
    };

    private static readonly HashSet<char> NoSpaceChars =
        new(NoSpaceTokens.Keys.Where(k => k.Length == 1).Select(k => k[0]));

    private readonly Dictionary<string, ushort> _userTokenIndex = new();
    private readonly List<string> _userTokens = [];

    private readonly Dictionary<string, ushort> _tokenListIndex = new();
    private readonly List<ushort[]> _tokenLists = [];

    public ushort Add(string str)
    {
        var tokens = Tokenize(str);
        return tokens.Length == 1 ? tokens[0] : GetOrAddTokenList(tokens);
    }

    public string[] UserTokens => _userTokens.ToArray();
    public ushort[][] TokenLists => _tokenLists.ToArray();

    private ushort[] Tokenize(string str)
    {
        var result = new List<ushort>();
        foreach (var part in str.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
            TokenizePart(part, result);
        return result.ToArray();
    }

    private void TokenizePart(string word, List<ushort> result)
    {
        if (NoSpaceTokens.TryGetValue(word, out var id)) { result.Add(id); return; }
        if (NormalTokens.TryGetValue(word, out id)) { result.Add(id); return; }

        // Find earliest split: purely alphanumeric prefix + all-no-space-char suffix.
        // Punctuation embedded within a word (e.g. major.minor, e.g., v1.2) → no split, single user token.
        int splitAt = -1;
        for (int i = 1; i < word.Length; i++)
        {
            if (IsAlphanumeric(word, 0, i) && IsAllNoSpaceChars(word, i))
            {
                splitAt = i;
                break;
            }
        }

        if (splitAt > 0)
        {
            result.Add(GetOrAddUserToken(word[..splitAt]));
            for (int i = splitAt; i < word.Length; i++)
                result.Add(NoSpaceTokens[word[i].ToString()]);
        }
        else
        {
            result.Add(GetOrAddUserToken(word));
        }
    }

    private ushort GetOrAddUserToken(string token)
    {
        if (NoSpaceTokens.TryGetValue(token, out var id)) return id;
        if (NormalTokens.TryGetValue(token, out id)) return id;
        if (_userTokenIndex.TryGetValue(token, out id)) return id;
        id = (ushort)(2000 + _userTokens.Count);
        _userTokenIndex[token] = id;
        _userTokens.Add(token);
        return id;
    }

    private ushort GetOrAddTokenList(ushort[] tokens)
    {
        var key = string.Join(",", tokens);
        if (_tokenListIndex.TryGetValue(key, out var id)) return id;
        id = (ushort)(20000 + _tokenLists.Count);
        _tokenListIndex[key] = id;
        _tokenLists.Add(tokens);
        return id;
    }

    private static bool IsAlphanumeric(string s, int start, int end)
    {
        for (int i = start; i < end; i++)
            if (!char.IsLetterOrDigit(s[i])) return false;
        return end > start;
    }

    private static bool IsAllNoSpaceChars(string s, int start)
    {
        for (int i = start; i < s.Length; i++)
            if (!NoSpaceChars.Contains(s[i])) return false;
        return true;
    }
}
