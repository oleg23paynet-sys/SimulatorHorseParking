#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;

namespace HorseParking.Core.Parking
{
    public enum ParkingClientDialogueMoment
    {
        Arriving = 0,
        Parked = 1,
        Returning = 2,
        WaitingForPayment = 3,
        PaymentReceived = 4,
        Leaving = 5,
        PlayerGreeting = 6
    }

    public enum ParkingClientReaction
    {
        Neutral = 0,
        Friendly = 1,
        Impatient = 2,
        Suspicious = 3,
        Satisfied = 4
    }

    /// <summary>One localized, framework-independent NPC response.</summary>
    public sealed class ParkingClientDialogueLine
    {
        public ParkingClientDialogueLine(
            ParkingClientDialogueMoment moment,
            LocalizationKey textKey,
            ParkingClientReaction reaction)
        {
            Moment = moment;
            TextKey = textKey;
            Reaction = reaction;
        }

        public ParkingClientDialogueMoment Moment { get; }
        public LocalizationKey TextKey { get; }
        public ParkingClientReaction Reaction { get; }
    }

    /// <summary>
    /// Dialogue content for one client archetype. Core stores stable keys only;
    /// translated text and Unity UI remain outside the domain.
    /// </summary>
    public sealed class ParkingClientDialogueProfile
    {
        public ParkingClientDialogueProfile(
            string archetypeId,
            IReadOnlyList<ParkingClientDialogueLine> lines)
        {
            if (string.IsNullOrWhiteSpace(archetypeId))
                throw new ArgumentException("Dialogue archetype id is required.", nameof(archetypeId));
            if (lines == null || lines.Count == 0)
                throw new ArgumentException("At least one dialogue line is required.", nameof(lines));

            ArchetypeId = archetypeId;
            Lines = lines;
        }

        public string ArchetypeId { get; }
        public IReadOnlyList<ParkingClientDialogueLine> Lines { get; }
    }
}
