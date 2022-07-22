using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Model;

namespace AiCup22.Strategy;

public class Context
{
    public const int WandWeaponType = 0;
    public const int StaffWeaponType = 1;
    public const int BowWeaponType = 2;

    public const double MaxObstaclesRadius = 6;
    public int AreaPickUp = 15;

    private readonly Constants _constants;

    public readonly Dictionary<int, MyLoot> Items = new();
    public readonly Dictionary<int, MyObstacle> Obstacles;
    public readonly Dictionary<int, MyProjectile> Projectiles = new();
    public readonly Dictionary<int, MyUnit> Enemies = new();
    public readonly Dictionary<int, MyUnit> Units = new();
    public Zone Zone;
    public int CurrentTick;

    public Context(Constants constants)
    {
        _constants = constants;

        Obstacles = new Dictionary<int, MyObstacle>(_constants.Obstacles.Length);
        foreach (var obstacle in _constants.Obstacles)
        {
            Obstacles.Add(obstacle.Id, new MyObstacle(obstacle));
        }
    }

    public void Init(Game game)
    {
        CurrentTick = game.CurrentTick;
        Zone = game.Zone;

        AddOrUpdateUnits(game);

        UpdateDistanceToObstacle();

        AddOrUpdateProjectiles(game);

        AddOrUpdateLoot(game);
    }

    private void AddOrUpdateUnits(Game game)
    {
        RemoveEnemyUnits(game);
        RemoveUnits(game);

        foreach (Unit unit in game.Units)
        {
            var bot = new MyUnit(unit, game.CurrentTick);
            if (bot.PlayerId != game.MyId)
            {
                Enemies[bot.Id] = bot;
            }
            else
            {
                Units[bot.Id] = bot;
            }
        }

        foreach (var enemy in Enemies.Values)
        {
            foreach (var unit in Units.Values)
            {
                var distanceSquaredToMyUnit = Calc.DistanceSquared(enemy.Position, unit.Position);

                distanceSquaredToMyUnit = enemy.Weapon is StaffWeaponType &&
                                          enemy.Ammo[StaffWeaponType] > 0
                    ? distanceSquaredToMyUnit / 2
                    : distanceSquaredToMyUnit;

                distanceSquaredToMyUnit = enemy.Weapon is BowWeaponType &&
                                          enemy.Ammo[BowWeaponType] > 0
                    ? distanceSquaredToMyUnit / 4
                    : distanceSquaredToMyUnit;

                distanceSquaredToMyUnit = enemy.Health + enemy.Shield <= 100
                    ? distanceSquaredToMyUnit / 4
                    : distanceSquaredToMyUnit;

                enemy.DistanceSquaredToMyUnit[unit.Id] = distanceSquaredToMyUnit;
            }

            enemy.AreaPickUpIds.Clear();
            foreach (var item in Items.Values)
            {
                if (Math.Abs(enemy.Position.X - item.Position.X) <= AreaPickUp &&
                    Math.Abs(enemy.Position.Y - item.Position.Y) <= AreaPickUp)
                {
                    enemy.AreaPickUpIds.Add(item.Id);
                }
            }

            Enemies[enemy.Id] = enemy;
        }

        foreach (var unit1 in Units.Values)
        {
            foreach (var unit2 in Units.Values)
            {
                if (unit1.Id == unit2.Id)
                {
                    continue;
                }

                var distanceSquaredToMyUnit = Calc.DistanceSquared(unit1.Position, unit2.Position);
                unit1.DistanceSquaredToMyUnit[unit2.Id] = distanceSquaredToMyUnit;
            }

            Units[unit1.Id] = unit1;
        }
    }

    private void RemoveEnemyUnits(Game game)
    {
        var removedEnemyUnits = new List<int>();
        foreach (var enemy in Enemies.Values)
        {
            if (CurrentTick - enemy.CurrentTick >= 20 * _constants.TicksPerSecond &&
                !removedEnemyUnits.Contains(enemy.Id))
            {
                removedEnemyUnits.Add(enemy.Id);
            }

            foreach (var unit in Units.Values)
            {
                var directionVec = Calc.VecMultiply(unit.Direction, _constants.ViewDistance);

                bool isInSector = Calc.IsInSector(directionVec, Calc.VecDiff(unit.Position, enemy.Position),
                    _constants.FieldOfView);

                if (isInSector)
                {
                    bool disappeared = game.Units.All(i => i.Id != enemy.Id);
                    if (disappeared && !removedEnemyUnits.Contains(enemy.Id))
                    {
                        removedEnemyUnits.Add(enemy.Id);
                    }
                }
            }
        }

        foreach (var removedId in removedEnemyUnits)
        {
            Enemies.Remove(removedId);
        }
    }

    private void RemoveUnits(Game game)
    {
        var removedUnits = new List<int>();
        foreach (var unit in Units.Values)
        {
            if (game.Units.All(u => u.Id != unit.Id))
            {
                removedUnits.Add(unit.Id);
            }
        }

        foreach (var removedId in removedUnits)
        {
            Units.Remove(removedId);
        }
    }

    private void UpdateDistanceToObstacle()
    {
        foreach (MyObstacle obstacle in Obstacles.Values)
        {
            var myObstacle = obstacle;

            foreach (var unit in Units.Values)
            {
                var distanceSquaredToMyUnit = Calc.DistanceSquared(myObstacle.Position, unit.Position);
                myObstacle.DistanceSquaredToMyUnit[unit.Id] = distanceSquaredToMyUnit;
            }

            Obstacles[obstacle.Id] = myObstacle;
        }
    }

    private void AddOrUpdateProjectiles(Game game)
    {
        foreach (var projectile in game.Projectiles)
        {
            var myProjectile = new MyProjectile(projectile, game.CurrentTick);
            Projectiles[projectile.Id] = myProjectile;
        }

        var removedItems = new List<int>();
        foreach (MyProjectile projectile in Projectiles.Values)
        {
            if (projectile.CurrentTick == game.CurrentTick)
            {
                continue;
            }

            var myProjectile = projectile;

            // todo collision with obstacles
            myProjectile.Position = Calc.VecAdd(myProjectile.Position, myProjectile.Velocity);
            int ticksToRemove = (int)Math.Ceiling(myProjectile.LifeTime * _constants.TicksPerSecond) + 5;
            if (projectile.CurrentTick + ticksToRemove >= game.CurrentTick)
            {
                Projectiles[projectile.Id] = myProjectile;
            }
            else
            {
                removedItems.Add(myProjectile.Id);
            }
        }

        foreach (var removedId in removedItems)
        {
            Projectiles.Remove(removedId);
        }
    }

    private void AddOrUpdateLoot(Game game)
    {
        RemoveDisappearedLoot(game);

        foreach (Loot loot in game.Loot)
        {
            var item = new MyLoot(loot);
            Items[item.Id] = item;
        }

        foreach (var item in Items.Values)
        {
            var myLoot = item;

            var distanceSquaredToZoneCenter = Calc.DistanceSquared(myLoot.Position, game.Zone.CurrentCenter);
            myLoot.InZone = distanceSquaredToZoneCenter <= game.Zone.CurrentRadius * game.Zone.CurrentRadius;

            foreach (var unit in Units.Values)
            {
                var distanceSquaredToMyUnit = Calc.DistanceSquared(myLoot.Position, unit.Position);
                myLoot.DistanceSquaredToMyUnit[unit.Id] = distanceSquaredToMyUnit;
                myLoot.InMyUnit[unit.Id] = distanceSquaredToMyUnit <= _constants.UnitRadius * _constants.UnitRadius;
            }

            Items[item.Id] = myLoot;
        }
    }

    private void RemoveDisappearedLoot(Game game)
    {
        var removedItems = new List<int>();
        foreach (var item in Items.Values)
        {
            foreach (var unit in Units.Values)
            {
                var directionVec = Calc.VecMultiply(unit.Direction, _constants.ViewDistance);

                bool isInSector = Calc.IsInSector(directionVec, Calc.VecDiff(unit.Position, item.Position),
                    _constants.FieldOfView);

                if (isInSector)
                {
                    bool disappeared = game.Loot.All(i => i.Id != item.Id);
                    if (disappeared && !removedItems.Contains(item.Id))
                    {
                        removedItems.Add(item.Id);
                    }
                }
            }
        }

        foreach (var removedId in removedItems)
        {
            Items.Remove(removedId);
        }
    }
}