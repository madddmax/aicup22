using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Debugging;
using AiCup22.Model;
using AiCup22.Strategy;

namespace AiCup22;

public class MyStrategy
{
    private const int WandWeaponType = 0;
    private const int StaffWeaponType = 1;
    private const int BowWeaponType = 2;

    private const double MaxObstaclesRadius = 6;
    private static readonly Random Random = new();

    private readonly Constants _constants;
    private readonly Context _context;

    private readonly Dictionary<int, UnitStrategy> _unitStrategies = new();

    public MyStrategy(Constants constants)
    {
        _constants = constants;
        _context = new Context(constants);
    }

    public Order GetOrder(Game game, DebugInterface debugInterface)
    {
        var orders = new Dictionary<int, UnitOrder>();

        _context.Init(game);

        foreach (var unit in _context.Units.Values)
        {
            ActionOrder action = null;
            var strategy = GetStrategy(unit);

            Debug.DrawText(debugInterface, unit.Position, unit.ShieldPotions.ToString());

            if (unit.RemainingSpawnTime == null &&
                unit.Action == null &&
                unit.Weapon is BowWeaponType &&
                unit.Ammo[unit.Weapon.Value] > 0)
                // unit.Shield + unit.Health > _constants.UnitHealth)
            {
                action = Hunting(debugInterface, unit, unit.Weapon.Value, strategy);
            }
            else if(strategy.State == StrategyState.Hunting)
            {
                RandomMove(unit, strategy);
            }
            else
            {
                SetRandomState(unit, strategy);
            }

            if (strategy.State != StrategyState.Hunting)// &&
                // unit.RemainingSpawnTime == null)
            {
                if (unit.Weapon is not BowWeaponType)
                {
                    action = PickUp(MyLootType.Bow, unit, strategy);
                }
                else if (unit.Weapon is BowWeaponType &&
                         unit.Ammo[unit.Weapon.Value] < _constants.Weapons[unit.Weapon.Value].MaxInventoryAmmo / 3)
                {
                    action = PickUp(MyLootType.BowAmmo, unit, strategy);
                }
                else if (unit.ShieldPotions == 0)
                {
                    action = PickUp(MyLootType.ShieldPotion, unit, strategy);
                }
                else if (unit.ShieldPotions > 0 &&
                         unit.Shield <= _constants.MaxShield - _constants.ShieldPerPotion &&
                         (strategy.State != StrategyState.PickUp ||
                          strategy.ApproxTicksDistance > _constants.ShieldPotionUseTime))
                {
                    action = new ActionOrder.UseShieldPotion();
                }
            }

            if (strategy.State == StrategyState.PickUp &&
                unit.RemainingSpawnTime != null)
            {
                RandomMove(unit, strategy);
            }

            RandomMoveIfNeeded(unit, strategy);

            var target = Calc.VecDiff(unit.Position, strategy.MovePosition);
            Debug.DrawLine(debugInterface, unit.Position, strategy.MovePosition);

            var pathTarget = FindPath(debugInterface, unit, target);

            orders.Add(
                unit.Id,
                new UnitOrder(pathTarget, target, action)
            );
        }

        return new Order(orders);
    }

    private UnitStrategy GetStrategy(MyUnit unit)
    {
        if (!_unitStrategies.TryGetValue(unit.Id, out var strategy))
        {
            strategy = new UnitStrategy(unit.Id);
        }

        var unitAverageSpeed = _constants.MaxUnitForwardSpeed + _constants.MaxUnitBackwardSpeed / 2;
        strategy.ApproxTicksDistance = (int)(Calc.Distance(unit.Position, strategy.MovePosition) / unitAverageSpeed);

        _unitStrategies[unit.Id] = strategy;

        return strategy;
    }

    private ActionOrder Hunting(DebugInterface debugInterface, MyUnit unit, int weapon, UnitStrategy unitStrategy)
    {
        ActionOrder action = null;

        MyUnit enemy = _context.Enemies.Values
            .Where(i => i.IsSpawn(_context.CurrentTick, _constants.TicksPerSecond))
            .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
            .FirstOrDefault();

        if (enemy == default)
        {
            SetRandomState(unit, unitStrategy);
            return null;
        }

        List<MyObstacle> canNotShootObstacles =
            _context.Obstacles.Values
                .Where(i => i.CanShootThrough == false)
                .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
                .Take(20)
                .ToList();

        bool hit = false;
        bool obstacleCollision = false;
        bool unitCollision = false;

        Vec2 enemyPosition = enemy.Position;
        Vec2 enemyVelocity = Calc.VecDiv(enemy.Velocity, _constants.TicksPerSecond);

        Vec2 projectile = unit.Position;
        double projectileSpeed = _constants.Weapons[weapon].ProjectileSpeed / _constants.TicksPerSecond;
        Vec2 projectileV = Calc.VecMultiply(unit.Direction, projectileSpeed);

        for (int tick = 1; tick <= _constants.TicksPerSecond - 5; tick++)
        {
            enemyPosition = Calc.VecAdd(enemyPosition, enemyVelocity);

            var prevProjectile = projectile;
            projectile = Calc.VecAdd(projectile, projectileV);

            hit = Calc.IntersectCircleLine(prevProjectile, projectile, enemyPosition,
                _constants.UnitRadius * 2 / projectileSpeed);

            if (hit)
            {
                Debug.DrawLine(debugInterface, prevProjectile, projectile);
                Debug.DrawCircle(debugInterface, enemyPosition, _constants.UnitRadius);
                break;
            }

            foreach (var obstacle in canNotShootObstacles)
            {
                obstacleCollision = Calc.IntersectCircleLine(
                    prevProjectile, projectile, obstacle.Position, obstacle.Radius
                );

                if (obstacleCollision)
                {
                    break;
                }
            }

            foreach (var myUnit in _context.Units.Values)
            {
                if (myUnit.Id == unit.Id || myUnit.RemainingSpawnTime != null)
                {
                    continue;
                }

                unitCollision = Calc.IntersectCircleLine(
                    prevProjectile, projectile, myUnit.Position, _constants.UnitRadius
                );
                if (unitCollision)
                {
                    break;
                }
            }

            if (obstacleCollision || unitCollision)
            {
                break;
            }
        }

        if (hit)
        {
            action = new ActionOrder.Aim(true);
        }

        unitStrategy.MovePosition = enemy.Position;
        unitStrategy.State = StrategyState.Hunting;
        _unitStrategies[unit.Id] = unitStrategy;

        return action;
    }

    private void RandomMoveIfNeeded(MyUnit unit, UnitStrategy strategy)
    {
        var radius = MaxObstaclesRadius + _constants.UnitRadius;
        bool nearRandomPosition = Calc.InsideCircle(unit.Position, strategy.MovePosition, radius);
        if ((strategy.MovePosition.X == 0 && strategy.MovePosition.Y == 0) ||
            (strategy.State == StrategyState.RandomMove && nearRandomPosition))
        {
            RandomMove(unit, strategy);
        }

        double r = Math.Abs(_context.Zone.CurrentRadius - 2 * _constants.UnitRadius);
        bool inZone = Calc.InsideCircle(
            strategy.MovePosition, _context.Zone.CurrentCenter, r
        );
        if (!inZone)
        {
            RandomMove(unit, strategy);
        }
    }

    private void SetRandomState(MyUnit unit, UnitStrategy unitStrategy)
    {
        unitStrategy.State = StrategyState.RandomMove;
        unitStrategy.AreaPickUpIds.Clear();
        _unitStrategies[unit.Id] = unitStrategy;
    }

    private void RandomMove(MyUnit unit, UnitStrategy unitStrategy)
    {
        unitStrategy.State = StrategyState.RandomMove;
        unitStrategy.AreaPickUpIds.Clear();
        unitStrategy.MovePosition = GetRandomMove(_context.Zone);
        _unitStrategies[unit.Id] = unitStrategy;
    }

    private static Vec2 GetRandomMove(Zone zone)
    {
        var angle = Random.Next(360);
        var moveX = zone.NextCenter.X + zone.NextRadius * Math.Cos(angle);
        var moveY = zone.NextCenter.Y + zone.NextRadius * Math.Sin(angle);

        return new Vec2(moveX, moveY);
    }

    private ActionOrder PickUp(MyLootType lootType, MyUnit unit, UnitStrategy strategy)
    {
        ActionOrder action = null;

        List<int> currentPickedUp = _unitStrategies.Values
            .Where(s => s.UnitId != unit.Id)
            .SelectMany(s => s.AreaPickUpIds)
            .ToList();

        MyLoot item = _context.Items.Values
            .Where(i =>
                i.InZone &&
                i.Type == lootType &&
                !currentPickedUp.Contains(i.Id)
            )
            .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
            .FirstOrDefault();

        if (item == default)
        {
            SetRandomState(unit, strategy);
            return null;
        }

        if (item.InMyUnit[unit.Id] && unit.Action == null)
        {
            action = new ActionOrder.Pickup(item.Id);
            _context.Items.Remove(item.Id);

            SetRandomState(unit, strategy);
        }
        else
        {
            foreach (var loot in _context.Items.Values)
            {
                const double pickUpArea = 15;
                if (Math.Abs(loot.Position.X - item.Position.X) <= pickUpArea &&
                    Math.Abs(loot.Position.Y - item.Position.Y) <= pickUpArea)
                {
                    strategy.AreaPickUpIds.Add(loot.Id);
                }
            }

            strategy.State = StrategyState.PickUp;
            strategy.MovePosition = item.Position;
            _unitStrategies[unit.Id] = strategy;
        }

        return action;
    }

    private Vec2 FindPath(DebugInterface debugInterface, MyUnit unit, Vec2 target)
    {
        List<PathResult> pathResults = new List<PathResult>(23);

        List<MyObstacle> nearestObstacles =
            _context.Obstacles.Values
                .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
                .Take(10)
                .ToList();

        double fullAngle = 360;
        double simAngle = 15;

        int simulationSec = 3;
        double ticksPerSecond = _constants.TicksPerSecond;

        for (int i = 0; i < (fullAngle / simAngle) - 1; i++)
        {
            var angle = simAngle * i;

            bool obstacleCollision = false;
            bool unitCollision = false;

            var newPosition = unit.Position;

            var simVec = Calc.Normalize(target);
            var speedModifier = GetSpeedModifier(unit.Direction, simVec);

            double simSpeed = unit.RemainingSpawnTime != null
                ? _constants.SpawnMovementSpeed / ticksPerSecond
                : (_constants.MaxUnitForwardSpeed * speedModifier) / ticksPerSecond;

            simVec = Calc.VecMultiply(simVec, simSpeed);
            simVec = Calc.Rotate(simVec, angle);

            Vec2 velocity = Calc.VecDiv(unit.Velocity, ticksPerSecond);

            List<MyProjectile> simProjectiles = _context.Projectiles.Values.ToList();

            var pathResult = new PathResult
            {
                SimVec = simVec
            };

            int tick = 1;
            for (; tick <= simulationSec * ticksPerSecond; tick++)
            {
                velocity = GetVelocity(velocity, simVec);
                newPosition = Calc.VecAdd(newPosition, velocity);

                if (tick < ticksPerSecond)
                {
                    var projectilesToRemove = new List<MyProjectile>();

                    foreach (MyProjectile projectile in simProjectiles)
                    {
                        var projectileVelocity = Calc.VecDiv(projectile.Velocity, ticksPerSecond);

                        Vec2 simProjectileVelocity;
                        Vec2 prevProjectilePos;
                        if (tick == 1)
                        {
                            prevProjectilePos = projectile.Position;
                        }
                        else
                        {
                            simProjectileVelocity = Calc.VecMultiply(projectileVelocity, tick - 1);
                            prevProjectilePos = Calc.VecAdd(projectile.Position, simProjectileVelocity);
                        }

                        simProjectileVelocity = Calc.VecMultiply(projectileVelocity, tick + 1);
                        var projectilePos = Calc.VecAdd(projectile.Position, simProjectileVelocity);

                        bool hit = Calc.IntersectCircleLine(
                            prevProjectilePos, projectilePos, newPosition, _constants.UnitRadius * 2
                        );

                        if (hit)
                        {
                            var weapon = _constants.Weapons[projectile.WeaponTypeIndex];
                            pathResult.Score -= weapon.ProjectileDamage;
                            projectilesToRemove.Add(projectile);
                        }

                        int ticksToRemove = (int)Math.Ceiling(projectile.LifeTime * _constants.TicksPerSecond) + 1;
                        if (projectile.CurrentTick + ticksToRemove < _context.CurrentTick + tick)
                        {
                            projectilesToRemove.Add(projectile);
                        }
                    }

                    foreach (var projectile in projectilesToRemove)
                    {
                        simProjectiles.Remove(projectile);
                    }

                    bool inZone = Calc.InsideCircle(
                        newPosition, _context.Zone.CurrentCenter, _context.Zone.CurrentRadius
                    );

                    if (!inZone)
                    {
                        pathResult.Score -= _constants.ZoneDamagePerSecond / ticksPerSecond;
                    }
                }

                if (tick < ticksPerSecond / 2)
                {
                    foreach (var obstacle in nearestObstacles)
                    {
                        var r = obstacle.Radius;
                        obstacleCollision = Calc.InsideCircle(newPosition, obstacle.Position, r);
                        if (obstacleCollision)
                        {
                            break;
                        }
                    }
                }

                if (tick < ticksPerSecond)
                {
                    foreach (var myUnit in _context.Units.Values)
                    {
                        if (myUnit.Id == unit.Id)
                        {
                            continue;
                        }

                        var r = _constants.UnitRadius + _constants.UnitRadius;
                        unitCollision = Calc.InsideCircle(newPosition, myUnit.Position, r);
                        if (unitCollision)
                        {
                            break;
                        }
                    }

                    foreach (var enemy in _context.Enemies.Values)
                    {
                        var r = _constants.UnitRadius + _constants.UnitRadius;
                        unitCollision = Calc.InsideCircle(newPosition, enemy.Position, r);
                        if (unitCollision)
                        {
                            break;
                        }
                    }
                }

                if (obstacleCollision || unitCollision)
                {
                    var red = new Color(200, 0, 0, 100);
                    Debug.DrawLine(debugInterface, unit.Position, newPosition, red);
                    break;
                }
            }

            if (!obstacleCollision && !unitCollision)
            {
                pathResults.Add(pathResult);

                var r = Math.Abs(pathResult.Score / 2);
                var green = new Color(r, 200, 0, 100);
                Debug.DrawLine(debugInterface, unit.Position, newPosition, green);
            }
        }

        if (pathResults.Count == 0)
        {
            return target;
        }

        var bestPath = pathResults.OrderByDescending(p => p.Score).First();
        var bestVec = Calc.VecMultiply(bestPath.SimVec, ticksPerSecond);

        var blue = new Color(0, 0, 200, 100);
        Debug.DrawLine(debugInterface, unit.Position, Calc.VecAdd(unit.Position, bestVec), blue, 0.2);

        return bestVec;
    }

    private static double GetSpeedModifier(Vec2 vec, Vec2 simVec)
    {
        var angleBetween = Calc.AngleBetween(vec, simVec);
        if (angleBetween == 0)
        {
            return 1;
        }

        if (Math.Abs(angleBetween - 180) < 0.1)
        {
            return 0.5;
        }

        if (angleBetween > 180)
        {
            angleBetween = 360 - angleBetween;
        }

        return Math.Abs((angleBetween * 0.5 / 180) - 1);
    }

    private static Vec2 GetVelocity(Vec2 velocity, Vec2 simVec)
    {
        double diffLen = Calc.Distance(simVec, velocity);
        if (diffLen <= 1)
        {
            return simVec;
        }

        Vec2 diff = Calc.VecDiff(simVec, velocity);
        var acceleration = new Vec2(diff.X / diffLen, diff.Y / diffLen);

        return Calc.VecAdd(velocity, acceleration);
    }

    public void DebugUpdate(int displayedTick, DebugInterface debugInterface)
    {
        return;
    }

    public void Finish() {}
}