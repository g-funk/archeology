using System;
using System.Collections.Generic;
using System.Linq;

namespace Arkeology.Production.Client;

public class CollectionManager
{
    public event Action<int>? CollectionUnlocked;
    public event Action<int>? ItemDiscovered;

    private List<Collection> _collections = new();
    private Dictionary<int, Item> _itemIndex = new();
    private Dictionary<int, List<Item>> _partOf = new();

    public IReadOnlyList<Collection> Collections => _collections;

    public void LoadCollections(IEnumerable<Collection> collections)
    {
        _collections = collections.ToList();
        _itemIndex.Clear();
        _partOf.Clear();

        foreach (var item in _collections.SelectMany(c => c.AllItems))
            _itemIndex[item.Id] = item;

        foreach (var item in _itemIndex.Values.Where(i => i.IsPartial))
        {
            foreach (var part in item.Parts!)
            {
                if (!_partOf.ContainsKey(part.Id))
                    _partOf[part.Id] = new List<Item>();
                _partOf[part.Id].Add(item);
            }
        }
    }

    public void DiscoverItem(int itemId)
    {
        if (!_itemIndex.TryGetValue(itemId, out var item)) return;
        if (item.IsDiscovered) return;

        item.MarkDiscovered();
        ItemDiscovered?.Invoke(itemId);

        if (_partOf.TryGetValue(itemId, out var parents))
        {
            foreach (var parent in parents.Where(p => p.IsDiscovered))
                ItemDiscovered?.Invoke(parent.Id);
        }
    }
}
