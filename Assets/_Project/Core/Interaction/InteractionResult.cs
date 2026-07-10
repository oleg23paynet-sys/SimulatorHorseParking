#nullable enable

using HorseParking.Core.Localization;

namespace HorseParking.Core.Interaction
{

/// <summary>
/// Result returned by an interaction target. Presentation decides how to render it.
/// </summary>
public sealed class InteractionResult
{
    private InteractionResult(bool succeeded, LocalizationKey messageKey)
    {
        Succeeded = succeeded;
        MessageKey = messageKey;
    }

    public bool Succeeded { get; }

    public LocalizationKey MessageKey { get; }

    public static InteractionResult Success(LocalizationKey messageKey) => new(true, messageKey);

    public static InteractionResult Failure(LocalizationKey messageKey) => new(false, messageKey);
}
}
