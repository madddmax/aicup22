using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Debugging;
using AiCup22.Model;
using AiCup22.Strategy;

namespace AiCup22;

public class MyStrategy
{
    private double MaxObstaclesRadius = 3;
    private readonly Constants _constants;
    private Context _context;

    static readonly Random random = new();
    private Vec2 _movePosition = new(0, 0);
    private bool randomMove = false;

    public MyStrategy(Constants constants)
    {
        _constants = constants;
        _context = new Context(constants);
    }

    public Order GetOrder(Game game, DebugInterface debugInterface)
    {
        var orders = new Dictionary<int, UnitOrder>();

        _context.Init(game);

        ////

        //Debug.DrawViewPie(debugInterface, _context.MyUnit);

        ////

        Vec2 target = game.Zone.NextCenter;
        ActionOrder action = null;

        if (_context.MyUnit.ShieldPotions > 0 &&
            _context.MyUnit.Shield <= _constants.MaxShield - _constants.ShieldPerPotion)
        {
            action = new ActionOrder.UseShieldPotion();
        }

        foreach (var item in _context.Items.Values)
        {
            Debug.DrawCircle(debugInterface, item.Position);
        }

        Debug.DrawText(debugInterface, _context.MyUnit.Position, _context.MyUnit.ShieldPotions.ToString());

        randomMove = true;

        if (_context.MyUnit.ShieldPotions < _constants.MaxShieldPotionsInInventory)
        {
            MyLoot potion = _context.Items.Values
                .Where(i =>
                    i.InZone &&
                    i.Type == MyLootType.ShieldPotion
                )
                .OrderBy(i => i.DistanceSquaredToMyUnit)
                .FirstOrDefault();

            if (potion != default)
            {
                if (potion.InMyUnit && _context.MyUnit.Action == null)
                {
                    var potionDiff = _context.MyUnit.ShieldPotions + potion.Amount -
                                     _constants.MaxShieldPotionsInInventory;

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

                _movePosition = potion.Position;
                randomMove = false;
            }
        }

        var radius = MaxObstaclesRadius + _constants.UnitRadius;
        bool nearPosition = Calc.InsideCircle(_context.MyUnit.Position, _movePosition, radius);
        if (randomMove && nearPosition)
        {
            // todo проблема при маленькой зоне, т.к. еду в зону
            var angle = random.Next(360);
            var moveX = game.Zone.NextCenter.X + game.Zone.NextRadius * Math.Cos(angle);
            var moveY = game.Zone.NextCenter.Y + game.Zone.NextRadius * Math.Sin(angle);
            _movePosition = new Vec2(moveX, moveY);
            randomMove = true;
        }

        target = Calc.VecDiff(_context.MyUnit.Position, _movePosition);
        Debug.DrawLine(debugInterface, _context.MyUnit.Position, _movePosition);


        // todo рэндомить движение при обсчете столкновения + нужно вычислять новую поближе к цели!

        var pathTarget = FindPath(debugInterface, target);

        orders.Add(
            _context.MyUnit.Id,
            new UnitOrder(pathTarget, target, action)
        );

        return new Order(orders);
    }

    private Vec2 FindPath(DebugInterface debugInterface, Vec2 target)
    {
        List<MyObstacle> nearestObstacles =
            _context.Obstacles.Values
                .OrderBy(i => i.DistanceSquaredToMyUnit)
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
            var newMyPosition = _context.MyUnit.Position;

            var simulationVec = Calc.Normalize(target);
            var speedModifier = GetSpeedModifier(_context.MyUnit.Direction, simulationVec);
            var speedModifier2 = GetSpeedModifier(_context.MyUnit.Velocity, simulationVec);

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
                    Debug.DrawLine(debugInterface, _context.MyUnit.Position, newMyPosition, green);
                    break;
                }

                bool inPosition = Calc.InsideCircle(newMyPosition, _movePosition,
                    _constants.UnitRadius / ticksDivider);
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
                    Debug.DrawLine(debugInterface, _context.MyUnit.Position, newMyPosition, red);
                    break;
                }
            }

            if (!insideObstacle && !hit)
            {
                target = simulationVec;
                target = Calc.VecMultiply(target, simSpeed * dividedTick);

                var blue = new Color(0, 0, 200, 100);
                Debug.DrawLine(debugInterface, _context.MyUnit.Position, newMyPosition, blue);
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