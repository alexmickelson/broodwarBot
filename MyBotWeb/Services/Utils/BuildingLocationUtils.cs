using BWAPI.NET;

namespace MyBotWeb.Services;

public static class BuildingLocationUtils
{
    public static List<TilePosition> MarkAndGetPossibleBuildLocations(
        Game game,
        UnitType buildingType,
        TilePosition nearPosition
    )
    {
        var possibleLocations = new List<TilePosition>();

        // Get nexus and resource locations
        var nexus = game.GetAllUnits()
            .FirstOrDefault(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsResourceDepot());
        if (nexus == null)
            return possibleLocations;

        var resources = game.GetMinerals().Concat(game.GetGeysers()).ToList();
        var existingBuildings = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsBuilding())
            .ToList();

        int maxSearchRadius = 20; // Limit the search radius for performance
        for (int dx = -maxSearchRadius; dx <= maxSearchRadius; dx++)
        {
            for (int dy = -maxSearchRadius; dy <= maxSearchRadius; dy++)
            {
                TilePosition testPosition = new TilePosition(
                    nearPosition.X + dx,
                    nearPosition.Y + dy
                );
                TestAndMarkLocation(
                    game: game,
                    buildingType: buildingType,
                    testPosition: testPosition,
                    nexus: nexus,
                    resources: resources,
                    existingBuildings: existingBuildings,
                    possibleLocations: possibleLocations
                );
            }
        }
        return possibleLocations;
    }

    private static void TestAndMarkLocation(
        Game game,
        UnitType buildingType,
        TilePosition testPosition,
        Unit nexus,
        List<Unit> resources,
        List<Unit> existingBuildings,
        List<TilePosition> possibleLocations
    )
    {
        var isGoodLocation =
            game.CanBuildHere(testPosition, buildingType, builder: null, checkExplored: true)
            && !IsPositionBetweenNexusAndResources(testPosition, nexus, buildingType, resources)
            && IsAwayFromBuildingsAndWalls(game, testPosition, buildingType, existingBuildings);

        if (isGoodLocation)
        {
            possibleLocations.Add(testPosition);
            Position pixelPos = new Position(testPosition.X * 32, testPosition.Y * 32);
            game.DrawBoxMap(
                pixelPos.X,
                pixelPos.Y,
                pixelPos.X + buildingType.TileWidth() * 32,
                pixelPos.Y + buildingType.TileHeight() * 32,
                Color.Grey
            );
        }
    }

    private static bool IsAwayFromBuildingsAndWalls(
        Game game,
        TilePosition buildPos,
        UnitType buildingType,
        List<Unit> existingBuildings
    )
    {
        if (buildingType == UnitType.Protoss_Assimilator)
        {
            return true;
        }

        var existingPylonCount = existingBuildings.Count(b =>
            b.GetUnitType() == UnitType.Protoss_Pylon
        );
        int minBuildingDistance = buildingType switch
        {
            UnitType.Protoss_Pylon => existingPylonCount < 4 ? 5 * 32 : 2 * 32,
            _ => 1 * 32,
        };
        Position buildPixelPos = new Position(buildPos.X * 32, buildPos.Y * 32);

        // Check distance from other buildings
        foreach (var building in existingBuildings)
        {
            if (building.GetDistance(buildPixelPos) < minBuildingDistance)
            {
                return false;
            }
        }

        // Check distance from walls
        int bufferTiles = buildingType switch
        {
            _ => 1,
        };
        int tileWidth = buildingType.TileWidth();
        int tileHeight = buildingType.TileHeight();

        for (int dx = -bufferTiles; dx < tileWidth + bufferTiles; dx++)
        {
            for (int dy = -bufferTiles; dy < tileHeight + bufferTiles; dy++)
            {
                TilePosition checkPos = new TilePosition(buildPos.X + dx, buildPos.Y + dy);

                if (!game.IsBuildable(checkPos))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsPositionBetweenNexusAndResources(
        TilePosition buildPos,
        Unit nexus,
        UnitType buildingType,
        List<Unit> resources
    )
    {
        if (buildingType == UnitType.Protoss_Assimilator)
        {
            return false;
        }
        Position buildPixelPos = new Position(buildPos.X * 32 + 16, buildPos.Y * 32 + 16); // Center of tile
        Position nexusPos = nexus.GetPosition();

        foreach (var resource in resources)
        {
            Position resourcePos = resource.GetPosition();

            // Calculate distances
            double nexusToResource = Math.Sqrt(
                Math.Pow(resourcePos.X - nexusPos.X, 2) + Math.Pow(resourcePos.Y - nexusPos.Y, 2)
            );
            double nexusToBuild = Math.Sqrt(
                Math.Pow(buildPixelPos.X - nexusPos.X, 2)
                    + Math.Pow(buildPixelPos.Y - nexusPos.Y, 2)
            );
            double buildToResource = Math.Sqrt(
                Math.Pow(resourcePos.X - buildPixelPos.X, 2)
                    + Math.Pow(resourcePos.Y - buildPixelPos.Y, 2)
            );

            // If the build position is roughly on the line between nexus and resource (with some tolerance)
            // then nexusToBuild + buildToResource ≈ nexusToResource
            if (Math.Abs((nexusToBuild + buildToResource) - nexusToResource) < 64) // 2 tiles tolerance
            {
                return true;
            }
        }

        return false;
    }
}
