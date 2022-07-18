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
                _unitStrategies.Add(unit.Id, new UnitStrategy());
            }

            var unitStrategy = _unitStrategies[unit.Id];
            ActionOrder action = null;

            if (unit.ShieldPotions > 0 &&
                unit.Shield <= _constants.MaxShield - _constants.ShieldPerPotion)
            {
                action = new ActionOrder.UseShieldPotion();
            }

            foreach (var item in _context.Items.Values)
            {
                Debug.DrawCircle(debugInterface, item.Position);
            }

            Debug.DrawText(debugInterface, unit.Position, unit.ShieldPotions.ToString());

            unitStrategy.State = StrategyState.RandomMove;

            if (unit.ShieldPotions < _constants.MaxShieldPotionsInInventory)
            {
                MyLoot potion = _context.Items.Values
                    .Where(i =>
                        i.InZone &&
                        i.Type == MyLootType.ShieldPotion
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

            bool insideObstacle = false;
            bool hit = false;
            var newMyPosition = unit.Position;

            var simulationVec = Calc.Normalize(target);
            var speedModifier = GetSpeedModifier(unit.Direction, simulationVec);
            var speedModifier2 = GetSpeedModifier(unit.Velocity, simulationVec);

            double simSpeed = (_constants.MaxUnitForwardSpeed * speedModifier * speedModifier2) / ticksDivider;
            simulationVec = Calc.VecMultiply(simulationVec, simSpeed);
            simulationVec = Calc.Rotate(simulationVec, angle);


            int dividedTick = 1;
            for (; dividedTick <= simulationTicks * ticksDivider; dividedTick++)
            {
                newMyPosition = Calc.VecAdd(newMyPosition, simulationVec);

                foreach (var projectile in _context.Projectiles.Values)
                {
                    var baseProjectileVelocity = Calc.VecDiv(projectile.Velocity, ticksDivider);

                    Vec2 simProjectileVelocity;
                    Vec2 projectilePosition1;
                    if (dividedTick == 1)
                    {
                        projectilePosition1 = projectile.Position;
                    }
                    else
                    {
                        simProjectileVelocity = Calc.VecMultiply(baseProjectileVelocity, dividedTick - 1);
                        projectilePosition1 = Calc.VecAdd(projectile.Position, simProjectileVelocity);
                    }

                    simProjectileVelocity = Calc.VecMultiply(baseProjectileVelocity, dividedTick);
                    var projectilePosition2 = Calc.VecAdd(projectile.Position, simProjectileVelocity);

                    hit = Calc.IntersectCircleLine(projectilePosition1, projectilePosition2, newMyPosition,
                        _constants.UnitRadius * 1.1);

                    if (hit)
                    {
                        break;
                    }
                }

                if (hit)
                {
                    var green = new Color(0, 200, 0, 100);
                    Debug.DrawLine(debugInterface, unit.Position, newMyPosition, green);
                    break;
                }

                double accuracy = _constants.UnitRadius / ticksDivider;
                bool inPosition = Calc.InsideCircle(newMyPosition, movePosition, accuracy);
                if (inPosition)
                {
                    break;
                }

                if (dividedTick < ticksDivider / 2)
                {
                    foreach (var obstacle in nearestObstacles)
                    {
                        var r = obstacle.Radius + _constants.UnitRadius;
                        insideObstacle = Calc.InsideCircle(newMyPosition, obstacle.Position, r);
                        if (insideObstacle)
                        {
                            break;
                        }
                    }
                }

                if (insideObstacle)
                {
                    var red = new Color(200, 0, 0, 100);
                    Debug.DrawLine(debugInterface, unit.Position, newMyPosition, red);
                    break;
                }
            }

            if (!insideObstacle && !hit)
            {
                target = simulationVec;
                target = Calc.VecMultiply(target, simSpeed * dividedTick);

                var blue = new Color(0, 0, 200, 100);
                Debug.DrawLine(debugInterface, unit.Position, newMyPosition, blue);
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

        var reminder = angleBetween % 180 / 180;
        if (reminder < 0.5)
        {
            return 1 - reminder;
        }

        return reminder;
    }

    public void DebugUpdate(int displayedTick, DebugInterface debugInterface)
    {
        return;
    }

    public void Finish() {}
}