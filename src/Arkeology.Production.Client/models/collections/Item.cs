using System.Collections.Generic;
using System.Linq;

namespace Arkeology.Production.Client;

public class Item
{
    public ItemConfig Config { get; }
    public IReadOnlyList<Item>? Parts { get; }

    public bool IsPartial => Config.IsPartial;
    public bool IsDiscovered => IsPartial ? Parts!.All(p => p.IsDiscovered) : _discovered;
    private bool _discovered;

    public Item(ItemConfig config, IReadOnlyList<Item>? parts = null)
    {
        Config = config;
        Parts = parts;
    }

    public void MarkDiscovered() => _discovered = true;
}
