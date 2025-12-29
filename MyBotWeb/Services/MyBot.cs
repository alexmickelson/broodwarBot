using BWAPI.NET;

namespace MyBotWeb.Services;

public enum UnitAssignment
{
    Minerals,
    Gas,
    Building,
    Attacking,
}

public record UnitAssignmentDetail(
    UnitAssignment Assignment,
    int? TargetId = null,
    Position? TargetPosition = null
);

public class MyBot
{
    private Game? _game;
    public Dictionary<int, (UnitAssignment Assignment, int? TargetId)> WorkerAssignments = new();
    public Dictionary<int, UnitAssignmentDetail> UnitAssignments = new();
    public BotBuildOrder BuildOrder;
    public Position MilitaryPoint { get; set; } = Position.None;
    public Position MoveMinimapPoint { get; set; } = Position.None;

    public MyBot()
    {
        BuildOrder = new BotBuildOrder(WorkerAssignments);
    }

    public void OnStart(Game game)
    {
        _game = game;
    }

    public void OnEnd(bool isWinner) { }

    public void OnFrame()
    {
        if (_game == null)
            return;
        BuildOrder.OnFrame(_game);
        AssignWorkers(_game);
        AssignUnits(_game);
        if (MoveMinimapPoint != Position.None)
        {
            // Center the screen on the target position (screen is 640x480, so center is at offset -320, -240)
            _game.SetScreenPosition(MoveMinimapPoint.X - 320, MoveMinimapPoint.Y - 240);
            MoveMinimapPoint = Position.None;
        }
    }

    private void AssignWorkers(Game game)
    {
        var workers = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && u.GetUnitType().IsWorker())
            .ToList();

        RemoveDeadWorkersFromAssignments(workers);

        var assimilators = game.GetAllUnits()
            .Where(u =>
                u.GetPlayer() == game.Self()
                && u.GetUnitType() == UnitType.Protoss_Assimilator
                && u.IsCompleted()
            )
            .ToList();

        foreach (var assimilator in assimilators)
        {
            var workersOnThisGas = WorkerAssignments.Values.Count(a =>
                a.Assignment == UnitAssignment.Gas && a.TargetId == assimilator.GetID()
            );

            if (workersOnThisGas < 3)
            {
                var neededWorkers = 3 - workersOnThisGas;
                var mineralWorkers = workers
                    .Where(w =>
                        WorkerAssignments.TryGetValue(w.GetID(), out var assignment)
                        && assignment.Assignment == UnitAssignment.Minerals
                    )
                    .OrderBy(w => w.GetPosition().GetDistance(assimilator.GetPosition()))
                    .Take(neededWorkers)
                    .ToList();

                foreach (var worker in mineralWorkers)
                {
                    worker.Gather(assimilator);
                    WorkerAssignments[worker.GetID()] = (UnitAssignment.Gas, assimilator.GetID());
                }
            }
        }

        foreach (var unit in workers.Where(u => u.IsIdle()))
        {
            AssignWorkerToMinerals(game, unit);
        }

        var unassignedWorkers = workers
            .Where(u => !WorkerAssignments.ContainsKey(u.GetID()))
            .ToList();

        foreach (var unit in unassignedWorkers)
        {
            AssignWorkerToMinerals(game, unit);
        }
    }

    private void AssignUnits(Game game)
    {
        var nonWorkerUnits = game.GetAllUnits()
            .Where(u => u.GetPlayer() == game.Self() && !u.GetUnitType().IsWorker())
            .ToList();
        RemoveDeadUnitsFromAssignments(nonWorkerUnits);

        foreach (var unit in nonWorkerUnits)
        {
            UnitAssignments[unit.GetID()] = new UnitAssignmentDetail(
                Assignment: UnitAssignment.Attacking,
                TargetId: null,
                TargetPosition: MilitaryPoint
            );
        }

        foreach (var unit in nonWorkerUnits)
        {
            var assignmentDetail = UnitAssignments[unit.GetID()];
            switch (assignmentDetail.Assignment)
            {
                case UnitAssignment.Attacking:
                    if (assignmentDetail.TargetPosition == null)
                        break;
                    var targetPosition = (Position)assignmentDetail.TargetPosition;

                    if (unit.GetPosition().GetDistance(targetPosition) < 64)
                    {
                        // Stand and attack nearby enemies
                        if (!unit.IsIdle())
                        {
                            break;
                        }
                        var nearbyEnemies = game.GetAllUnits()
                            .Where(e =>
                                e.GetPlayer() != game.Self()
                                && !e.GetPlayer().IsNeutral()
                                && e.GetPosition().GetDistance(unit.GetPosition())
                                    < unit.GetUnitType().SightRange()
                            )
                            .OrderBy(e => e.GetUnitType().IsBuilding())
                            .ThenBy(e => e.GetPosition().GetDistance(unit.GetPosition()))
                            .FirstOrDefault();

                        if (nearbyEnemies != null)
                        {
                            unit.Attack(nearbyEnemies);
                        }
                    }
                    else
                    {
                        // Check if unit is under attack - defend yourself!
                        var attackingEnemies = game.GetAllUnits()
                            .Where(e =>
                                e.GetPlayer() != game.Self()
                                && !e.GetPlayer().IsNeutral()
                                &&
                                //    e.GetOrderTarget() == unit &&
                                e.GetPosition().GetDistance(unit.GetPosition())
                                    < unit.GetUnitType().SightRange()
                            )
                            .OrderBy(e => e.GetUnitType().IsBuilding())
                            .ThenBy(e => e.GetPosition().GetDistance(unit.GetPosition()))
                            .ThenBy(e => e.GetHitPoints())
                            .FirstOrDefault();

                        var inRangeEnemies = game.GetAllUnits()
                            .Where(e =>
                                e.GetPlayer() != game.Self()
                                && !e.GetPlayer().IsNeutral()
                                && e.GetPosition().GetDistance(unit.GetPosition())
                                    < (unit.GetUnitType().GroundWeapon().MaxRange() + 10)
                            )
                            .OrderBy(e => e.GetUnitType().IsBuilding())
                            .ThenBy(e => e.GetPosition().GetDistance(unit.GetPosition()))
                            .ThenBy(e => e.GetHitPoints())
                            .FirstOrDefault();

                        if (attackingEnemies != null)
                        {
                            if (unit.GetOrderTarget() != attackingEnemies)
                            {
                                unit.Attack(attackingEnemies);
                            }
                        }
                        if (inRangeEnemies != null)
                        {
                            if (unit.GetOrderTarget() != inRangeEnemies)
                            {
                                unit.Attack(inRangeEnemies);
                            }
                        }
                        else if (
                            unit.IsIdle()
                            || (
                                unit.GetOrderTarget() == null
                                && unit.GetTargetPosition() != targetPosition
                            )
                        )
                        {
                            unit.Attack(targetPosition);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void RemoveDeadWorkersFromAssignments(List<Unit> workers)
    {
        var validWorkerIds = workers.Select(w => w.GetID()).ToHashSet();
        var deadWorkerIds = WorkerAssignments
            .Keys.Where(id => !validWorkerIds.Contains(id))
            .ToList();
        foreach (var deadId in deadWorkerIds)
        {
            WorkerAssignments.Remove(deadId);
        }
    }

    private void RemoveDeadUnitsFromAssignments(List<Unit> units)
    {
        var validUnitIds = units.Select(u => u.GetID()).ToHashSet();
        var deadUnitIds = UnitAssignments.Keys.Where(id => !validUnitIds.Contains(id)).ToList();
        foreach (var deadId in deadUnitIds)
        {
            UnitAssignments.Remove(deadId);
        }
    }

    private void AssignWorkerToMinerals(Game game, Unit worker)
    {
        var workersPerMineral = WorkerAssignments
            .Values.Where(a => a.Assignment == UnitAssignment.Minerals && a.TargetId.HasValue)
            .GroupBy(a => a.TargetId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var nearestMineral = game.GetMinerals()
            .Where(m => workersPerMineral.GetValueOrDefault(m.GetID(), 0) < 3)
            .OrderBy(m => m.GetPosition().GetDistance(worker.GetPosition()))
            .FirstOrDefault();

        if (nearestMineral != null)
        {
            worker.Gather(nearestMineral);
            WorkerAssignments[worker.GetID()] = (UnitAssignment.Minerals, nearestMineral.GetID());
        }
    }

    public void OnUnitComplete(Unit unit) { }

    public void OnUnitDestroy(Unit unit) { }

    public void OnUnitMorph(Unit unit) { }

    public void OnSendText(string text) { }

    public void OnReceiveText(Player player, string text) { }

    public void OnPlayerLeft(Player player) { }

    public void OnNukeDetect(Position target) { }

    public void OnUnitEvade(Unit unit) { }

    public void OnUnitShow(Unit unit) { }

    public void OnUnitHide(Unit unit) { }

    public void OnUnitCreate(Unit unit) { }

    public void OnUnitRenegade(Unit unit) { }

    public void OnSaveGame(string gameName) { }

    public void OnUnitDiscover(Unit unit) { }
}
