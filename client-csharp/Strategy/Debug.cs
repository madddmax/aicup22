using AiCup22.Debugging;
using AiCup22.Model;

namespace AiCup22.Strategy;

public static class Debug
{
    private const bool ReleaseMode = false;

    public static void DrawLine(DebugInterface debugInterface, Vec2 p1, Vec2 p2)
    {
        DrawLine(debugInterface, p1, p2, new Color(0, 0, 0, 50));
    }

    public static void DrawLine(DebugInterface debugInterface, Vec2 p1, Vec2 p2, Color color)
    {
        if (ReleaseMode)
        {
            return;
        }

        debugInterface.AddPolyLine(new[] {p1, p2}, 0.1, color);
    }

    public static void DrawText(DebugInterface debugInterface, Vec2 p, string text)
    {
        if (ReleaseMode)
        {
            return;
        }

        debugInterface.AddPlacedText(p, text, new Vec2(1, 1), 1, new Color(0, 0, 200, 50));
    }

    public static void DrawViewPie(DebugInterface debugInterface, Unit unit)
    {
        if (ReleaseMode)
        {
            return;
        }

        var angle = Calc.ToRad(unit.Direction);
        var halfFieldOfView = Calc.ToRad(60);

        var blue = new Color(0, 0, 200, 5);
        debugInterface.AddPie(unit.Position, 50, angle - halfFieldOfView, angle + halfFieldOfView, blue);
    }

    public static void DrawCircle(DebugInterface debugInterface, Vec2 p)
    {
        if (ReleaseMode)
        {
            return;
        }

        debugInterface.AddCircle(p, 0.3, new Color(0, 0, 0, 100));
    }
}