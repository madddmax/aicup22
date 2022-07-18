using AiCup22.Model;

namespace AiCup22.Strategy;

public struct UnitStrategy
{
    public StrategyState State { get; set; } = StrategyState.RandomMove;
    public Vec2 MovePosition { get; set; } = new(0, 0);
}