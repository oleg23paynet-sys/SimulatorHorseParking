#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Construction;
using HorseParking.Application.Logistics;
using HorseParking.Core.Construction;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using UnityEngine;

namespace HorseParking.Presentation.Construction
{
    [CreateAssetMenu(
        fileName = "ConstructionRequirementsSettings",
        menuName = "Horse Parking/Construction Requirements Settings")]
    public sealed class ConstructionRequirementsSettings : ScriptableObject
    {
        [Serializable]
        private sealed class RequirementSeed
        {
            [SerializeField] private string resourceId = "wood";
            [Min(1)] [SerializeField] private int requiredQuantity = 1;

            public RequirementSeed(string id, int quantity)
            {
                resourceId = id;
                requiredQuantity = quantity;
            }

            public ConstructionRequirement CreateRequirement()
            {
                return new ConstructionRequirement(new ResourceId(resourceId), requiredQuantity);
            }
        }

        [SerializeField] private string planId = "parking-slot-02";
        [SerializeField] private string planDisplayNameKey = "construction.parking_slot";
        [Min(1f)] [SerializeField] private float constructionDurationSeconds = 15f;
        [SerializeField] private List<RequirementSeed> requirements = new()
        {
            new RequirementSeed("wood", 4),
            new RequirementSeed("stone", 3),
            new RequirementSeed("iron", 1)
        };

        public ConstructionRequirementsUseCase CreateUseCase(LogisticsInventoryUseCase inventoryUseCase)
        {
            var runtimeRequirements = new List<ConstructionRequirement>(requirements.Count);
            foreach (var requirement in requirements)
            {
                runtimeRequirements.Add(requirement.CreateRequirement());
            }

            var plan = new ConstructionPlan(
                planId,
                new LocalizationKey(planDisplayNameKey),
                runtimeRequirements);
            return new ConstructionRequirementsUseCase(
                plan,
                inventoryUseCase,
                constructionDurationSeconds);
        }
    }
}
