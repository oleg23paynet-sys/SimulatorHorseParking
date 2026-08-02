#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.WorldEvents;
using HorseParking.Application.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Core.Randomness;
using HorseParking.Core.WorldEvents;
using UnityEngine;

namespace HorseParking.Presentation.WorldEvents
{
    [CreateAssetMenu(fileName = "LivingWorldEventSettings", menuName = "Horse Parking/Living World Event Settings")]
    public sealed class LivingWorldEventSettings : ScriptableObject
    {
        [Serializable]
        private sealed class OptionDefinition
        {
            [SerializeField] private string id = "option";
            [SerializeField] private string labelKey = "event.option";
            [Range(0f, 1f)] [SerializeField] private float successChance = 1f;
            [SerializeField] private int successGoldDelta;
            [SerializeField] private int failureGoldDelta;
            [SerializeField] private string successOutcomeKey = "event.outcome.success";
            [SerializeField] private string failureOutcomeKey = "event.outcome.failure";

            public OptionDefinition(
                string id,
                string labelKey,
                float successChance,
                int successGoldDelta,
                int failureGoldDelta,
                string successOutcomeKey,
                string failureOutcomeKey)
            {
                this.id = id;
                this.labelKey = labelKey;
                this.successChance = successChance;
                this.successGoldDelta = successGoldDelta;
                this.failureGoldDelta = failureGoldDelta;
                this.successOutcomeKey = successOutcomeKey;
                this.failureOutcomeKey = failureOutcomeKey;
            }

            public LivingWorldEventOption Create() => new LivingWorldEventOption(
                id,
                new LocalizationKey(labelKey),
                successChance,
                successGoldDelta,
                failureGoldDelta,
                new LocalizationKey(successOutcomeKey),
                new LocalizationKey(failureOutcomeKey));
        }

        [Serializable]
        private sealed class EventDefinition
        {
            [SerializeField] private string id = "event";
            [SerializeField] private string triggerClientArchetypeId = "royal_inspector";
            [SerializeField] private string titleKey = "event.title";
            [SerializeField] private string descriptionKey = "event.description";
            [SerializeField] private List<OptionDefinition> options = new List<OptionDefinition>();

            public EventDefinition(
                string id,
                string triggerClientArchetypeId,
                string titleKey,
                string descriptionKey,
                List<OptionDefinition> options)
            {
                this.id = id;
                this.triggerClientArchetypeId = triggerClientArchetypeId;
                this.titleKey = titleKey;
                this.descriptionKey = descriptionKey;
                this.options = options;
            }

            public LivingWorldEventDefinition Create()
            {
                var runtimeOptions = new List<LivingWorldEventOption>(options.Count);
                foreach (var option in options)
                    runtimeOptions.Add(option.Create());

                return new LivingWorldEventDefinition(
                    id,
                    triggerClientArchetypeId,
                    new LocalizationKey(titleKey),
                    new LocalizationKey(descriptionKey),
                    runtimeOptions.AsReadOnly());
            }
        }

        [SerializeField] private List<EventDefinition> events = new List<EventDefinition>();

        public void EnsureDemoDefaults()
        {
            if (events.Count > 0) return;

            events.Add(new EventDefinition(
                "royal_inspection",
                "royal_inspector",
                "event.royal_inspection.title",
                "event.royal_inspection.description",
                new List<OptionDefinition>
                {
                    new OptionDefinition(
                        "pay_duty",
                        "event.royal_inspection.option.pay",
                        1f,
                        -12,
                        -12,
                        "event.royal_inspection.outcome.paid",
                        "event.royal_inspection.outcome.paid"),
                    new OptionDefinition(
                        "stable_excuse",
                        "event.royal_inspection.option.stable_excuse",
                        0.65f,
                        8,
                        -6,
                        "event.royal_inspection.outcome.excuse_success",
                        "event.royal_inspection.outcome.excuse_failed")
                }));
        }

        public LivingWorldEventUseCase CreateUseCase(
            LogisticsInventoryUseCase logistics,
            IRandomSource random)
        {
            var runtimeEvents = new List<LivingWorldEventDefinition>(events.Count);
            foreach (var definition in events)
                runtimeEvents.Add(definition.Create());

            return new LivingWorldEventUseCase(runtimeEvents.AsReadOnly(), logistics.Wallet, random);
        }
    }
}
