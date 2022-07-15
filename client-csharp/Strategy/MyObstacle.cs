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
    public AiCup22.Model.Vec2 Position { get; set; }

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

    public double DistanceSquaredToMyUnit { get; set; }

    public bool InMyUnit { get; set; }

    public MyObstacle(Obstacle obstacle)
    {
        Id = obstacle.Id;
        Position = obstacle.Position;
        Radius = obstacle.Radius;
        CanSeeThrough = obstacle.CanSeeThrough;
        CanShootThrough = obstacle.CanShootThrough;
        DistanceSquaredToMyUnit = 0;
        InMyUnit = false;
    }
}