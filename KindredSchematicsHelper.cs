using System;
using System.IO;

namespace KindredSchematicsHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Change this path to your KindredSchematics directory
            string folderPath = @"D:\dev\SC\KindredSchematics";

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Directory not found: {folderPath}");
                return;
            }

            Console.WriteLine("Top-level directories:");
            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                Console.WriteLine($"  [DIR]  {Path.GetFileName(dir)}");
            }

            Console.WriteLine("\nTop-level files:");
            foreach (var file in Directory.GetFiles(folderPath))
            {
                Console.WriteLine($"  [FILE] {Path.GetFileName(file)}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        public static int GetEntityTerritoryIndex(Entity entity)
        {
            if (entity.Has<TilePosition>(VAutoCore.EntityManager))
            {
                var pos = entity.Read<TilePosition>(VAutoCore.EntityManager).Tile;
                var territoryIndex = Core.CastleTerritory.GetTerritoryIndexFromTileCoord(pos);
                if (territoryIndex != -1)
                {
                    return territoryIndex;
                }
            }

            if (entity.Has<TileBounds>(VAutoCore.EntityManager))
            {
                var bounds = entity.Read<TileBounds>(VAutoCore.EntityManager).Value;
                for(var x=bounds.Min.x; x<=bounds.Max.x; x++)
                {
                    for(var y=bounds.Min.y; y<=bounds.Max.y; y++)
                    {
                        var territoryIndex = Core.CastleTerritory.GetTerritoryIndexFromTileCoord(new int2(x, y));
                        if (territoryIndex != -1)
                        {
                            return territoryIndex;
                        }
                    }
                }
            }

            if (entity.Has<Translation>(VAutoCore.EntityManager))
            {
                var pos = entity.Read<Translation>(VAutoCore.EntityManager).Value;
                return Core.CastleTerritory.GetTerritoryIndex(pos);
            }

            if (entity.Has<LocalToWorld>(VAutoCore.EntityManager)) 
            {
                var pos = entity.Read<LocalToWorld>(VAutoCore.EntityManager).Position;
                return Core.CastleTerritory.GetTerritoryIndex(pos);
            }

            return -1;
        }
    }
}












