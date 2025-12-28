using BWAPI.NET;

namespace MyBot;

public class MyBot
{
    private Game? _game;

    private int targetWorkerCount = 35;

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
        var nextUnit = BuildQueue.FirstOrDefault();
        game.DrawTextScreen(100, 100, $"Next Unit: {nextUnit}");
        var nextUnitMineralCost = nextUnit.MineralPrice();
        var currentMinerals = game.Self().Minerals();
        if (nextUnitMineralCost > currentMinerals)
        {
            game.DrawTextScreen(100, 120, $"Not enough minerals for {nextUnit} (Have: {currentMinerals}, Need: {nextUnitMineralCost})");
            return;
        }
        if (nextUnit.IsBuilding())
        {
            var miningWorkers = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsWorker() && u.IsGatheringMinerals());
            var workerToBuild = miningWorkers.FirstOrDefault();
            if (workerToBuild == null)
            {
                game.DrawTextScreen(100, 120, $"No available worker to build {nextUnit}");
                return;
            }
            workerToBuild.Build(nextUnit);
        }
        else
        {
            var idleBuldings = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.IsIdle());
            var (typeToBuild, typeToBuildId) = nextUnit.WhatBuilds();


            var builder = idleBuldings.FirstOrDefault(b => b.GetUnitType() == typeToBuild);
            if (builder == null)
            {
                game.DrawTextScreen(100, 120, $"No available ({typeToBuild}, {typeToBuildId}) for {nextUnit}");
                return;
            }
            builder.Train(nextUnit);
        }
        BuildQueue.RemoveAt(0);
    }

    private void BuildWorkers(Game game)
    {
        int workerCount = game.GetAllUnits().Count(u => u.GetUnitType().IsWorker() && u.GetPlayer() == game.Self());
        bool hasEnoughMineralsForAnotherWorker = game.Self().Minerals() >= 50;
        var availableTownHalls = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsResourceDepot()).Where(th => th.IsIdle());
        game.DrawTextScreen(100, 100, $"Workers: {workerCount} / {targetWorkerCount}, {hasEnoughMineralsForAnotherWorker}");

        if (workerCount < targetWorkerCount && hasEnoughMineralsForAnotherWorker && availableTownHalls.Any())
        {
            var selectedTownHall = availableTownHalls.First();
            game.DrawTextScreen(100, 120, $"Selected Town Hall: {selectedTownHall}");
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