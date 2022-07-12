using System;
using System.Collections.Generic;
using System.Linq;
using AiCup22.Debugging;
using AiCup22.Model;
using AiCup22.Strategy;

namespace AiCup22;

public class MyStrategy
{
    private readonly Constants _constants;
    private Context _context;

    static readonly Random random = new();
    private Vec2 _movePosition = new(0, 0);

    public MyStrategy(Constants constants)
    {
        _constants = constants;
        _context = new Context(constants);
    }

    public Order GetOrder(Game game, DebugInterface debugInterface)
    {
        var orders = new Dictionary<int, UnitOrder>();

        _context.Init(game);

        Vec2 target;
        ActionOrder action = null;

        if (_context.MyUnit.ShieldPotions > 0 &&
            _context.MyUnit.Shield <= _constants.MaxShield - _constants.ShieldPerPotion)
        {
            action = new ActionOrder.UseShieldPotion();
        }

        if (_context.MyUnit.ShieldPotions < _constants.MaxShieldPotionsInInventory)
        {
            MyLoot potion = _context.Items
                .Where(i =>
                    i.InZone &&
                    i.Type == MyLootType.ShieldPotion
                )
                .OrderBy(i => i.DistanceSquaredToMyUnit)
                .FirstOrDefault();

            if (potion != default)
            {
                target = Calc.VecDiff(_context.MyUnit.Position, potion.Position);
                Debug.DrawLine(debugInterface, _context.MyUnit.Position, potion.Position);

                if (potion.InMyUnit)
                {
                    action = new ActionOrder.Pickup(potion.Id);
                }
                else
                {
                    target = Calc.VecMultiply(target, (_constants.MaxUnitForwardSpeed + _constants.MaxUnitBackwardSpeed)/2);
                }

                orders.Add(
                    _context.MyUnit.Id,
                    new UnitOrder(target, target, action)
                );

                return new Order(orders);
            }
        }

        bool nearPosition = Calc.InsideCircle(_context.MyUnit.Position, _movePosition, _constants.UnitRadius);
        if (nearPosition)
        {
            var angle = random.Next(360);
            var moveX = game.Zone.NextCenter.X + game.Zone.NextRadius * Math.Cos(angle);
            var moveY = game.Zone.NextCenter.Y + game.Zone.NextRadius * Math.Sin(angle);
            _movePosition = new Vec2(moveX, moveY);
        }

        target = Calc.VecDiff(_context.MyUnit.Position, _movePosition);
        Debug.DrawLine(debugInterface, _context.MyUnit.Position, _movePosition);

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

        debugInterface.Clear();

        double size = 0.5;
        var alignment = new Vec2(0, 1);

        debugInterface.AddPlacedText(
            _context.MyUnit.Position,
            displayedTick.ToString(),
            alignment,
            size,
            new Debugging.Color(0, 0, 0, 200)
        );
    }

    public void Finish() {}
}