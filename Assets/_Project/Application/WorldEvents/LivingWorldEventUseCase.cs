#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Economy;
using HorseParking.Core.Localization;
using HorseParking.Core.Randomness;
using HorseParking.Core.WorldEvents;

namespace HorseParking.Application.WorldEvents
{
    public enum LivingWorldEventState
    {
        Available = 0,
        Resolved = 1
    }

    public enum LivingWorldEventFailureReason
    {
        None = 0,
        NoMatchingEvent = 1,
        EventAlreadyResolved = 2,
        UnknownOption = 3,
        InsufficientGold = 4
    }

    public sealed class LivingWorldEventSnapshot
    {
        public LivingWorldEventSnapshot(
            LivingWorldEventDefinition definition,
            int encounterId,
            LivingWorldEventState state,
            LocalizationKey? outcomeKey,
            int goldDelta)
        {
            Definition = definition;
            EncounterId = encounterId;
            State = state;
            OutcomeKey = outcomeKey;
            GoldDelta = goldDelta;
        }

        public LivingWorldEventDefinition Definition { get; }
        public int EncounterId { get; }
        public LivingWorldEventState State { get; }
        public LocalizationKey? OutcomeKey { get; }
        public int GoldDelta { get; }
    }

    public readonly struct LivingWorldEventResolution
    {
        private LivingWorldEventResolution(
            LivingWorldEventFailureReason failureReason,
            LocalizationKey? outcomeKey,
            int goldDelta)
        {
            FailureReason = failureReason;
            OutcomeKey = outcomeKey;
            GoldDelta = goldDelta;
        }

        public LivingWorldEventFailureReason FailureReason { get; }
        public LocalizationKey? OutcomeKey { get; }
        public int GoldDelta { get; }
        public bool Succeeded => FailureReason == LivingWorldEventFailureReason.None;

        public static LivingWorldEventResolution Success(LocalizationKey outcomeKey, int goldDelta) =>
            new LivingWorldEventResolution(LivingWorldEventFailureReason.None, outcomeKey, goldDelta);

        public static LivingWorldEventResolution Failure(LivingWorldEventFailureReason reason) =>
            new LivingWorldEventResolution(reason, null, 0);
    }

    /// <summary>
    /// Resolves data-driven optional events against the shared player wallet.
    /// It does not know about Unity, UI, NPC routes or the parking lifecycle.
    /// </summary>
    public sealed class LivingWorldEventUseCase
    {
        private static readonly LocalizationKey TransactionKey = new LocalizationKey("economy.world_event");

        private readonly IReadOnlyList<LivingWorldEventDefinition> definitions;
        private readonly GoldWallet wallet;
        private readonly IRandomSource random;
        private LivingWorldEventSnapshot? current;

        public LivingWorldEventUseCase(
            IReadOnlyList<LivingWorldEventDefinition> definitions,
            GoldWallet wallet,
            IRandomSource random)
        {
            if (definitions == null || definitions.Count == 0)
                throw new ArgumentException("At least one world-event definition is required.", nameof(definitions));

            this.definitions = definitions;
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public event Action? EventChanged;

        public LivingWorldEventSnapshot? Current => current;

        public bool CanOffer(string clientArchetypeId, int encounterId)
        {
            if (string.IsNullOrWhiteSpace(clientArchetypeId) || encounterId <= 0)
                return false;

            if (current != null && current.EncounterId == encounterId)
                return current.State == LivingWorldEventState.Available;

            return FindDefinition(clientArchetypeId) != null;
        }

        public bool TryOffer(string clientArchetypeId, int encounterId, out LivingWorldEventSnapshot snapshot)
        {
            snapshot = null!;
            if (!CanOffer(clientArchetypeId, encounterId))
                return false;

            if (current != null && current.EncounterId == encounterId)
            {
                snapshot = current;
                return true;
            }

            var definition = FindDefinition(clientArchetypeId);
            if (definition == null)
                return false;

            current = new LivingWorldEventSnapshot(
                definition,
                encounterId,
                LivingWorldEventState.Available,
                null,
                0);
            snapshot = current;
            EventChanged?.Invoke();
            return true;
        }

        public LivingWorldEventResolution TryResolve(string optionId)
        {
            if (current == null)
                return LivingWorldEventResolution.Failure(LivingWorldEventFailureReason.NoMatchingEvent);
            if (current.State == LivingWorldEventState.Resolved)
                return LivingWorldEventResolution.Failure(LivingWorldEventFailureReason.EventAlreadyResolved);

            LivingWorldEventOption? option = null;
            foreach (var candidate in current.Definition.Options)
            {
                if (string.Equals(candidate.Id, optionId, StringComparison.Ordinal))
                {
                    option = candidate;
                    break;
                }
            }

            if (option == null)
                return LivingWorldEventResolution.Failure(LivingWorldEventFailureReason.UnknownOption);

            var succeeded = option.SuccessChance >= 1f
                            || random.NextFloat(0f, 1f) <= option.SuccessChance;
            var goldDelta = succeeded ? option.SuccessGoldDelta : option.FailureGoldDelta;
            var outcomeKey = succeeded ? option.SuccessOutcomeKey : option.FailureOutcomeKey;

            if (goldDelta < 0 && !wallet.TryDebit(-goldDelta, GoldTransactionKind.Expense, TransactionKey))
                return LivingWorldEventResolution.Failure(LivingWorldEventFailureReason.InsufficientGold);
            if (goldDelta > 0)
                wallet.Credit(goldDelta, TransactionKey);

            current = new LivingWorldEventSnapshot(
                current.Definition,
                current.EncounterId,
                LivingWorldEventState.Resolved,
                outcomeKey,
                goldDelta);
            EventChanged?.Invoke();
            return LivingWorldEventResolution.Success(outcomeKey, goldDelta);
        }

        public void EndEncounter(int encounterId)
        {
            if (current == null || current.EncounterId != encounterId)
                return;

            current = null;
            EventChanged?.Invoke();
        }

        private LivingWorldEventDefinition? FindDefinition(string clientArchetypeId)
        {
            foreach (var definition in definitions)
            {
                if (string.Equals(
                        definition.TriggerClientArchetypeId,
                        clientArchetypeId,
                        StringComparison.Ordinal))
                    return definition;
            }

            return null;
        }
    }
}
