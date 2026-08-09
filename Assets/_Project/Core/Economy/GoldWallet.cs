#nullable enable

using System;
using HorseParking.Core.Localization;

namespace HorseParking.Core.Economy
{
    public enum GoldTransactionKind
    {
        Income = 0,
        Expense = 1,
        Purchase = 2,
        Upgrade = 3
    }

    public sealed class GoldTransaction
    {
        public GoldTransaction(
            GoldTransactionKind kind,
            int signedAmount,
            int balanceAfter,
            LocalizationKey descriptionKey)
        {
            Kind = kind;
            SignedAmount = signedAmount;
            BalanceAfter = balanceAfter;
            DescriptionKey = descriptionKey;
        }

        public GoldTransactionKind Kind { get; }
        public int SignedAmount { get; }
        public int BalanceAfter { get; }
        public LocalizationKey DescriptionKey { get; }
    }

    /// <summary>
    /// Framework-independent source of truth for every gold operation in the game.
    /// The balance can never become negative.
    /// </summary>
    public sealed class GoldWallet
    {
        public GoldWallet(int startingBalance)
        {
            if (startingBalance < 0) throw new ArgumentOutOfRangeException(nameof(startingBalance));
            Balance = startingBalance;
        }

        public event Action? BalanceChanged;
        public event Action<GoldTransaction>? TransactionRecorded;

        public int Balance { get; private set; }
        public GoldTransaction? LastTransaction { get; private set; }

        public int Credit(int amount, LocalizationKey descriptionKey)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Balance = checked(Balance + amount);
            Record(GoldTransactionKind.Income, amount, descriptionKey);
            return Balance;
        }

        public bool TryDebit(
            int amount,
            GoldTransactionKind kind,
            LocalizationKey descriptionKey)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > Balance) return false;

            Balance -= amount;
            Record(kind, -amount, descriptionKey);
            return true;
        }

        /// <summary>
        /// Restores an already validated persisted balance without recording a new
        /// income or expense. Loading progress must not create a fake transaction.
        /// </summary>
        public void RestoreBalance(int balance)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            Balance = balance;
            LastTransaction = null;
            BalanceChanged?.Invoke();
        }

        private void Record(
            GoldTransactionKind kind,
            int signedAmount,
            LocalizationKey descriptionKey)
        {
            LastTransaction = new GoldTransaction(kind, signedAmount, Balance, descriptionKey);
            BalanceChanged?.Invoke();
            TransactionRecorded?.Invoke(LastTransaction);
        }
    }
}
