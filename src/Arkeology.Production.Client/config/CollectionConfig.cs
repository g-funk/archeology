using System.Collections.Generic;

namespace Arkeology.Production.Client;

public record ShelfConfig(IReadOnlyList<int> ItemIds);

public record CollectionConfig(int Id, string Name, int Difficulty, IReadOnlyList<ShelfConfig> Shelves);
