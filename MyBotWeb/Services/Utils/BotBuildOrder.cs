using BWAPI.NET;

namespace MyBotWeb.Services;

public class BotBuildOrder
{
    private UnitType? _pendingBuilding = null;
    private int? _pendingBuildingTotalCount = null;
    private readonly Dictionary<int, (UnitAssignment Assignment, int? TargetId)> _workerAssignments;

    public List<UnitType> BuildQueue { get; } =
        new List<UnitType> // start with 9 probes
        {
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Pylon,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Gateway,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Cybernetics_Core,
            UnitType.Protoss_Pylon,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Zealot,
            UnitType.Protoss_Assimilator,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Gateway,
            UnitType.Protoss_Gateway,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Gateway,
            UnitType.Protoss_Pylon,
            UnitType.Protoss_Pylon,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Pylon,
            UnitType.Protoss_Zealot,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Dragoon,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Dragoon,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Dragoon,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Probe,
            UnitType.Protoss_Dragoon,
        };

    public Dictionary<UnitType, int> DesiredUnitCounts { get; } =
        new Dictionary<UnitType, int>
        {
            { UnitType.Protoss_Probe, 20 },
            { UnitType.Protoss_Zealot, 5 },
            { UnitType.Protoss_Dragoon, 10 },
        };

    public BotBuildOrder(
        Dictionary<int, (UnitAssignment Assignment, int? TargetId)> workerAssignments
    )
    {
        _workerAssignments = workerAssignments;
    }

    public void OnFrame(Game game)
    {
        var nextBuildings = BuildQueue.Where(u => u.IsBuilding());
        if (nextBuildings.Any())
        {
            BuildingLocationUtils.MarkAndGetPossibleBuildLocations(
                game,
                nextBuildings.FirstOrDefault(),
                game.Self().GetStartLocation()
            );
        }

        if (CheckPendingBuilding(game))
        {
            return;
        }

        if (!BuildQueue.Any())
        {
            AddSomethingToBuildQueue(game);
        }

        var nextUnit = BuildQueue.FirstOrDefault();
        game.DrawTextScreen(5, 5, $"Next Unit: {nextUnit}");
        var nextUnitMineralCost = nextUnit.MineralPrice();
        var currentMinerals = game.Self().Minerals();
        if (nextUnitMineralCost > currentMinerals)
        {
            game.DrawTextScreen(
                5,
                20,
                $"{nextUnit} {currentMinerals}/{nextUnitMineralCost} minerals"
            );
            return;
        }

        var buildCommand = nextUnit.IsBuilding() switch
        {
            _ when nextUnit.IsBuilding() => GetBuildBuildingCommand(game, nextUnit),
            _ => GetUnitTrainCommand(game, nextUnit),
        };

        if (buildCommand == null)
        {
            return;
        }

        buildCommand();
    }

    private bool CheckPendingBuilding(Game game)
    {
        if (_pendingBuilding != null)
        {
            var buildingCountOfPendingType = game.GetAllUnits()
                .Count(u => u.GetPlayer() == game.Self() && u.GetUnitType() == _pendingBuilding);

            if (buildingCountOfPendingType < _pendingBuildingTotalCount)
            {
                game.DrawTextScreen(
                    5,
                    5,
                    $"Waiting for construction of {_pendingBuilding} to start..."
                );
                return true;
            }
            else
            {
                _pendingBuilding = null;
                _pendingBuildingTotalCount = null;
                BuildQueue.RemoveAt(0);
            }
        }

        return false;
    }

    private void AddSomethingToBuildQueue(Game game)
    {
        var supplyAvailable = game.Self().SupplyTotal() - game.Self().SupplyUsed();

        if (supplyAvailable <= 10)
        {
            BuildQueue.Add(UnitType.Protoss_Pylon);
            return;
        }
        var zelotCount = game.GetAllUnits()
            .Count(u => u.GetPlayer() == game.Self() && u.GetUnitType() == UnitType.Protoss_Zealot);
        if (zelotCount < 4)
        {
            BuildQueue.Add(UnitType.Protoss_Zealot);
            return;
        }

        if (game.Self().Gas() > game.Self().Minerals())
        {
            BuildQueue.Add(UnitType.Protoss_Dragoon);
            return;
        }

        BuildQueue.Add(UnitType.Protoss_Zealot);
    }

    private Func<bool>? GetBuildBuildingCommand(Game game, UnitType nextBuilding)
    {
        var miningWorkers = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsWorker());
        var workerToBuild = miningWorkers.FirstOrDefault();
        if (workerToBuild == null)
        {
            game.DrawTextScreen(5, 120, $"No available worker to build {nextBuilding}");
            return null;
        }

        return () =>
        {
            var possibleLocations = BuildingLocationUtils.MarkAndGetPossibleBuildLocations(
                game,
                nextBuilding,
                game.Self().GetStartLocation()
            );
            if (!possibleLocations.Any())
            {
                game.DrawTextScreen(5, 35, $"No valid build location found for {nextBuilding}");
                return false;
            }

            var nextBuildLocation = possibleLocations[possibleLocations.Count / 2];
            Position pixelPos = new Position(nextBuildLocation.X * 32, nextBuildLocation.Y * 32);
            game.DrawBoxMap(
                pixelPos.X,
                pixelPos.Y,
                pixelPos.X + nextBuilding.TileWidth() * 32,
                pixelPos.Y + nextBuilding.TileHeight() * 32,
                Color.Green
            );
            var result = workerToBuild.Build(nextBuilding, (TilePosition)nextBuildLocation);
            if (!result)
            {
                game.DrawTextScreen(
                    5,
                    35,
                    $"Failed to issue build command for {nextBuilding} at {nextBuildLocation}"
                );
            }
            else
            {
                _workerAssignments[workerToBuild.GetID()] = (UnitAssignment.Building, null);
                _pendingBuilding = nextBuilding;
                _pendingBuildingTotalCount =
                    game.GetAllUnits()
                        .Count(u => u.GetPlayer() == game.Self() && u.GetUnitType() == nextBuilding)
                    + 1;
            }
            return result;
        };
    }

    private Func<bool>? GetUnitTrainCommand(Game game, UnitType nextUnit)
    {
        var idleBuldings = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && u.IsIdle());
        var (typeToBuild, typeToBuildId) = nextUnit.WhatBuilds();

        var buildingsThatCanBuild = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && u.GetUnitType() == typeToBuild)
            .OrderBy(b => b.GetTrainingQueueCount());

        if (!buildingsThatCanBuild.Any())
        {
            game.DrawTextScreen(
                5,
                120,
                $"No available ({typeToBuild}, {typeToBuildId}) for {nextUnit}"
            );
            return null;
        }
        return () =>
        {
            var builder = buildingsThatCanBuild.First();
            var result = builder.Train(nextUnit);
            if (result)
            {
                BuildQueue.RemoveAt(0);
            }
            else
            {
                game.DrawTextScreen(
                    5,
                    120,
                    $"Failed to train {nextUnit} from {builder.GetUnitType()}"
                );
            }
            return result;
        };
    }
}
