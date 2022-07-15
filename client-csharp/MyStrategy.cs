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
                if (potion.InMyUnit)
                {
                    // todo может остаться ещё amount от предмета // ремовить когда подниму
                    //_context.Items.Remove(potion.Id);
                    action = new ActionOrder.Pickup(potion.Id);
                }

                _movePosition = potion.Position;
                randomMove = false;
            }
        }

        List<MyObstacle> nearestObstacles =
            _context.Obstacles.Values
                .OrderBy(i => i.DistanceSquaredToMyUnit)
                .Take(10)
                .ToList();

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
        for (int i = 0; i < 24; i++)
        {
            var newMyPosition = _context.MyUnit.Position;

            bool insideObstacle = false;
            var simulationVec = Calc.Normalize(target);
            simulationVec = Calc.VecMultiply(simulationVec, _constants.MaxUnitForwardSpeed / 10);
            simulationVec = Calc.Rotate(simulationVec, 15 * i);

            // _constants.TicksPerSecond
            for (int halfTick = 0; halfTick < 2 * 10; halfTick++)
            {
                newMyPosition = Calc.VecAdd(newMyPosition, simulationVec);

                bool nearPosition2 = Calc.InsideCircle(newMyPosition, _movePosition, _constants.UnitRadius/4);
                if (nearPosition2)
                {
                    break;
                }

                foreach (var obstacle in nearestObstacles)
                {
                    var r = obstacle.Radius + _constants.UnitRadius;
                    insideObstacle = Calc.InsideCircle(newMyPosition, obstacle.Position, r);
                    if (insideObstacle)
                    {
                        break;
                    }
                }

                if (insideObstacle)
                {
                    var red = new Color(200, 0, 0, 100);
                    Debug.DrawLine(debugInterface, _context.MyUnit.Position, newMyPosition, red);
                    break;
                }
            }

            if (!insideObstacle)
            {
                // todo считать скорость для достижения точки
                target = simulationVec;
                break;
            }
        }


        target = Calc.VecMultiply(target, _constants.MaxUnitForwardSpeed);

        orders.Add(
            _context.MyUnit.Id,
            new UnitOrder(target, target, action)
        );

        return new Order(orders);
    }

    public void DebugUpdate(int displayedTick, DebugInterface debugInterface)
    {
        return;
    }

    public void Finish() {}
}