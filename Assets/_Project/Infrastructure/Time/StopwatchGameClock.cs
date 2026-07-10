using System.Diagnostics;
using HorseParking.Core.Time;

namespace HorseParking.Infrastructure.Time
{
    /// <summary>
    /// Simple clock for non-Unity services. A Unity-specific clock may replace it through DI later.
    /// </summary>
    public sealed class StopwatchGameClock : IGameClock
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        public double ElapsedSeconds => stopwatch.Elapsed.TotalSeconds;
    }
}
