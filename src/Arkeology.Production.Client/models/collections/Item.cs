using System.Collections.Generic;
using System.Linq;

namespace Arkeology.Production.Client;

public enum Rarity { Common, Uncommon, Rare }

public class Item
{
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Rarity Rarity { get; }
    public IReadOnlyList<Item>? Parts { get; }

    public bool IsPartial => Parts is { Count: > 0 };
    public bool IsDiscovered => IsPartial ? Parts!.All(p => p.IsDiscovered) : _discovered;

    private bool _discovered;

    public Item(int id, string name, string description, Rarity rarity, IReadOnlyList<Item>? parts = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Rarity = rarity;
        Parts = parts;
    }

    public void MarkDiscovered() => _discovered = true;
}
