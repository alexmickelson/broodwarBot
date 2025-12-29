using BWAPI.NET;

namespace MyBotWeb.Services;

public class MyBot
{
    private Game? _game;

    private int targetWorkerCount = 35;
    private UnitType? pendingBuilding = null;
    private int? pendingBuildingTotalCount = null;
    private TilePosition? nextBuildLocation = null;

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
        MineWorkers(_game);
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
            return; // already assigned
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

    private void BuildWorkers(Game game)
    {
        int workerCount = game.GetAllUnits().Count(u => u.GetUnitType().IsWorker() && u.GetPlayer() == game.Self());
        bool hasEnoughMineralsForAnotherWorker = game.Self().Minerals() >= 50;
        var availableTownHalls = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsResourceDepot()).Where(th => th.IsIdle());
        game.DrawTextScreen(5, 5, $"Workers: {workerCount} / {targetWorkerCount}, {hasEnoughMineralsForAnotherWorker}");

        if (workerCount < targetWorkerCount && hasEnoughMineralsForAnotherWorker && availableTownHalls.Any())
        {
            var selectedTownHall = availableTownHalls.First();
            game.DrawTextScreen(5, 120, $"Selected Town Hall: {selectedTownHall}");
            selectedTownHall?.Train(UnitType.Protoss_Probe);
        }
    }

    private void MineWorkers(Game game)
    {
        var workers = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsWorker());
        foreach (var unit in workers)
        {
            if (unit.IsIdle())
            {
                Unit? closestMineral = null;
                int closestDistance = int.MaxValue;

                foreach (Unit mineral in game.GetMinerals())
                {
                    int distance = unit.GetDistance(mineral);
                    if (distance < closestDistance)
                    {
                        closestMineral = mineral;
                        closestDistance = distance;
                    }
                }

                if (closestMineral != null)
                {
                    unit.Gather(closestMineral);
                }
            }
        }
    }

    public void OnUnitComplete(Unit unit)
    {
        if (_game == null) return;

        // if (unit.GetUnitType().IsWorker())
        // {
        //     Unit? closestMineral = null;
        //     int closestDistance = int.MaxValue;

        //     foreach (Unit mineral in _game.GetMinerals())
        //     {
        //         int distance = unit.GetDistance(mineral);
        //         if (distance < closestDistance)
        //         {
        //             closestMineral = mineral;
        //             closestDistance = distance;
        //         }
        //     }

        //     if (closestMineral != null)
        //     {
        //         unit.Gather(closestMineral);
        //     }
        // }
    }

    public void OnUnitDestroy(Unit unit)
    {
        // Bot logic for unit destruction
    }

    public void OnUnitMorph(Unit unit)
    {
        // Bot logic for unit morphing
    }

    public void OnSendText(string text)
    {
        // Bot logic for text messages
    }

    public void OnReceiveText(Player player, string text)
    {
        // Bot logic for received text
    }

    public void OnPlayerLeft(Player player)
    {
        // Bot logic for player leaving
    }

    public void OnNukeDetect(Position target)
    {
        // Bot logic for nuke detection
    }

    public void OnUnitEvade(Unit unit)
    {
        // Bot logic for unit evasion
    }

    public void OnUnitShow(Unit unit)
    {
        // Bot logic for unit visibility
    }

    public void OnUnitHide(Unit unit)
    {
        // Bot logic for unit hiding
    }

    public void OnUnitCreate(Unit unit)
    {
        // Bot logic for unit creation
    }

    public void OnUnitRenegade(Unit unit)
    {
        // Bot logic for unit renegade
    }

    public void OnSaveGame(string gameName)
    {
        // Bot logic for save game
    }

    public void OnUnitDiscover(Unit unit)
    {
        // Bot logic for unit discovery
    }
}