using System;
using HorseParking.Core.Randomness;

namespace HorseParking.Infrastructure.Randomness
{
    /// <summary>
    /// Deterministic random implementation. A seed can be supplied for repeatable simulations and tests.
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random random;

        public SeededRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat(float minInclusive, float maxInclusive)
        {
            if (minInclusive > maxInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(minInclusive));
            }

            return minInclusive + ((float)random.NextDouble() * (maxInclusive - minInclusive));
        }
    }
}
