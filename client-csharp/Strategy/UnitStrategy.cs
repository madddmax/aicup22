using AiCup22.Model;

namespace AiCup22.Strategy;

public struct UnitStrategy
{
    public StrategyState State { get; set; }
    public Vec2 MovePosition { get; set; }

    public UnitStrategy()
    {
        State = StrategyState.RandomMove;
        MovePosition = new Vec2(0, 0);
    }
}