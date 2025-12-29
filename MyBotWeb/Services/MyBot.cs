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
    public Dictionary<int, (WorkerAssignment Assignment, int? TargetId)> WorkerAssignments = new Dictionary<int, (WorkerAssignment, int?)>();
    public BotBuildOrder BuildOrder;

    public MyBot()
    {
        BuildOrder = new BotBuildOrder(WorkerAssignments);
    }

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
        BuildOrder.OnFrame(_game);
        AssignWorkers(_game);
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
