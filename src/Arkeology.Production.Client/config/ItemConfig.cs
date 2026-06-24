using System;
using System.Collections.Generic;

namespace Arkeology.Production.Client;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

public class ItemConfig
{
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Rarity Rarity { get; }
    public IReadOnlyList<int>? Parts { get; }
    // Cube-coordinate offsets (dq, dr) relative to the item's anchor cell.
    // Empty for partial items (those with Parts). Convert to odd-r offset grid:
    //   col = dq + (dr - (dr & 1)) / 2,  row = dr
    public IReadOnlyList<(int Dq, int Dr)> CubeOffsets { get; }

    public bool IsPartial => Parts is { Count: > 0 };
    public bool HasShape  => CubeOffsets.Count > 0;

    public ItemConfig(int id, string name, string description, Rarity rarity,
        IReadOnlyList<int>? parts = null,
        IReadOnlyList<(int Dq, int Dr)>? cubeOffsets = null)
    {
        Id          = id;
        Name        = name;
        Description = description;
        Rarity      = rarity;
        Parts       = parts;
        CubeOffsets = cubeOffsets ?? Array.Empty<(int, int)>();
    }
}
