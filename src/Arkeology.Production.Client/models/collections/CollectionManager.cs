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
            IndexItem(item);
    }

    // Build and load collections from binary configs + item lookup.
    public void LoadFrom(IReadOnlyList<CollectionConfig> configs, IReadOnlyDictionary<int, ItemConfig> itemLookup)
    {
        var built = new Dictionary<int, Item>();

        var collections = new List<Collection>(configs.Count);
        foreach (var cfg in configs)
        {
            var shelves = new List<Shelf>(cfg.Shelves.Count);
            foreach (var shelfCfg in cfg.Shelves)
            {
                var items = new List<Item>(shelfCfg.ItemIds.Count);
                foreach (var id in shelfCfg.ItemIds)
                {
                    if (itemLookup.TryGetValue(id, out var itemCfg))
                        items.Add(GetOrBuild(itemCfg, itemLookup, built));
                }
                shelves.Add(new Shelf(items));
            }
            collections.Add(new Collection(cfg.Id, cfg.Name, CollectionState.Locked, cfg.Difficulty, shelves));
        }

        LoadCollections(collections);
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
                ItemDiscovered?.Invoke(parent.Config.Id);
        }

        foreach (var c in _collections.Where(c => c.State == CollectionState.Locked && !c.IsLocked))
        {
            c.State = CollectionState.Unlocked;
            CollectionUnlocked?.Invoke(c.Id);
        }
    }

    private void IndexItem(Item item)
    {
        _itemIndex[item.Config.Id] = item;

        if (!item.IsPartial) return;

        foreach (var part in item.Parts!)
        {
            _itemIndex[part.Config.Id] = part;

            if (!_partOf.ContainsKey(part.Config.Id))
                _partOf[part.Config.Id] = new List<Item>();
            _partOf[part.Config.Id].Add(item);
        }
    }

    private static Item GetOrBuild(ItemConfig cfg, IReadOnlyDictionary<int, ItemConfig> lookup, Dictionary<int, Item> built)
    {
        if (built.TryGetValue(cfg.Id, out var existing)) return existing;

        IReadOnlyList<Item>? parts = null;
        if (cfg.IsPartial)
        {
            var partList = new List<Item>(cfg.Parts!.Count);
            foreach (var partId in cfg.Parts)
            {
                if (lookup.TryGetValue(partId, out var partCfg))
                    partList.Add(GetOrBuild(partCfg, lookup, built));
            }
            parts = partList;
        }

        var item = new Item(cfg, parts);
        built[cfg.Id] = item;
        return item;
    }
}
