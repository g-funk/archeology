using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arkeology.Production.Client;

// Loads predefined tokens from config/json/predefined_tokens.json.
// The JSON is the canonical source of truth shared with the Python encoders.
// Call StringTable.Configure(jsonPath) at startup; otherwise StringTable falls
// back to LoadDefault() which mirrors the JSON and must be kept in sync.
public static class PredefinedTokens
{
    private record TokenEntry(
        [property: JsonPropertyName("id")]    int    Id,
        [property: JsonPropertyName("value")] string Value);

    private record TokenFile(
        [property: JsonPropertyName("no_space")] TokenEntry[] NoSpace,
        [property: JsonPropertyName("normal")]   TokenEntry[] Normal);

    public static (Dictionary<ushort, string> ById, Dictionary<string, ushort> ByValue)
        Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var file = JsonSerializer.Deserialize<TokenFile>(json)!;
        return Build(file.NoSpace, file.Normal);
    }

    // Embedded defaults — must exactly mirror config/json/predefined_tokens.json.
    // Update both together whenever the predefined set changes (version bump required).
    public static (Dictionary<ushort, string> ById, Dictionary<string, ushort> ByValue)
        LoadDefault()
    {
        TokenEntry[] noSpace =
        [
            new(1, " "), new(2, ","), new(3, "."), new(4, "!"), new(5, "?"),
            new(6, ":"), new(7, ";"), new(8, ")"), new(9, "'"),
        ];
        TokenEntry[] normal =
        [
            new(1000, "The"),  new(1001, "the"),  new(1002, "A"),    new(1003, "a"),
            new(1004, "An"),   new(1005, "an"),   new(1006, "\n"),
            new(1007, "is"),   new(1008, "are"),  new(1009, "was"),  new(1010, "were"),
            new(1011, "of"),   new(1012, "in"),   new(1013, "to"),   new(1014, "and"),
            new(1015, "or"),   new(1016, "for"),  new(1017, "with"), new(1018, "that"),
            new(1019, "this"), new(1020, "it"),   new(1021, "from"), new(1022, "("),
        ];
        return Build(noSpace, normal);
    }

    private static (Dictionary<ushort, string>, Dictionary<string, ushort>)
        Build(TokenEntry[] noSpace, TokenEntry[] normal)
    {
        var byId    = new Dictionary<ushort, string>();
        var byValue = new Dictionary<string, ushort>();
        foreach (var e in noSpace.Concat(normal))
        {
            byId[(ushort)e.Id] = e.Value;
            byValue[e.Value]   = (ushort)e.Id;
        }
        return (byId, byValue);
    }
}
