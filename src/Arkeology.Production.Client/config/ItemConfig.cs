using System.Collections.Generic;

namespace Arkeology.Production.Client;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

public class ItemConfig
{
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Rarity Rarity { get; }
    public IReadOnlyList<ItemConfig>? Parts { get; }

    public bool IsPartial => Parts is { Count: > 0 };

    public ItemConfig(int id, string name, string description, Rarity rarity, IReadOnlyList<ItemConfig>? parts = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Rarity = rarity;
        Parts = parts;
    }
}
