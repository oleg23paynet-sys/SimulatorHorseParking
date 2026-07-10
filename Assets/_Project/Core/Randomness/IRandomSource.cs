namespace HorseParking.Core.Randomness
{

/// <summary>
/// Controlled random source injected into Core so simulations and tests can be reproduced.
/// </summary>
public interface IRandomSource
{
    int NextInt(int minInclusive, int maxExclusive);

    float NextFloat(float minInclusive, float maxInclusive);
}
}
