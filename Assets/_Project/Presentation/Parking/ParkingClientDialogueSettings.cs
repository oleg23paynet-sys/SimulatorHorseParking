#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Parking;
using HorseParking.Core.Localization;
using HorseParking.Core.Parking;
using HorseParking.Core.Randomness;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    [CreateAssetMenu(
        fileName = "ParkingClientDialogueSettings",
        menuName = "Horse Parking/Client Dialogue Settings")]
    public sealed class ParkingClientDialogueSettings : ScriptableObject
    {
        [Serializable]
        private sealed class LineDefinition
        {
            [SerializeField] private ParkingClientDialogueMoment moment;
            [SerializeField] private string textKey = "client.dialogue.line";
            [SerializeField] private ParkingClientReaction reaction;

            public LineDefinition(
                ParkingClientDialogueMoment moment,
                string textKey,
                ParkingClientReaction reaction)
            {
                this.moment = moment;
                this.textKey = textKey;
                this.reaction = reaction;
            }

            public ParkingClientDialogueLine Create()
            {
                return new ParkingClientDialogueLine(
                    moment,
                    new LocalizationKey(textKey),
                    reaction);
            }
        }

        [Serializable]
        private sealed class ProfileDefinition
        {
            [SerializeField] private string archetypeId = "traveler";
            [SerializeField] private List<LineDefinition> lines = new List<LineDefinition>();

            public ProfileDefinition(string archetypeId, List<LineDefinition> lines)
            {
                this.archetypeId = archetypeId;
                this.lines = lines;
            }

            public ParkingClientDialogueProfile Create()
            {
                var runtimeLines = new List<ParkingClientDialogueLine>(lines.Count);
                foreach (var line in lines)
                    runtimeLines.Add(line.Create());
                return new ParkingClientDialogueProfile(archetypeId, runtimeLines.AsReadOnly());
            }
        }

        [SerializeField] private List<ProfileDefinition> profiles = new List<ProfileDefinition>();
        [Min(1f)] [SerializeField] private float automaticLineDurationSeconds = 4f;
        [Min(1f)] [SerializeField] private float interactionLineDurationSeconds = 5f;

        public float AutomaticLineDurationSeconds => automaticLineDurationSeconds;
        public float InteractionLineDurationSeconds => interactionLineDurationSeconds;

        public void EnsureDemoDefaults()
        {
            if (profiles.Count > 0) return;

            profiles.Add(CreateProfile(
                "traveler",
                ParkingClientReaction.Friendly,
                "traveler"));
            profiles.Add(CreateProfile(
                "merchant",
                ParkingClientReaction.Impatient,
                "merchant"));
            profiles.Add(CreateProfile(
                "royal_inspector",
                ParkingClientReaction.Suspicious,
                "royal_inspector"));
        }

        public ParkingClientDialogueUseCase CreateUseCase(IRandomSource random)
        {
            var runtimeProfiles = new List<ParkingClientDialogueProfile>(profiles.Count);
            foreach (var profile in profiles)
                runtimeProfiles.Add(profile.Create());
            return new ParkingClientDialogueUseCase(runtimeProfiles.AsReadOnly(), random);
        }

        private static ProfileDefinition CreateProfile(
            string id,
            ParkingClientReaction defaultReaction,
            string keyId)
        {
            var prefix = "client.dialogue." + keyId + ".";
            return new ProfileDefinition(
                id,
                new List<LineDefinition>
                {
                    new LineDefinition(ParkingClientDialogueMoment.Arriving, prefix + "arriving", defaultReaction),
                    new LineDefinition(ParkingClientDialogueMoment.Parked, prefix + "parked", defaultReaction),
                    new LineDefinition(ParkingClientDialogueMoment.Returning, prefix + "returning", defaultReaction),
                    new LineDefinition(ParkingClientDialogueMoment.WaitingForPayment, prefix + "waiting_payment", defaultReaction),
                    new LineDefinition(ParkingClientDialogueMoment.PaymentReceived, prefix + "payment_received", ParkingClientReaction.Satisfied),
                    new LineDefinition(ParkingClientDialogueMoment.Leaving, prefix + "leaving", ParkingClientReaction.Satisfied),
                    new LineDefinition(ParkingClientDialogueMoment.PlayerGreeting, prefix + "greeting.1", defaultReaction),
                    new LineDefinition(ParkingClientDialogueMoment.PlayerGreeting, prefix + "greeting.2", defaultReaction)
                });
        }
    }
}
