using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Debugging;
using AiCup22.Model;
using AiCup22.Strategy;

namespace AiCup22;

public class MyStrategy
{
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
            if (!_unitStrategies.ContainsKey(unit.Id))
            {
                _unitStrategies.Add(unit.Id, new UnitStrategy(unit.Id));
            }

            var unitStrategy = _unitStrategies[unit.Id];
            unitStrategy.State = StrategyState.RandomMove;
            Debug.DrawText(debugInterface, unit.Position, unit.ShieldPotions.ToString());

            ActionOrder action = null;

            if (unit.Action == null &&
                unit.Shield > _constants.MaxShield / 3 &&
                unit.Weapon != null &&
                unit.Ammo[unit.Weapon.Value] > 0)
            {
                MyUnit enemy = _context.Enemies.Values
                    .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
                    .FirstOrDefault();

                if (enemy != default)
                {
                    Vec2 projectile = unit.Position;
                    Vec2 enemyPosition = enemy.Position;

                    bool hit = false;

                    int simulationTicks = 20;
                    double ticksDivider = 10;

                    double projectileSpeed = 30 / ticksDivider;
                    Vec2 projectileV = Calc.VecMultiply(unit.Direction, projectileSpeed);

                    for (int tick = 1; tick <= simulationTicks * ticksDivider; tick++)
                    {
                        var prevProjectile = projectile;
                        projectile = Calc.VecAdd(projectile, projectileV);

                        hit = Calc.IntersectCircleLine(prevProjectile, projectile, enemyPosition,
                            _constants.UnitRadius * 8 / projectileSpeed);

                        if (hit)
                        {
                            Debug.DrawLine(debugInterface, prevProjectile, projectile);
                            Debug.DrawCircle(debugInterface, enemyPosition, _constants.UnitRadius);
                            break;
                        }
                    }

                    if (hit)
                    {
                        action = new ActionOrder.Aim(true);
                    }

                    unitStrategy.MovePosition = enemy.Position;
                    unitStrategy.State = StrategyState.Hunting;
                    unitStrategy.EnemyId = enemy.Id;
                    _unitStrategies[unit.Id] = unitStrategy;
                }
            }

            if (unitStrategy.State != StrategyState.Hunting &&
                unit.ShieldPotions > 0 &&
                unit.Shield <= _constants.MaxShield - _constants.ShieldPerPotion)
            {
                action = new ActionOrder.UseShieldPotion();
            }

            if (unitStrategy.State != StrategyState.Hunting &&
                unit.ShieldPotions < _constants.MaxShieldPotionsInInventory)
            {
                List<int> currentPickedUp = _unitStrategies.Values
                    .Where(s => s.UnitId != unit.Id)
                    .Select(s => s.PickupLootId)
                    .ToList();

                MyLoot potion = _context.Items.Values
                    .Where(i =>
                        i.InZone &&
                        i.Type == MyLootType.ShieldPotion &&
                        !currentPickedUp.Contains(i.Id)
                    )
                    .OrderBy(i => i.DistanceSquaredToMyUnit[unit.Id])
                    .FirstOrDefault();

                if (potion != default)
                {
                    if (potion.InMyUnit[unit.Id] && unit.Action == null)
                    {
                        var potionDiff = unit.ShieldPotions + potion.Amount - _constants.MaxShieldPotionsInInventory;
                        if (potionDiff <= 0)
                        {
                            _context.Items.Remove(potion.Id);
                        }
                        else
                        {
                            var contextItem = _context.Items[potion.Id];
                            contextItem.Amount = potionDiff;
                            _context.Items[potion.Id] = contextItem;
                        }

                        action = new ActionOrder.Pickup(potion.Id);
                    }

                    unitStrategy.MovePosition = potion.Position;
                    unitStrategy.State = StrategyState.PickupPotion;
                    unitStrategy.PickupLootId = potion.Id;
                    _unitStrategies[unit.Id] = unitStrategy;
                }
            }

            var radius = MaxObstaclesRadius + _constants.UnitRadius;
            bool nearPosition = Calc.InsideCircle(unit.Position, unitStrategy.MovePosition, radius);
            if (unitStrategy.State == StrategyState.RandomMove && nearPosition)
            {
                // todo проблема при маленькой зоне, т.к. еду в зону
                var angle = Random.Next(360);
                var moveX = game.Zone.NextCenter.X + game.Zone.NextRadius * Math.Cos(angle);
                var moveY = game.Zone.NextCenter.Y + game.Zone.NextRadius * Math.Sin(angle);
                unitStrategy.MovePosition = new Vec2(moveX, moveY);
                _unitStrategies[unit.Id] = unitStrategy;
            }

            var target = Calc.VecDiff(unit.Position, unitStrategy.MovePosition);
            Debug.DrawLine(debugInterface, unit.Position, unitStrategy.MovePosition);

            // todo рэндомить движение при обсчете столкновения + нужно вычислять новую поближе к цели!
            var pathTarget = FindPath(debugInterface, unit, target, unitStrategy.MovePosition);

            orders.Add(
                unit.Id,
                new UnitOrder(pathTarget, target, action)
            );
        }

        return new Order(orders);
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

        int simulationTicks = 30;
        double ticksDivider = 10;

        for (int i = 0; i < (fullAngle / simAngle) - 1; i++)
        {
            var angle = simAngle * i;

            bool obstacleCollision = false;
            bool unitCollision = false;
            bool hit = false;

            var newPosition = unit.Position;

            var simVec = Calc.Normalize(target);
            var speedModifier = GetSpeedModifier(unit.Direction, simVec);

            double simSpeed = (_constants.MaxUnitForwardSpeed * speedModifier) / ticksDivider;
            simVec = Calc.VecMultiply(simVec, simSpeed);
            simVec = Calc.Rotate(simVec, angle);

            int tick = 1;
            for (; tick <= simulationTicks * ticksDivider; tick++)
            {
                newPosition = Calc.VecAdd(newPosition, simVec);

                foreach (var projectile in _context.Projectiles.Values)
                {
                    var baseProjectileVelocity = Calc.VecDiv(projectile.Velocity, ticksDivider);

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

                double accuracy = _constants.UnitRadius / ticksDivider;
                bool inPosition = Calc.InsideCircle(newPosition, movePosition, accuracy);
                if (inPosition)
                {
                    break;
                }

                if (tick < ticksDivider / 2)
                {
                    foreach (var obstacle in nearestObstacles)
                    {
                        var r = obstacle.Radius + _constants.UnitRadius;
                        obstacleCollision = Calc.InsideCircle(newPosition, obstacle.Position, r);
                        if (obstacleCollision)
                        {
                            break;
                        }
                    }
                }

                foreach (var myUnit in _context.Units.Values)
                {
                    if (myUnit.Id == unit.Id)
                    {
                        continue;
                    }

                    var r = _constants.UnitRadius + 5 * _constants.UnitRadius;
                    unitCollision = Calc.InsideCircle(newPosition, myUnit.Position, r);
                    if (unitCollision)
                    {
                        break;
                    }
                }

                foreach (var enemy in _context.Enemies.Values)
                {
                    var r = _constants.UnitRadius + 5 * _constants.UnitRadius;
                    unitCollision = Calc.InsideCircle(newPosition, enemy.Position, r);
                    if (unitCollision)
                    {
                        break;
                    }
                }

                if (obstacleCollision || unitCollision)
                {
                    var red = new Color(200, 0, 0, 100);
                    Debug.DrawLine(debugInterface, unit.Position, newPosition, red);
                    break;
                }
            }

            if (!obstacleCollision && !unitCollision && !hit)
            {
                target = simVec;
                target = Calc.VecMultiply(target, ticksDivider);

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