#nullable enable

using System;

namespace HorseParking.Core.Construction
{
    public enum ConstructionState
    {
        Planned = 0,
        InProgress = 1,
        Completed = 2
    }

    /// <summary>Unity-independent construction state machine driven by elapsed game time.</summary>
    public sealed class ConstructionProject
    {
        public ConstructionProject(ConstructionPlan plan, double durationSeconds)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (durationSeconds <= 0d) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            DurationSeconds = durationSeconds;
        }

        public ConstructionPlan Plan { get; }
        public double DurationSeconds { get; }
        public double ElapsedSeconds { get; private set; }
        public ConstructionState State { get; private set; } = ConstructionState.Planned;
        public double NormalizedProgress => State == ConstructionState.Completed
            ? 1d
            : Math.Min(1d, ElapsedSeconds / DurationSeconds);

        public bool TryStart()
        {
            if (State != ConstructionState.Planned) return false;
            State = ConstructionState.InProgress;
            return true;
        }

        public bool Advance(double deltaSeconds)
        {
            if (deltaSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (State != ConstructionState.InProgress || deltaSeconds <= 0d) return false;

            ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
            if (ElapsedSeconds >= DurationSeconds)
            {
                State = ConstructionState.Completed;
            }

            return true;
        }

        public void Restore(ConstructionState state, double normalizedProgress)
        {
            if (!Enum.IsDefined(typeof(ConstructionState), state))
                throw new ArgumentOutOfRangeException(nameof(state));
            if (double.IsNaN(normalizedProgress) || normalizedProgress < 0d || normalizedProgress > 1d)
                throw new ArgumentOutOfRangeException(nameof(normalizedProgress));
            if (state == ConstructionState.Planned && normalizedProgress != 0d)
                throw new ArgumentException("A planned construction cannot contain progress.", nameof(normalizedProgress));
            if (state == ConstructionState.Completed && normalizedProgress != 1d)
                throw new ArgumentException("A completed construction must contain full progress.", nameof(normalizedProgress));
            if (state == ConstructionState.InProgress && normalizedProgress >= 1d)
                throw new ArgumentException("In-progress construction must be below 100 percent.", nameof(normalizedProgress));

            State = state;
            ElapsedSeconds = state == ConstructionState.Completed
                ? DurationSeconds
                : DurationSeconds * normalizedProgress;
        }
    }
}
