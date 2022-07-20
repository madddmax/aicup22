using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22.Strategy;

public struct UnitStrategy
{
    public int UnitId { get; set; }
    public StrategyState State { get; set; }
    public Vec2 MovePosition { get; set; }
    public List<int> AreaPickUpIds { get; set; }

    public UnitStrategy(int unitId)
    {
        UnitId = unitId;
        State = StrategyState.RandomMove;
        MovePosition = new Vec2(0, 0);
        AreaPickUpIds = new List<int>();
    }
}