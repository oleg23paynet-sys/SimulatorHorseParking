#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Parking;
using HorseParking.Core.Randomness;

namespace HorseParking.Application.Parking
{
    /// <summary>
    /// Selects a contextual NPC line without knowing about Unity, UI or a concrete locale.
    /// </summary>
    public sealed class ParkingClientDialogueUseCase
    {
        private readonly IReadOnlyDictionary<string, ParkingClientDialogueProfile> profiles;
        private readonly IRandomSource random;
        private readonly Dictionary<string, int> previousLineByMoment = new Dictionary<string, int>();

        public ParkingClientDialogueUseCase(
            IReadOnlyList<ParkingClientDialogueProfile> profiles,
            IRandomSource random)
        {
            if (profiles == null || profiles.Count == 0)
                throw new ArgumentException("At least one dialogue profile is required.", nameof(profiles));

            var profileMap = new Dictionary<string, ParkingClientDialogueProfile>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                if (!profileMap.TryAdd(profile.ArchetypeId, profile))
                    throw new ArgumentException("Duplicate dialogue profile: " + profile.ArchetypeId, nameof(profiles));
            }

            this.profiles = profileMap;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool TrySelectLine(
            ParkingClientArchetype archetype,
            ParkingClientDialogueMoment moment,
            out ParkingClientDialogueLine line)
        {
            if (archetype == null) throw new ArgumentNullException(nameof(archetype));
            line = null!;

            if (!profiles.TryGetValue(archetype.Id, out var profile))
                return false;

            var matches = new List<ParkingClientDialogueLine>();
            foreach (var candidate in profile.Lines)
            {
                if (candidate.Moment == moment)
                    matches.Add(candidate);
            }

            if (matches.Count == 0)
                return false;

            var selectedIndex = random.NextInt(0, matches.Count);
            var historyKey = archetype.Id + ":" + moment;
            if (matches.Count > 1
                && previousLineByMoment.TryGetValue(historyKey, out var previousIndex)
                && selectedIndex == previousIndex)
            {
                selectedIndex = (selectedIndex + 1) % matches.Count;
            }

            previousLineByMoment[historyKey] = selectedIndex;
            line = matches[selectedIndex];
            return true;
        }
    }
}
