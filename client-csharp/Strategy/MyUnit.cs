using System;
using System.Collections.Generic;
using AiCup22.Model;
using Action = AiCup22.Model.Action;

namespace AiCup22.Strategy;

public struct MyUnit : IEquatable<MyUnit>
{
    /// <summary>
    /// Unique id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Id of the player (team) controlling the unit
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Current health
    /// </summary>
    public double Health { get; set; }

    /// <summary>
    /// Current shield value
    /// </summary>
    public double Shield { get; set; }

    /// <summary>
    /// Left extra lives of this unit
    /// </summary>
    public int ExtraLives { get; set; }

    /// <summary>
    /// Current position of unit's center
    /// </summary>
    public Vec2 Position { get; set; }

    /// <summary>
    /// Remaining time until unit will be spawned, or None
    /// </summary>
    public double? RemainingSpawnTime { get; set; }

    /// <summary>
    /// Current velocity
    /// </summary>
    public Vec2 Velocity { get; set; }

    /// <summary>
    /// Current view direction (vector of length 1)
    /// </summary>
    public Vec2 Direction { get; set; }

    /// <summary>
    /// Value describing process of aiming (0 - not aiming, 1 - ready to shoot)
    /// </summary>
    public double Aim { get; set; }

    /// <summary>
    /// Current action unit is performing, or None
    /// </summary>
    public Action? Action { get; set; }

    /// <summary>
    /// Tick when health regeneration will start (can be less than current game tick)
    /// </summary>
    public int HealthRegenerationStartTick { get; set; }

    /// <summary>
    /// Index of the weapon this unit is holding (starting with 0), or None
    /// </summary>
    public int? Weapon { get; set; }

    /// <summary>
    /// Next tick when unit can shoot again (can be less than current game tick)
    /// </summary>
    public int NextShotTick { get; set; }

    /// <summary>
    /// List of ammo in unit's inventory for every weapon type
    /// </summary>
    public int[] Ammo { get; set; }

    /// <summary>
    /// Number of shield potions in inventory
    /// </summary>
    public int ShieldPotions { get; set; }

    public int CurrentTick { get; set; }

    public Dictionary<int, double> DistanceSquaredToMyUnit { get; set; }

    public MyUnit(Unit unit, int currentTick)
    {
        Id = unit.Id;
        PlayerId = unit.PlayerId;
        Health = unit.Health;
        Shield = unit.Shield;
        ExtraLives = unit.ExtraLives;
        Position = unit.Position;
        RemainingSpawnTime = unit.RemainingSpawnTime;
        Velocity = unit.Velocity;
        Direction = unit.Direction;
        Aim = unit.Aim;
        Action = unit.Action;
        HealthRegenerationStartTick = unit.HealthRegenerationStartTick;
        Weapon = unit.Weapon;
        NextShotTick = unit.NextShotTick;
        Ammo = unit.Ammo;
        ShieldPotions = unit.ShieldPotions;

        CurrentTick = currentTick;
        DistanceSquaredToMyUnit = new Dictionary<int, double>();
    }

    public bool Equals(MyUnit other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is MyUnit other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(MyUnit left, MyUnit right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MyUnit left, MyUnit right)
    {
        return !left.Equals(right);
    }
}
