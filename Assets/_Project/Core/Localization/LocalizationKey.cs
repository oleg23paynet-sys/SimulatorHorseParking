#nullable enable

namespace HorseParking.Core.Localization
{

/// <summary>
/// Stable identifier of player-facing text. The actual translation is supplied outside Core.
/// </summary>
public readonly struct LocalizationKey
{
    public LocalizationKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Localization key cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
}
