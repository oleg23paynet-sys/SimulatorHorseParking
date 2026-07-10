#nullable enable

using HorseParking.Core.Localization;

namespace HorseParking.Core.Interaction
{

/// <summary>
/// Localized metadata displayed by Presentation when an interaction target is focused.
/// </summary>
public sealed class InteractionPrompt
{
    public InteractionPrompt(LocalizationKey actionKey, LocalizationKey targetKey)
    {
        ActionKey = actionKey;
        TargetKey = targetKey;
    }

    public LocalizationKey ActionKey { get; }

    public LocalizationKey TargetKey { get; }
}
}
