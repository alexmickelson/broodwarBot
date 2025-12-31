using System.Collections.Immutable;
using BWAPI.NET;

namespace MyBotWeb.Services;

public static class MilitaryUnitUtils
{
  public static ImmutableDictionary<int, UnitAssignmentDetail> AssignUnits(
    Game game,
    ImmutableDictionary<int, UnitAssignmentDetail> unitAssignments,
    Position militaryPoint
  )
  {
    var nonWorkerUnits = game.GetAllUnits()
      .Where(u => u.GetPlayer() == game.Self() && !u.GetUnitType().IsWorker())
      .ToList();
    unitAssignments = RemoveDeadUnitsFromAssignments(unitAssignments, nonWorkerUnits);

    var builder = unitAssignments.ToBuilder();
    foreach (var unit in nonWorkerUnits)
    {
      builder[unit.GetID()] = new UnitAssignmentDetail(
        Assignment: UnitAssignment.Attacking,
        TargetId: null,
        TargetPosition: militaryPoint
      );
    }
    unitAssignments = builder.ToImmutable();

    foreach (var unit in nonWorkerUnits)
    {
      var assignmentDetail = unitAssignments[unit.GetID()];
      switch (assignmentDetail.Assignment)
      {
        case UnitAssignment.Attacking:
          HandleAttackingUnit(game, unit, assignmentDetail);
          break;
        default:
          break;
      }
    }

    return unitAssignments;
  }

  private static void HandleAttackingUnit(
    Game game,
    Unit unit,
    UnitAssignmentDetail assignmentDetail
  )
  {
    if (assignmentDetail.TargetPosition == null)
      return;
    var targetPosition = (Position)assignmentDetail.TargetPosition;

    if (unit.GetPosition().GetDistance(targetPosition) < 64)
    {
      // Stand and attack nearby enemies
      if (!unit.IsIdle())
      {
        return;
      }
      var nearbyEnemies = game.GetAllUnits()
        .Where(e =>
          e.GetPlayer() != game.Self()
          && !e.GetPlayer().IsNeutral()
          && e.GetPosition().GetDistance(unit.GetPosition()) < unit.GetUnitType().SightRange()
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
      var myUnits = game.GetAllUnits().Where(u => u.GetPlayer() == game.Self()).ToList();

      var attackingEnemies = game.GetAllUnits()
        .Where(e =>
          e.GetPlayer() != game.Self()
          && !e.GetPlayer().IsNeutral()
          && myUnits.Any(myUnit =>
            e.GetPosition().GetDistance(myUnit.GetPosition()) < e.GetUnitType().SightRange()
          )
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
        || (unit.GetOrderTarget() == null && unit.GetTargetPosition() != targetPosition)
      )
      {
        unit.Attack(targetPosition);
      }
    }
  }

  public static ImmutableDictionary<int, UnitAssignmentDetail> RemoveDeadUnitsFromAssignments(
    ImmutableDictionary<int, UnitAssignmentDetail> unitAssignments,
    List<Unit> units
  )
  {
    var validUnitIds = units.Select(u => u.GetID()).ToHashSet();
    return unitAssignments.Where(kvp => validUnitIds.Contains(kvp.Key)).ToImmutableDictionary();
  }
}
