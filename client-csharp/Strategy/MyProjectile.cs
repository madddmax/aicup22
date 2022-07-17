using AiCup22.Model;

namespace AiCup22.Strategy;

public struct MyProjectile
{
    /// <summary>
    /// Unique id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Index of the weapon this projectile was shot from (starts with 0)
    /// </summary>
    public int WeaponTypeIndex { get; set; }

    /// <summary>
    /// Id of unit who made the shot
    /// </summary>
    public int ShooterId { get; set; }

    /// <summary>
    /// Id of player (team), whose unit made the shot
    /// </summary>
    public int ShooterPlayerId { get; set; }

    /// <summary>
    /// Current position
    /// </summary>
    public Vec2 Position { get; set; }

    /// <summary>
    /// Projectile's velocity
    /// </summary>
    public Vec2 Velocity { get; set; }

    /// <summary>
    /// Left time of projectile's life
    /// </summary>
    public double LifeTime { get; set; }

    public int CurrentTick { get; set; }

    public MyProjectile(Projectile projectile, int currentTick)
    {
        Id = projectile.Id;
        WeaponTypeIndex = projectile.WeaponTypeIndex;
        ShooterId = projectile.ShooterId;
        ShooterPlayerId = projectile.ShooterPlayerId;
        Position = projectile.Position;
        Velocity = projectile.Velocity;
        LifeTime = projectile.LifeTime;

        CurrentTick = currentTick;
    }
}