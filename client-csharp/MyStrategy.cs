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

    private const double MaxObstaclesRadius = 3;
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
            //var brown = new Color(150, 75, 0, 100);
            //Debug.DrawLine(debugInterface, unit.Position, strategy.MovePosition, brown);

            if (unit.RemainingSpawnTime == null &&
                unit.Action == null &&
                unit.Weapon is StaffWeaponType &&
                unit.Ammo[unit.Weapon.Value] > 0 &&
                unit.Shield + unit.Health > _constants.UnitHealth)
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

            if (strategy.State != StrategyState.Hunting &&
                unit.RemainingSpawnTime == null)
            {
                if (unit.Weapon is not StaffWeaponType)
                {
                    action = PickUp(MyLootType.Staff, unit, strategy);
                }
                else if (unit.Weapon is StaffWeaponType &&
                         unit.Ammo[unit.Weapon.Value] < _constants.Weapons[unit.Weapon.Value].MaxInventoryAmmo / 2)
                {
                    action = PickUp(MyLootType.StaffAmmo, unit, strategy);
                }
                else if (unit.ShieldPotions < 3)
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

            var pathTarget = FindPath(debugInterface, unit, target, strategy.MovePosition);

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
                .Take(5)
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

        bool inZone = Calc.InsideCircle(
            strategy.MovePosition, _context.Zone.CurrentCenter, _context.Zone.CurrentRadius
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

    private Vec2 FindPath(DebugInterface debugInterface, MyUnit unit, Vec2 target, Vec2 movePosition)
    {
        List<MyObstacle> nearestObstacles =
            _context.Obstacles.Values
                .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
                .Take(10)
                .ToList();

        double fullAngle = 360;
        double simAngle = 15; // класс для вычисления лучшей позиции со score

        int simulationSec = 3;
        double secDivider = _constants.TicksPerSecond;

        for (int i = 0; i < (fullAngle / simAngle) - 1; i++)
        {
            var angle = simAngle * i;

            bool obstacleCollision = false;
            bool unitCollision = false;
            bool inZone = true;
            bool hit = false;

            var newPosition = unit.Position;

            var simVec = Calc.Normalize(target);
            var speedModifier = GetSpeedModifier(unit.Direction, simVec);

            double simSpeed = unit.RemainingSpawnTime != null
                ? _constants.SpawnMovementSpeed / secDivider
                : (_constants.MaxUnitForwardSpeed * speedModifier) / secDivider;

            simVec = Calc.VecMultiply(simVec, simSpeed);
            simVec = Calc.Rotate(simVec, angle);

            int tick = 1;
            for (; tick <= simulationSec * secDivider; tick++)
            {
                newPosition = Calc.VecAdd(newPosition, simVec);

                foreach (var projectile in _context.Projectiles.Values)
                {
                    var baseProjectileVelocity = Calc.VecDiv(projectile.Velocity, secDivider);

                    Vec2 simProjectileVelocity;
                    Vec2 projectilePosition1;
                    if (tick == 1)
                    {
                        projectilePosition1 = projectile.Position;
                    }
                    else
                    {
                        simProjectileVelocity = Calc.VecMultiply(baseProjectileVelocity, tick - 1);
                        projectilePosition1 = Calc.VecAdd(projectile.Position, simProjectileVelocity);
                    }

                    simProjectileVelocity = Calc.VecMultiply(baseProjectileVelocity, tick);
                    var projectilePosition2 = Calc.VecAdd(projectile.Position, simProjectileVelocity);

                    hit = Calc.IntersectCircleLine(projectilePosition1, projectilePosition2, newPosition,
                        _constants.UnitRadius * 1.1);

                    if (hit)
                    {
                        break;
                    }
                }

                if (hit)
                {
                    var green = new Color(0, 200, 0, 100);
                    Debug.DrawLine(debugInterface, unit.Position, newPosition, green);
                    break;
                }

                double accuracy = _constants.UnitRadius / secDivider;
                bool inPosition = Calc.InsideCircle(newPosition, movePosition, accuracy);
                if (inPosition)
                {
                    break;
                }

                if (tick < secDivider / 2)
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

                if (tick < secDivider)
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

                    inZone = Calc.InsideCircle(
                        newPosition, _context.Zone.CurrentCenter, _context.Zone.CurrentRadius
                    );
                }

                if (obstacleCollision || unitCollision || !inZone)
                {
                    var red = new Color(200, 0, 0, 100);
                    Debug.DrawLine(debugInterface, unit.Position, newPosition, red);
                    break;
                }
            }

            if (!obstacleCollision && !unitCollision && !hit && inZone)
            {
                target = simVec;
                target = Calc.VecMultiply(target, secDivider);

                var blue = new Color(0, 0, 200, 100);
                Debug.DrawLine(debugInterface, unit.Position, newPosition, blue);
                break;
            }
        }

        return target;
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

    public void DebugUpdate(int displayedTick, DebugInterface debugInterface)
    {
        return;
    }

    public void Finish() {}
}