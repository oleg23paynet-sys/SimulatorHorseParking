#nullable enable

using System.Collections.Generic;

namespace HorseParking.Core.Localization
{

/// <summary>
/// Port used by game rules that need player-facing text without depending on Unity localization packages.
/// </summary>
public interface ILocalizationService
{
    string CurrentLocale { get; }

    string Translate(LocalizationKey key, IReadOnlyDictionary<string, object>? arguments = null);
}
}
