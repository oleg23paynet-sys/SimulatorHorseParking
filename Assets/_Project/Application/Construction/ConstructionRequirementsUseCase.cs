#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Core.Construction;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;

namespace HorseParking.Application.Construction
{
    public sealed class ConstructionRequirementSnapshot
    {
        public ConstructionRequirementSnapshot(
            ResourceId resourceId,
            LocalizationKey displayNameKey,
            int requiredQuantity,
            int availableQuantity)
        {
            ResourceId = resourceId;
            DisplayNameKey = displayNameKey;
            RequiredQuantity = requiredQuantity;
            AvailableQuantity = Math.Max(0, availableQuantity);
        }

        public ResourceId ResourceId { get; }
        public LocalizationKey DisplayNameKey { get; }
        public int RequiredQuantity { get; }
        public int AvailableQuantity { get; }
        public int MissingQuantity => Math.Max(0, RequiredQuantity - AvailableQuantity);
        public bool IsSatisfied => MissingQuantity == 0;
    }

    public sealed class ConstructionReadinessSnapshot
    {
        public ConstructionReadinessSnapshot(
            string planId,
            LocalizationKey planDisplayNameKey,
            IReadOnlyList<ConstructionRequirementSnapshot> requirements,
            ConstructionState state,
            double normalizedProgress,
            double durationSeconds)
        {
            PlanId = planId;
            PlanDisplayNameKey = planDisplayNameKey;
            Requirements = requirements;
            State = state;
            NormalizedProgress = Math.Max(0d, Math.Min(1d, normalizedProgress));
            DurationSeconds = durationSeconds;
        }

        public string PlanId { get; }
        public LocalizationKey PlanDisplayNameKey { get; }
        public IReadOnlyList<ConstructionRequirementSnapshot> Requirements { get; }
        public ConstructionState State { get; }
        public double NormalizedProgress { get; }
        public double DurationSeconds { get; }

        public bool CanStart
        {
            get
            {
                if (State != ConstructionState.Planned) return false;
                foreach (var requirement in Requirements)
                {
                    if (!requirement.IsSatisfied) return false;
                }
                return true;
            }
        }
    }

    public enum ConstructionStartFailureReason
    {
        None = 0,
        MissingResources = 1,
        AlreadyInProgress = 2,
        AlreadyCompleted = 3
    }

    public readonly struct ConstructionStartResult
    {
        private ConstructionStartResult(ConstructionStartFailureReason failureReason)
        {
            FailureReason = failureReason;
        }

        public bool Succeeded => FailureReason == ConstructionStartFailureReason.None;
        public ConstructionStartFailureReason FailureReason { get; }
        public static ConstructionStartResult Success() => new(ConstructionStartFailureReason.None);
        public static ConstructionStartResult Failure(ConstructionStartFailureReason reason) => new(reason);
    }

    /// <summary>
    /// Application boundary for readiness, atomic resource spending and construction progress.
    /// </summary>
    public sealed class ConstructionRequirementsUseCase
    {
        private readonly ConstructionProject project;
        private readonly LogisticsInventoryUseCase inventoryUseCase;
        private double constructionSpeedMultiplier = 1d;

        public event Action? ConstructionStarted;
        public event Action? ConstructionProgressChanged;
        public event Action? ConstructionCompleted;

        public ConstructionRequirementsUseCase(
            ConstructionPlan plan,
            LogisticsInventoryUseCase inventoryUseCase,
            double durationSeconds)
        {
            project = new ConstructionProject(
                plan ?? throw new ArgumentNullException(nameof(plan)),
                durationSeconds);
            this.inventoryUseCase = inventoryUseCase ?? throw new ArgumentNullException(nameof(inventoryUseCase));
        }

        public ConstructionReadinessSnapshot GetSnapshot()
        {
            var warehouse = inventoryUseCase.GetWarehouseSnapshot();
            var inventoryItems = new Dictionary<ResourceId, InventoryItemSnapshot>();
            foreach (var item in warehouse.Items) inventoryItems[item.ResourceId] = item;

            var requirements = new List<ConstructionRequirementSnapshot>(project.Plan.Requirements.Count);
            foreach (var requirement in project.Plan.Requirements)
            {
                if (inventoryItems.TryGetValue(requirement.ResourceId, out var item))
                {
                    requirements.Add(new ConstructionRequirementSnapshot(
                        requirement.ResourceId,
                        item.DisplayNameKey,
                        requirement.RequiredQuantity,
                        item.Quantity));
                }
                else
                {
                    requirements.Add(new ConstructionRequirementSnapshot(
                        requirement.ResourceId,
                        new LocalizationKey("resource." + requirement.ResourceId.Value),
                        requirement.RequiredQuantity,
                        0));
                }
            }

            return new ConstructionReadinessSnapshot(
                project.Plan.Id,
                project.Plan.DisplayNameKey,
                requirements.AsReadOnly(),
                project.State,
                project.NormalizedProgress,
                project.DurationSeconds);
        }

        public ConstructionStartResult TryStartConstruction()
        {
            if (project.State == ConstructionState.InProgress)
            {
                return ConstructionStartResult.Failure(ConstructionStartFailureReason.AlreadyInProgress);
            }

            if (project.State == ConstructionState.Completed)
            {
                return ConstructionStartResult.Failure(ConstructionStartFailureReason.AlreadyCompleted);
            }

            var snapshot = GetSnapshot();
            if (!snapshot.CanStart)
            {
                return ConstructionStartResult.Failure(ConstructionStartFailureReason.MissingResources);
            }

            var cost = new Dictionary<ResourceId, int>(project.Plan.Requirements.Count);
            foreach (var requirement in project.Plan.Requirements)
            {
                cost.Add(requirement.ResourceId, requirement.RequiredQuantity);
            }

            var consumption = inventoryUseCase.TryConsumeWarehouseResources(cost);
            if (!consumption.Succeeded)
            {
                return ConstructionStartResult.Failure(ConstructionStartFailureReason.MissingResources);
            }

            if (!project.TryStart())
            {
                throw new InvalidOperationException("Construction state changed after atomic resource consumption.");
            }

            constructionSpeedMultiplier = 1d;
            ConstructionStarted?.Invoke();
            ConstructionProgressChanged?.Invoke();
            return ConstructionStartResult.Success();
        }

        public void AdvanceConstruction(double deltaSeconds)
        {
            var wasCompleted = project.State == ConstructionState.Completed;
            if (!project.Advance(deltaSeconds * constructionSpeedMultiplier)) return;

            ConstructionProgressChanged?.Invoke();
            if (!wasCompleted && project.State == ConstructionState.Completed)
            {
                ConstructionCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Applies a persistent construction-speed multiplier for the active build.
        /// Presentation decides how a hit is detected; the application layer owns its effect.
        /// </summary>
        public bool ApplyWorkerHitSpeedMultiplier(double speedMultiplier)
        {
            if (project.State != ConstructionState.InProgress || speedMultiplier <= 1d) return false;
            constructionSpeedMultiplier = Math.Max(constructionSpeedMultiplier, speedMultiplier);
            return true;
        }
    }
}
