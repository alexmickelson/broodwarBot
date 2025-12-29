using BWAPI.NET;

namespace MyBotWeb.Services;


public enum WorkerAssignment
{
    Minerals,
    Gas,
    Building
}
public class MyBot
{
    private Game? _game;

    private int targetWorkerCount = 35;
    private UnitType? pendingBuilding = null;
    private int? pendingBuildingTotalCount = null;
    private TilePosition? nextBuildLocation = null;
    public Dictionary<int, (WorkerAssignment Assignment, int? TargetId)> WorkerAssignments = new Dictionary<int, (WorkerAssignment, int?)>();

    public List<UnitType> BuildQueue { get; } = new List<UnitType> // start with 9 probes
    {
        UnitType.Protoss_Probe,
        UnitType.Protoss_Probe,
        UnitType.Protoss_Pylon,
        UnitType.Protoss_Probe,
        UnitType.Protoss_Gateway,
        UnitType.Protoss_Probe,
        UnitType.Protoss_Cybernetics_Core,
        UnitType.Protoss_Probe,
        UnitType.Protoss_Zealot,
    };

    public void OnStart(Game game)
    {
        _game = game;

    }

    public void OnEnd(bool isWinner)
    {
    }

    public void OnFrame()
    {
        if (_game == null) return;
        // BuildWorkers(_game);
        HandleBuildOrder(_game);
        AssignWorkers(_game);
    }

    private void HandleBuildOrder(Game game)
    {
        var nextBuildings = BuildQueue.Where(u => u.IsBuilding());
        if (nextBuildings.Any())
        {
            assignBuildLocationNear(game, nextBuildings.FirstOrDefault(), game.Self().GetStartLocation());

        }

        // Check if we have a pending building and if it has started construction
        if (pendingBuilding != null)
        {
            var buildingCountOfPendingType = game.GetAllUnits()
                .Count(u => u.GetPlayer() == game.Self() && u.GetUnitType() == pendingBuilding);

            if (buildingCountOfPendingType < pendingBuildingTotalCount)
            {
                game.DrawTextScreen(5, 5, $"Waiting for construction of {pendingBuilding} to start...");
                return;
            }
            else
            {
                pendingBuilding = null;
                pendingBuildingTotalCount = null;
                BuildQueue.RemoveAt(0);
            }
        }
        if (!BuildQueue.Any())
        {
            return;
        }


        var nextUnit = BuildQueue.FirstOrDefault();
        game.DrawTextScreen(5, 5, $"Next Unit: {nextUnit}");
        var nextUnitMineralCost = nextUnit.MineralPrice();
        var currentMinerals = game.Self().Minerals();
        if (nextUnitMineralCost > currentMinerals)
        {
            game.DrawTextScreen(5, 20, $"{nextUnit} {currentMinerals}/{nextUnitMineralCost} minerals");
            return;
        }


        var buildCommand = nextUnit.IsBuilding() switch
        {
            _ when nextUnit.IsBuilding() => getBuildBuildingCommand(game, nextUnit),
            _ => getUnitTrainCommand(game, nextUnit),
        };

        if (buildCommand == null)
        {
            return;
        }

        buildCommand();
    }

    private Func<bool>? getBuildBuildingCommand(Game game, UnitType nextBuilding)
    {
        var miningWorkers = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self()
            && u.GetUnitType().IsWorker());
        var workerToBuild = miningWorkers.FirstOrDefault();
        if (workerToBuild == null)
        {
            game.DrawTextScreen(5, 120, $"No available worker to build {nextBuilding}");
            return null;
        }

        assignBuildLocationNear(game, nextBuilding, game.Self().GetStartLocation());
        if (nextBuildLocation == null)
        {
            game.DrawTextScreen(5, 120, $"No valid build location found for {nextBuilding}");
            return null;
        }
        return () =>
        {
            var result = workerToBuild.Build(nextBuilding, (TilePosition)nextBuildLocation);
            if (!result)
            {
                Console.WriteLine($"Failed to issue build command for {nextBuilding} at {nextBuildLocation}");
                game.DrawTextScreen(5, 120, $"Failed to issue build command for {nextBuilding} at {nextBuildLocation}");
            }
            else
            {
                WorkerAssignments[workerToBuild.GetID()] = (WorkerAssignment.Building, null);
                pendingBuilding = nextBuilding;
                pendingBuildingTotalCount = game.GetAllUnits().Count(u => u.GetPlayer() == game.Self() && u.GetUnitType() == nextBuilding) + 1;
                nextBuildLocation = null;
            }
            return result;
        };
    }

    private void assignBuildLocationNear(Game game, UnitType buildingType, TilePosition nearPosition)
    {
        if (nextBuildLocation != null && !game.CanBuildHere((TilePosition)nextBuildLocation, buildingType)) // something happened since assignment
        {
            nextBuildLocation = null;
        }
        var possibleLocations = BuildingLocationUtils.MarkAndGetPossibleBuildLocations(game, buildingType, nearPosition);
        if (possibleLocations.Any() && nextBuildLocation == null)
        {
            nextBuildLocation = possibleLocations[possibleLocations.Count / 2];
        }
        if (nextBuildLocation != null)
        {
            Position pixelPos = new Position(nextBuildLocation.Value.X * 32, nextBuildLocation.Value.Y * 32);
            game.DrawBoxMap(
                pixelPos.X,
                pixelPos.Y,
                pixelPos.X + buildingType.TileWidth() * 32,
                pixelPos.Y + buildingType.TileHeight() * 32,
                Color.Green
            );
        }
    }

    private Func<bool>? getUnitTrainCommand(Game game, UnitType nextUnit)
    {
        var idleBuldings = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.IsIdle());
        var (typeToBuild, typeToBuildId) = nextUnit.WhatBuilds();
        var builder = idleBuldings.FirstOrDefault(b => b.GetUnitType() == typeToBuild);
        if (builder == null)
        {
            game.DrawTextScreen(5, 120, $"No available ({typeToBuild}, {typeToBuildId}) for {nextUnit}");
            return null;
        }
        return () =>
        {
            var result = builder.Train(nextUnit);
            if (result)
            {
                BuildQueue.RemoveAt(0);
            }
            else
            {
                game.DrawTextScreen(5, 120, $"Failed to train {nextUnit} from {builder.GetUnitType()}");
            }
            return result;
        };
    }
    private void AssignWorkers(Game game)
    {
        var workers = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsWorker()).ToList();

        RemoveDeadWorkersFromAssignments(workers);

        // Assign workers to assimilators that need them
        var assimilators = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && 
                   u.GetUnitType() == UnitType.Protoss_Assimilator && 
                   u.IsCompleted())
            .ToList();

        foreach (var assimilator in assimilators)
        {
            var workersOnThisGas = WorkerAssignments.Values
                .Count(a => a.Assignment == WorkerAssignment.Gas && a.TargetId == assimilator.GetID());

            if (workersOnThisGas < 3)
            {
                var neededWorkers = 3 - workersOnThisGas;
                var mineralWorkers = workers
                    .Where(w => WorkerAssignments.TryGetValue(w.GetID(), out var assignment) && 
                           assignment.Assignment == WorkerAssignment.Minerals)
                    .OrderBy(w => w.GetPosition().GetDistance(assimilator.GetPosition()))
                    .Take(neededWorkers)
                    .ToList();

                foreach (var worker in mineralWorkers)
                {
                    worker.Gather(assimilator);
                    WorkerAssignments[worker.GetID()] = (WorkerAssignment.Gas, assimilator.GetID());
                }
            }
        }

        foreach (var unit in workers.Where(u => u.IsIdle()))
        {
            AssignWorkerToMinerals(game, unit);
        }

        var unassignedWorkers = workers.Where(u => !WorkerAssignments.ContainsKey(u.GetID())).ToList();

        foreach (var unit in unassignedWorkers)
        {
            AssignWorkerToMinerals(game, unit);
        }
    }

    private void RemoveDeadWorkersFromAssignments(List<Unit> workers)
    {
        var validWorkerIds = workers.Select(w => w.GetID()).ToHashSet();
        var deadWorkerIds = WorkerAssignments.Keys.Where(id => !validWorkerIds.Contains(id)).ToList();
        foreach (var deadId in deadWorkerIds)
        {
            WorkerAssignments.Remove(deadId);
        }
    }

    private void AssignWorkerToMinerals(Game game, Unit worker)
    {
        var workersPerMineral = WorkerAssignments.Values
            .Where(a => a.Assignment == WorkerAssignment.Minerals && a.TargetId.HasValue)
            .GroupBy(a => a.TargetId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var nearestMineral = game.GetMinerals()
            .Where(m => workersPerMineral.GetValueOrDefault(m.GetID(), 0) < 3)
            .OrderBy(m => m.GetPosition().GetDistance(worker.GetPosition()))
            .FirstOrDefault();

        if (nearestMineral != null)
        {
            worker.Gather(nearestMineral);
            WorkerAssignments[worker.GetID()] = (WorkerAssignment.Minerals, nearestMineral.GetID());
        }
    }


    public void OnUnitComplete(Unit unit)
    {
    }

    public void OnUnitDestroy(Unit unit)
    {
    }

    public void OnUnitMorph(Unit unit)
    {
    }

    public void OnSendText(string text)
    {
    }

    public void OnReceiveText(Player player, string text)
    {
    }

    public void OnPlayerLeft(Player player)
    {
    }

    public void OnNukeDetect(Position target)
    {
    }

    public void OnUnitEvade(Unit unit)
    {
    }

    public void OnUnitShow(Unit unit)
    {
    }

    public void OnUnitHide(Unit unit)
    {
    }

    public void OnUnitCreate(Unit unit)
    {
    }

    public void OnUnitRenegade(Unit unit)
    {
    }

    public void OnSaveGame(string gameName)
    {
    }

    public void OnUnitDiscover(Unit unit)
    {
    }
}
