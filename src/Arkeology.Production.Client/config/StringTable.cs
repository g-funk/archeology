using System;
using System.Collections.Generic;
using System.Text;

namespace Arkeology.Production.Client;

public class StringTable
{
    // Predefined tokens: versioned contract between encoder and decoder, never written to binary.
    // Initialized from the embedded defaults in PredefinedTokens.LoadDefault().
    // Call StringTable.Configure(jsonPath) at startup to load from the canonical JSON instead.
    private static Dictionary<ushort, string> _predefinedById;
    private static Dictionary<string, ushort> _predefinedByValue;

    static StringTable()
    {
        (_predefinedById, _predefinedByValue) = PredefinedTokens.LoadDefault();
    }

    // Load predefined tokens from config/json/predefined_tokens.json.
    // Call once at game startup before any config files are read.
    public static void Configure(string jsonPath)
    {
        (_predefinedById, _predefinedByValue) = PredefinedTokens.Load(jsonPath);
    }

    private readonly string[] _userTokens;
    private readonly ushort[][] _tokenLists;

    public StringTable(string[] userTokens, ushort[][] tokenLists)
    {
        _userTokens = userTokens;
        _tokenLists = tokenLists;
    }

    public string Resolve(ushort ptr)
    {
        if (ptr < 2000)  return ResolvePredefined(ptr);
        if (ptr < 20000) return _userTokens[ptr - 2000];
        return ResolveList(ptr - 20000);
    }

    private static string ResolvePredefined(ushort id)
    {
        if (!_predefinedById.TryGetValue(id, out var token))
            throw new InvalidOperationException(
                $"Unknown predefined token ID {id}. " +
                "Predefined token tables are version-locked — ensure encoder and decoder versions match.");
        return token;
    }

    private string ResolveList(int index)
    {
        var ids = _tokenLists[index];
        var sb  = new StringBuilder();
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (i > 0 && id >= 1000)
                sb.Append(' ');
            sb.Append(Resolve(id));
        }
        return sb.ToString();
    }

    public static bool TryGetPredefinedId(string s, out ushort id)
        => _predefinedByValue.TryGetValue(s, out id);

#if DEBUG
    public string[] UserTokens => _userTokens;
#endif
}
