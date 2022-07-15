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

    public static Vec2 VecAdd(Vec2 p1, Vec2 p2)
    {
        var x = p2.X + p1.X;
        var y = p2.Y + p1.Y;

        return new Vec2(x, y);
    }

    public static Vec2 VecMultiply(Vec2 p1, double k)
    {
        return new Vec2(p1.X * k, p1.Y * k);
    }

    public static double ToRad(Vec2 p)
    {
        return Math.Atan2(p.Y, p.X);
    }

    public static double ToRad(double angle)
    {
        return angle * Math.PI / 180;
    }

    public static bool IsInSector(Vec2 p1, Vec2 p2) {
        var rq0 = p1.X*p1.X + p1.Y*p1.Y;
        var rq = p2.X*p2.X + p2.Y*p2.Y;
        return rq0 >= rq && (p1.X*p2.X + p1.Y*p2.Y)/Math.Sqrt(rq0*rq) >= Math.Cos(60.0/180.0*Math.PI);
    }

    public static Vec2 Normalize(Vec2 p)
    {
        double len = Math.Sqrt(p.X * p.X + p.Y * p.Y);
        return new Vec2(p.X / len, p.Y / len);
    }

    public static Vec2 Rotate(Vec2 p, double angle)
    {
        var a = ToRad(angle);
        var x = p.X * Math.Cos(a) - p.Y * Math.Sin(a);
        var y = p.X * Math.Sin(a) + p.Y * Math.Cos(a);
        return new Vec2(x, y);
    }
}