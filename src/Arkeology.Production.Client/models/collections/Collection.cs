using System.Collections.Generic;
using System.Linq;

namespace Arkeology.Production.Client;

public enum CollectionState : byte { Locked, Unlocked }

public class Collection
{
    public int Id { get; }
    public string Name { get; }
    public CollectionState State { get; set; }
    public int Difficulty { get; }
    public IReadOnlyList<Shelf> Shelves { get; }

    //todo: this allocates unnecessarily
    public bool IsLocked => !Shelves.Any(s => s.Items.Any(i => i.IsDiscovered));

    public IEnumerable<Item> AllItems => Shelves.SelectMany(s => s.Items);

    public Collection(int id, string name, CollectionState state, int difficulty, IReadOnlyList<Shelf> shelves)
    {
        Id = id;
        Name = name;
        State = state;
        Difficulty = difficulty;
        Shelves = shelves;
    }
}
