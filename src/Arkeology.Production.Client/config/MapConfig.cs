using System.Collections.Generic;

namespace Arkeology.Production.Client;

public class MapLayerConfig
{
    public bool IsRandom { get; }
    public TileType[]? Tiles { get; } // null if random; Width*Height values if provided

    public MapLayerConfig(bool isRandom, TileType[]? tiles = null)
    {
        IsRandom = isRandom;
        Tiles = tiles;
    }
}

public class MapShapeConfig
{
    public int ItemId { get; }
    public int Layer { get; }
    public int X { get; }
    public int Y { get; }

    public MapShapeConfig(int itemId, int layer, int x, int y)
    {
        ItemId = itemId;
        Layer = layer;
        X = x;
        Y = y;
    }
}

public class MapConfig
{
    public int Id { get; }
    public int Width { get; }
    public int Height { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<MapLayerConfig> Layers { get; }
    public IReadOnlyList<MapShapeConfig> Shapes { get; }
    public int ScrapCount { get; }

    public MapConfig(int id, int width, int height, string name, string description,
        IReadOnlyList<MapLayerConfig> layers, IReadOnlyList<MapShapeConfig> shapes, int scrapCount)
    {
        Id = id;
        Width = width;
        Height = height;
        Name = name;
        Description = description;
        Layers = layers;
        Shapes = shapes;
        ScrapCount = scrapCount;
    }
}
