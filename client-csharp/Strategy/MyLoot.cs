using System;
using AiCup22.Model;

namespace AiCup22.Strategy;

public struct MyLoot : IEquatable<MyLoot>
{
    public int Id { get; set; }

    public Vec2 Position { get; set; }

    public MyLootType Type { get; set; }

    public int Amount { get; set; }

    public double DistanceSquaredToZoneCenter { get; set; }

    public bool InZone { get; set; }

    public double DistanceSquaredToMyUnit { get; set; }

    public bool InMyUnit { get; set; }

    public bool Equals(MyLoot other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is MyLoot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static bool operator ==(MyLoot left, MyLoot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MyLoot left, MyLoot right)
    {
        return !left.Equals(right);
    }
}