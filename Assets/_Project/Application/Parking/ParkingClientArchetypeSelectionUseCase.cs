#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Parking;
using HorseParking.Core.Randomness;

namespace HorseParking.Application.Parking
{
    /// <summary>Selects client profiles without coupling Core to Unity assets.</summary>
    public sealed class ParkingClientArchetypeSelectionUseCase
    {
        private readonly IReadOnlyList<ParkingClientArchetype> archetypes;
        private readonly IRandomSource random;
        private int previousIndex = -1;

        public ParkingClientArchetypeSelectionUseCase(
            IReadOnlyList<ParkingClientArchetype> archetypes,
            IRandomSource random)
        {
            if (archetypes == null || archetypes.Count == 0)
                throw new ArgumentException("At least one client archetype is required.", nameof(archetypes));
            this.archetypes = archetypes;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public ParkingClientArchetype SelectNext()
        {
            var index = random.NextInt(0, archetypes.Count);
            if (archetypes.Count > 1 && index == previousIndex)
            {
                index = (index + 1) % archetypes.Count;
            }

            previousIndex = index;
            return archetypes[index];
        }
    }
}
