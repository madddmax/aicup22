using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Strategy;

public static class Debug
{
    public static void DrawLine(DebugInterface debugInterface, Vec2 p1, Vec2 p2)
    {
        debugInterface.AddPolyLine(new[] {p1, p2}, 0.1, new Color(0, 0, 0, 50));
    }
}