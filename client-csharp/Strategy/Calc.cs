using System;
using AiCup22.Model;

namespace AiCup22.Strategy;

public static class Calc
{
    public static double Distance(Vec2 p1, Vec2 p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));

    public static double DistanceSquared(Vec2 p1, Vec2 p2) => Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2);

    public static bool InsideCircle(Vec2 p, Vec2 c, double r)
    {
        var distanceSquared = DistanceSquared(p, c);
        return distanceSquared <= r * r;
    }

    public static Vec2 VecDiff(Vec2 p1, Vec2 p2)
    {
        var x = p2.X - p1.X;
        var y = p2.Y - p1.Y;

        return new Vec2(x, y);
    }

    public static Vec2 VecMultiply(Vec2 p1, double k)
    {
        return new Vec2(p1.X * k, p1.Y * k);
    }
}