#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;

namespace HorseParking.Core.Construction
{
    public sealed class ConstructionRequirement
    {
        public ConstructionRequirement(ResourceId resourceId, int requiredQuantity)
        {
            if (requiredQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(requiredQuantity));
            ResourceId = resourceId;
            RequiredQuantity = requiredQuantity;
        }

        public ResourceId ResourceId { get; }
        public int RequiredQuantity { get; }
    }

    /// <summary>Immutable, Unity-independent definition of a predetermined construction site.</summary>
    public sealed class ConstructionPlan
    {
        public ConstructionPlan(
            string id,
            LocalizationKey displayNameKey,
            IEnumerable<ConstructionRequirement> requirements)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Plan id is required.", nameof(id));
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));

            var ordered = new List<ConstructionRequirement>();
            var resourceIds = new HashSet<ResourceId>();
            foreach (var requirement in requirements)
            {
                if (requirement == null) throw new ArgumentException("Requirement cannot be null.", nameof(requirements));
                if (!resourceIds.Add(requirement.ResourceId))
                {
                    throw new ArgumentException(
                        "Duplicate construction requirement: " + requirement.ResourceId.Value,
                        nameof(requirements));
                }
                ordered.Add(requirement);
            }

            if (ordered.Count == 0) throw new ArgumentException("At least one requirement is required.", nameof(requirements));
            Id = id.Trim();
            DisplayNameKey = displayNameKey;
            Requirements = ordered.AsReadOnly();
        }

        public string Id { get; }
        public LocalizationKey DisplayNameKey { get; }
        public IReadOnlyList<ConstructionRequirement> Requirements { get; }
    }
}
