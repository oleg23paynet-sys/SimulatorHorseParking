#nullable enable

using System.Collections.Generic;
using HorseParking.Core.Localization;

namespace HorseParking.Infrastructure.Localization
{
    /// <summary>
    /// Data-driven localization implementation. Missing keys safely fall back to their stable key value.
    /// </summary>
    public sealed class DictionaryLocalizationService : ILocalizationService
    {
        private readonly IReadOnlyDictionary<string, string> translations;

        public DictionaryLocalizationService(string currentLocale, IReadOnlyDictionary<string, string> translations)
        {
            CurrentLocale = currentLocale;
            this.translations = translations;
        }

        public string CurrentLocale { get; }

        public string Translate(LocalizationKey key, IReadOnlyDictionary<string, object>? arguments = null)
        {
            var result = translations.TryGetValue(key.Value, out var translation) ? translation : key.Value;

            if (arguments == null)
            {
                return result;
            }

            foreach (var argument in arguments)
            {
                result = result.Replace("{" + argument.Key + "}", argument.Value?.ToString() ?? string.Empty);
            }

            return result;
        }
    }
}
