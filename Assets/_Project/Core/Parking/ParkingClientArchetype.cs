#nullable enable

using System;
using HorseParking.Core.Localization;

namespace HorseParking.Core.Parking
{
    public enum ParkingClientTemperament
    {
        Calm = 0,
        Busy = 1,
        Demanding = 2
    }

    /// <summary>
    /// Framework-independent client profile. Visual models can be replaced without
    /// changing parking time, tariff or behavior data.
    /// </summary>
    public sealed class ParkingClientArchetype
    {
        public ParkingClientArchetype(
            string id,
            LocalizationKey nameKey,
            LocalizationKey descriptionKey,
            double parkingDurationSeconds,
            ParkingTariff tariff,
            ParkingClientTemperament temperament)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Client archetype id is required.", nameof(id));
            if (parkingDurationSeconds <= 0d) throw new ArgumentOutOfRangeException(nameof(parkingDurationSeconds));
            Id = id;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            ParkingDurationSeconds = parkingDurationSeconds;
            Tariff = tariff ?? throw new ArgumentNullException(nameof(tariff));
            Temperament = temperament;
        }

        public string Id { get; }
        public LocalizationKey NameKey { get; }
        public LocalizationKey DescriptionKey { get; }
        public double ParkingDurationSeconds { get; }
        public ParkingTariff Tariff { get; }
        public ParkingClientTemperament Temperament { get; }
    }
}
