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
    public int ShapeWidth { get; }
    public int ShapeHeight { get; }
    private readonly bool[] _shapeCells;

    public bool IsPartial => Parts is { Count: > 0 };

    public ItemConfig(int id, string name, string description, Rarity rarity,
        IReadOnlyList<int>? parts = null, int shapeWidth = 0, int shapeHeight = 0, bool[]? shapeCells = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Rarity = rarity;
        Parts = parts;
        ShapeWidth = shapeWidth;
        ShapeHeight = shapeHeight;
        _shapeCells = shapeCells ?? Array.Empty<bool>();
    }

    public bool IsShapeOccupied(int x, int y)
    {
        if (x < 0 || x >= ShapeWidth || y < 0 || y >= ShapeHeight) return false;
        int idx = y * ShapeWidth + x;
        return idx < _shapeCells.Length && _shapeCells[idx];
    }
}
