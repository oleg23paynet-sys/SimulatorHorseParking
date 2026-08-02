#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;

namespace HorseParking.Core.WorldEvents
{
    /// <summary>One data-driven player choice for a living-world event.</summary>
    public sealed class LivingWorldEventOption
    {
        public LivingWorldEventOption(
            string id,
            LocalizationKey labelKey,
            float successChance,
            int successGoldDelta,
            int failureGoldDelta,
            LocalizationKey successOutcomeKey,
            LocalizationKey failureOutcomeKey)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("World-event option id is required.", nameof(id));
            if (successChance < 0f || successChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(successChance));

            Id = id;
            LabelKey = labelKey;
            SuccessChance = successChance;
            SuccessGoldDelta = successGoldDelta;
            FailureGoldDelta = failureGoldDelta;
            SuccessOutcomeKey = successOutcomeKey;
            FailureOutcomeKey = failureOutcomeKey;
        }

        public string Id { get; }
        public LocalizationKey LabelKey { get; }
        public float SuccessChance { get; }
        public int SuccessGoldDelta { get; }
        public int FailureGoldDelta { get; }
        public LocalizationKey SuccessOutcomeKey { get; }
        public LocalizationKey FailureOutcomeKey { get; }
    }

    /// <summary>
    /// Framework-independent event definition. More events can be added through
    /// external settings without changing the resolution use case.
    /// </summary>
    public sealed class LivingWorldEventDefinition
    {
        public LivingWorldEventDefinition(
            string id,
            string triggerClientArchetypeId,
            LocalizationKey titleKey,
            LocalizationKey descriptionKey,
            IReadOnlyList<LivingWorldEventOption> options)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("World-event id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(triggerClientArchetypeId))
                throw new ArgumentException("Trigger archetype id is required.", nameof(triggerClientArchetypeId));
            if (options == null || options.Count < 2)
                throw new ArgumentException("A world event requires at least two choices.", nameof(options));

            Id = id;
            TriggerClientArchetypeId = triggerClientArchetypeId;
            TitleKey = titleKey;
            DescriptionKey = descriptionKey;
            Options = options;
        }

        public string Id { get; }
        public string TriggerClientArchetypeId { get; }
        public LocalizationKey TitleKey { get; }
        public LocalizationKey DescriptionKey { get; }
        public IReadOnlyList<LivingWorldEventOption> Options { get; }
    }
}
