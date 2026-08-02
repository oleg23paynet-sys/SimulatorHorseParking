#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Parking;
using HorseParking.Core.Localization;
using HorseParking.Core.Parking;
using HorseParking.Core.Randomness;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    [CreateAssetMenu(
        fileName = "ParkingClientArchetypeSettings",
        menuName = "Horse Parking/Client Archetype Settings")]
    public sealed class ParkingClientArchetypeSettings : ScriptableObject
    {
        [Serializable]
        private sealed class Definition
        {
            [SerializeField] private string id = "traveler";
            [SerializeField] private string nameKey = "client.archetype.traveler";
            [SerializeField] private string descriptionKey = "client.archetype.traveler.description";
            [Min(1f)] [SerializeField] private float parkingDurationSeconds = 5f;
            [Min(1f)] [SerializeField] private float billingPeriodSeconds = 20f;
            [Min(1)] [SerializeField] private int goldPerPeriod = 3;
            [SerializeField] private ParkingClientTemperament temperament;

            public Definition(
                string id,
                string nameKey,
                string descriptionKey,
                float parkingDurationSeconds,
                float billingPeriodSeconds,
                int goldPerPeriod,
                ParkingClientTemperament temperament)
            {
                this.id = id;
                this.nameKey = nameKey;
                this.descriptionKey = descriptionKey;
                this.parkingDurationSeconds = parkingDurationSeconds;
                this.billingPeriodSeconds = billingPeriodSeconds;
                this.goldPerPeriod = goldPerPeriod;
                this.temperament = temperament;
            }

            public ParkingClientArchetype Create()
            {
                return new ParkingClientArchetype(
                    id,
                    new LocalizationKey(nameKey),
                    new LocalizationKey(descriptionKey),
                    parkingDurationSeconds,
                    new ParkingTariff(billingPeriodSeconds, goldPerPeriod),
                    temperament);
            }
        }

        [SerializeField] private List<Definition> definitions = new List<Definition>();
        [Min(0f)] [SerializeField] private float delayBetweenClientsSeconds = 3f;

        public double DelayBetweenClientsSeconds => delayBetweenClientsSeconds;

        public void EnsureDemoDefaults()
        {
            if (definitions.Count > 0) return;
            definitions.Add(new Definition(
                "traveler",
                "client.archetype.traveler",
                "client.archetype.traveler.description",
                5f,
                20f,
                3,
                ParkingClientTemperament.Calm));
            definitions.Add(new Definition(
                "merchant",
                "client.archetype.merchant",
                "client.archetype.merchant.description",
                8f,
                20f,
                5,
                ParkingClientTemperament.Busy));
            definitions.Add(new Definition(
                "royal_inspector",
                "client.archetype.royal_inspector",
                "client.archetype.royal_inspector.description",
                12f,
                15f,
                7,
                ParkingClientTemperament.Demanding));
        }

        public ParkingClientArchetypeSelectionUseCase CreateUseCase(IRandomSource random)
        {
            var profiles = new List<ParkingClientArchetype>(definitions.Count);
            foreach (var definition in definitions)
            {
                profiles.Add(definition.Create());
            }

            return new ParkingClientArchetypeSelectionUseCase(profiles.AsReadOnly(), random);
        }
    }
}
