using System.Collections.Generic;
using System.IO;

namespace Arkeology.Production.Client;

public class MapsConfigReader : ConfigReader<IReadOnlyList<MapConfig>>
{
    protected override IReadOnlyList<MapConfig> ReadData(BinaryReader reader, ConfigHeader header)
    {
        var mapCount = reader.ReadUInt16();
        var result = new List<MapConfig>(mapCount);

        for (int m = 0; m < mapCount; m++)
        {
            var id = reader.ReadUInt16();
            var width = reader.ReadByte();
            var height = reader.ReadByte();
            var name = header.Strings.Resolve(reader.ReadUInt16());
            var desc = header.Strings.Resolve(reader.ReadUInt16());

            var layerCount = reader.ReadByte();
            var layers = new List<MapLayerConfig>(layerCount);
            for (int l = 0; l < layerCount; l++)
            {
                var infoByte = reader.ReadByte();
                if (infoByte == 1)
                {
                    var tileBytes = reader.ReadBytes(width * height);
                    var tiles = new TileType[width * height];
                    for (int t = 0; t < tiles.Length; t++)
                        tiles[t] = (TileType)tileBytes[t];
                    layers.Add(new MapLayerConfig(isRandom: false, tiles));
                }
                else
                {
                    layers.Add(new MapLayerConfig(isRandom: true));
                }
            }

            var shapeCount = reader.ReadByte();
            var shapes = new List<MapShapeConfig>(shapeCount);
            for (int s = 0; s < shapeCount; s++)
            {
                var itemId = reader.ReadUInt16();
                var layer = reader.ReadByte();
                var x = reader.ReadByte();
                var y = reader.ReadByte();
                shapes.Add(new MapShapeConfig(itemId, layer, x, y));
            }

            var scrapCount = reader.ReadByte();
            result.Add(new MapConfig(id, width, height, name, desc, layers, shapes, scrapCount));
        }

        return result;
    }
}
