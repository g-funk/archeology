using System.Collections.Generic;

namespace Arkeology.Production.Client;

public class StringTableBuilder
{
    private readonly Dictionary<string, ushort> _index = new();
    private readonly List<string> _tokens = [];

    public ushort Add(string str)
    {
        if (_index.TryGetValue(str, out var id))
            return id;
        id = (ushort)(2000 + _tokens.Count);
        _index[str] = id;
        _tokens.Add(str);
        return id;
    }

    public string[] Tokens => _tokens.ToArray();
}
