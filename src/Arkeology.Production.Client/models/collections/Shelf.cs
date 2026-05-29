using System.Collections.Generic;

namespace Arkeology.Production.Client;

public class Shelf
{
    public IReadOnlyList<Item> Items { get; }

    public Shelf(IReadOnlyList<Item> items)
    {
        Items = items;
    }
}
