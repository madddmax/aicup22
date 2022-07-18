using System.Collections.Generic;
using AiCup22.Model;

namespace AiCup22.Strategy;

public struct MyObstacle
{
    /// <summary>
    /// Unique id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Center position
    /// </summary>
    public Vec2 Position { get; set; }

    /// <summary>
    /// Obstacle's radius
    /// </summary>
    public double Radius { get; set; }

    /// <summary>
    /// Whether units can see through this obstacle, or it blocks the view
    /// </summary>
    public bool CanSeeThrough { get; set; }

    /// <summary>
    /// Whether projectiles can go through this obstacle
    /// </summary>
    public bool CanShootThrough { get; set; }

    public Dictionary<int, double> DistanceSquaredToMyUnit { get; set; }

    public Dictionary<int, bool> InMyUnit { get; set; }

    public MyObstacle(Obstacle obstacle)
    {
        Id = obstacle.Id;
        Position = obstacle.Position;
        Radius = obstacle.Radius;
        CanSeeThrough = obstacle.CanSeeThrough;
        CanShootThrough = obstacle.CanShootThrough;
        DistanceSquaredToMyUnit = new Dictionary<int, double>();
        InMyUnit = new Dictionary<int, bool>();
    }
}