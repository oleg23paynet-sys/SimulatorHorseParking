namespace HorseParking.Core.Time
{

/// <summary>
/// Source of game time. Implementations can use Unity time, test time, or a simulated clock.
/// </summary>
public interface IGameClock
{
    double ElapsedSeconds { get; }
}
}
