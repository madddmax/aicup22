using AiCup22.Model;

namespace AiCup22.Strategy;

public struct MySound
{
    /// <summary>
    /// Sound type index (starting with 0)
    /// </summary>
    public int TypeIndex { get; set; }

    /// <summary>
    /// Id of unit that heard this sound
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// Position where sound was heard (different from sound source position)
    /// </summary>
    public Vec2 Position { get; set; }

    public MySound(Sound sound)
    {
        TypeIndex = sound.TypeIndex;
        UnitId = sound.UnitId;
        Position = sound.Position;
    }
}