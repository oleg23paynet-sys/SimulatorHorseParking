#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Core.Economy;
using HorseParking.Core.Localization;

namespace HorseParking.Application.Economy
{
    public enum ParkingUpgradeId
    {
        CartCapacity = 0,
        CartSpeed = 1,
        ConstructionSpeed = 2
    }

    public enum EconomyActionFailureReason
    {
        None = 0,
        InsufficientGold = 1,
        MaximumLevelReached = 2
    }

    public readonly struct EconomyActionResult
    {
        private EconomyActionResult(EconomyActionFailureReason failureReason)
        {
            FailureReason = failureReason;
        }

        public bool Succeeded => FailureReason == EconomyActionFailureReason.None;
        public EconomyActionFailureReason FailureReason { get; }
        public static EconomyActionResult Success() => new EconomyActionResult(EconomyActionFailureReason.None);
        public static EconomyActionResult Failure(EconomyActionFailureReason reason) => new EconomyActionResult(reason);
    }

    public sealed class ParkingUpgradeSnapshot
    {
        public ParkingUpgradeSnapshot(
            ParkingUpgradeId id,
            int level,
            int maximumLevel,
            int nextCost,
            double effectPerLevel)
        {
            Id = id;
            Level = level;
            MaximumLevel = maximumLevel;
            NextCost = nextCost;
            EffectPerLevel = effectPerLevel;
        }

        public ParkingUpgradeId Id { get; }
        public int Level { get; }
        public int MaximumLevel { get; }
        public int NextCost { get; }
        public double EffectPerLevel { get; }
        public bool IsMaximumLevel => Level >= MaximumLevel;
    }

    public sealed class ParkingEconomySnapshot
    {
        public ParkingEconomySnapshot(
            int gold,
            int driverSalary,
            int royalTribute,
            double secondsUntilExpenses,
            IReadOnlyList<ParkingUpgradeSnapshot> upgrades,
            GoldTransaction? lastTransaction,
            LocalizationKey? noticeKey)
        {
            Gold = gold;
            DriverSalary = driverSalary;
            RoyalTribute = royalTribute;
            SecondsUntilExpenses = secondsUntilExpenses;
            Upgrades = upgrades;
            LastTransaction = lastTransaction;
            NoticeKey = noticeKey;
        }

        public int Gold { get; }
        public int DriverSalary { get; }
        public int RoyalTribute { get; }
        public double SecondsUntilExpenses { get; }
        public IReadOnlyList<ParkingUpgradeSnapshot> Upgrades { get; }
        public GoldTransaction? LastTransaction { get; }
        public LocalizationKey? NoticeKey { get; }
    }

    /// <summary>
    /// Application boundary for the Stage 4 business rules. Presentation only sends
    /// commands and reads snapshots; all balance checks stay here and in Core.
    /// </summary>
    public sealed class ParkingEconomyUseCase
    {
        private sealed class UpgradeState
        {
            public UpgradeState(int baseCost, int costStep, int maximumLevel)
            {
                BaseCost = baseCost;
                CostStep = costStep;
                MaximumLevel = maximumLevel;
            }

            public int BaseCost { get; }
            public int CostStep { get; }
            public int MaximumLevel { get; }
            public int Level { get; set; }
            public int NextCost => checked(BaseCost + Level * CostStep);
        }

        private static readonly LocalizationKey SalaryKey = new LocalizationKey("economy.expense.driver_salary");
        private static readonly LocalizationKey TributeKey = new LocalizationKey("economy.expense.royal_tribute");
        private static readonly LocalizationKey CartCapacityKey = new LocalizationKey("economy.upgrade.cart_capacity");
        private static readonly LocalizationKey CartSpeedKey = new LocalizationKey("economy.upgrade.cart_speed");
        private static readonly LocalizationKey ConstructionSpeedKey = new LocalizationKey("economy.upgrade.construction_speed");
        private static readonly LocalizationKey InsufficientExpenseKey = new LocalizationKey("economy.notice.expense_unpaid");
        private static readonly LocalizationKey InsufficientUpgradeKey = new LocalizationKey("economy.notice.upgrade_insufficient_gold");
        private static readonly LocalizationKey MaximumLevelKey = new LocalizationKey("economy.notice.maximum_level");

        private readonly GoldWallet wallet;
        private readonly LogisticsInventoryUseCase logistics;
        private readonly Dictionary<ParkingUpgradeId, UpgradeState> upgrades;
        private readonly int driverSalary;
        private readonly int royalTribute;
        private readonly double expenseIntervalSeconds;
        private readonly int cartCapacityPerLevel;
        private readonly double cartSpeedPerLevel;
        private readonly double constructionSpeedPerLevel;
        private double secondsUntilExpenses;
        private LocalizationKey? noticeKey;

        public ParkingEconomyUseCase(
            GoldWallet wallet,
            LogisticsInventoryUseCase logistics,
            int driverSalary,
            int royalTribute,
            double expenseIntervalSeconds,
            int maximumUpgradeLevel,
            int cartCapacityBaseCost,
            int cartCapacityCostStep,
            int cartCapacityPerLevel,
            int cartSpeedBaseCost,
            int cartSpeedCostStep,
            double cartSpeedPerLevel,
            int constructionSpeedBaseCost,
            int constructionSpeedCostStep,
            double constructionSpeedPerLevel)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.logistics = logistics ?? throw new ArgumentNullException(nameof(logistics));
            if (driverSalary <= 0) throw new ArgumentOutOfRangeException(nameof(driverSalary));
            if (royalTribute <= 0) throw new ArgumentOutOfRangeException(nameof(royalTribute));
            if (expenseIntervalSeconds <= 0d) throw new ArgumentOutOfRangeException(nameof(expenseIntervalSeconds));
            if (maximumUpgradeLevel <= 0) throw new ArgumentOutOfRangeException(nameof(maximumUpgradeLevel));
            if (cartCapacityBaseCost <= 0) throw new ArgumentOutOfRangeException(nameof(cartCapacityBaseCost));
            if (cartCapacityCostStep < 0) throw new ArgumentOutOfRangeException(nameof(cartCapacityCostStep));
            if (cartCapacityPerLevel <= 0) throw new ArgumentOutOfRangeException(nameof(cartCapacityPerLevel));
            if (cartSpeedBaseCost <= 0) throw new ArgumentOutOfRangeException(nameof(cartSpeedBaseCost));
            if (cartSpeedCostStep < 0) throw new ArgumentOutOfRangeException(nameof(cartSpeedCostStep));
            if (cartSpeedPerLevel <= 0d) throw new ArgumentOutOfRangeException(nameof(cartSpeedPerLevel));
            if (constructionSpeedBaseCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(constructionSpeedBaseCost));
            if (constructionSpeedCostStep < 0)
                throw new ArgumentOutOfRangeException(nameof(constructionSpeedCostStep));
            if (constructionSpeedPerLevel <= 0d) throw new ArgumentOutOfRangeException(nameof(constructionSpeedPerLevel));

            this.driverSalary = driverSalary;
            this.royalTribute = royalTribute;
            this.expenseIntervalSeconds = expenseIntervalSeconds;
            this.cartCapacityPerLevel = cartCapacityPerLevel;
            this.cartSpeedPerLevel = cartSpeedPerLevel;
            this.constructionSpeedPerLevel = constructionSpeedPerLevel;
            secondsUntilExpenses = expenseIntervalSeconds;
            upgrades = new Dictionary<ParkingUpgradeId, UpgradeState>
            {
                [ParkingUpgradeId.CartCapacity] =
                    new UpgradeState(cartCapacityBaseCost, cartCapacityCostStep, maximumUpgradeLevel),
                [ParkingUpgradeId.CartSpeed] =
                    new UpgradeState(cartSpeedBaseCost, cartSpeedCostStep, maximumUpgradeLevel),
                [ParkingUpgradeId.ConstructionSpeed] =
                    new UpgradeState(constructionSpeedBaseCost, constructionSpeedCostStep, maximumUpgradeLevel)
            };

            wallet.TransactionRecorded += _ =>
            {
                noticeKey = null;
                EconomyChanged?.Invoke();
            };
        }

        public event Action? EconomyChanged;

        public double CartSpeedMultiplier =>
            1d + upgrades[ParkingUpgradeId.CartSpeed].Level * cartSpeedPerLevel;

        public double ConstructionSpeedMultiplier =>
            1d + upgrades[ParkingUpgradeId.ConstructionSpeed].Level * constructionSpeedPerLevel;

        public void Advance(double deltaSeconds)
        {
            if (deltaSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (deltaSeconds == 0d) return;

            secondsUntilExpenses -= deltaSeconds;
            if (secondsUntilExpenses > 0d) return;
            secondsUntilExpenses += expenseIntervalSeconds;
            if (secondsUntilExpenses <= 0d) secondsUntilExpenses = expenseIntervalSeconds;

            var paidSalary = wallet.TryDebit(driverSalary, GoldTransactionKind.Expense, SalaryKey);
            var paidTribute = wallet.TryDebit(royalTribute, GoldTransactionKind.Expense, TributeKey);
            noticeKey = paidSalary && paidTribute ? null : InsufficientExpenseKey;
            EconomyChanged?.Invoke();
        }

        public EconomyActionResult TryPurchaseUpgrade(ParkingUpgradeId id)
        {
            var state = upgrades[id];
            if (state.Level >= state.MaximumLevel)
            {
                noticeKey = MaximumLevelKey;
                EconomyChanged?.Invoke();
                return EconomyActionResult.Failure(EconomyActionFailureReason.MaximumLevelReached);
            }

            var cost = state.NextCost;
            if (!wallet.TryDebit(cost, GoldTransactionKind.Upgrade, GetUpgradeKey(id)))
            {
                noticeKey = InsufficientUpgradeKey;
                EconomyChanged?.Invoke();
                return EconomyActionResult.Failure(EconomyActionFailureReason.InsufficientGold);
            }

            state.Level++;
            if (id == ParkingUpgradeId.CartCapacity)
            {
                logistics.IncreaseCartCapacity(cartCapacityPerLevel);
            }

            noticeKey = null;
            EconomyChanged?.Invoke();
            return EconomyActionResult.Success();
        }

        public ParkingEconomySnapshot GetSnapshot()
        {
            var snapshots = new List<ParkingUpgradeSnapshot>(upgrades.Count);
            foreach (var id in new[]
                     {
                         ParkingUpgradeId.CartCapacity,
                         ParkingUpgradeId.CartSpeed,
                         ParkingUpgradeId.ConstructionSpeed
                     })
            {
                var state = upgrades[id];
                snapshots.Add(new ParkingUpgradeSnapshot(
                    id,
                    state.Level,
                    state.MaximumLevel,
                    state.Level >= state.MaximumLevel ? 0 : state.NextCost,
                    GetEffectPerLevel(id)));
            }

            return new ParkingEconomySnapshot(
                wallet.Balance,
                driverSalary,
                royalTribute,
                Math.Max(0d, secondsUntilExpenses),
                snapshots.AsReadOnly(),
                wallet.LastTransaction,
                noticeKey);
        }

        public bool TryRestoreProgress(
            IReadOnlyDictionary<ParkingUpgradeId, int> restoredLevels,
            double restoredSecondsUntilExpenses)
        {
            if (!CanRestoreProgress(restoredLevels, restoredSecondsUntilExpenses)) return false;

            foreach (var pair in upgrades)
            {
                pair.Value.Level = restoredLevels.TryGetValue(pair.Key, out var restoredLevel)
                    ? restoredLevel
                    : 0;
            }

            secondsUntilExpenses = restoredSecondsUntilExpenses;
            noticeKey = null;
            EconomyChanged?.Invoke();
            return true;
        }

        public bool CanRestoreProgress(
            IReadOnlyDictionary<ParkingUpgradeId, int> restoredLevels,
            double restoredSecondsUntilExpenses)
        {
            if (restoredLevels == null
                || double.IsNaN(restoredSecondsUntilExpenses)
                || restoredSecondsUntilExpenses < 0d
                || restoredSecondsUntilExpenses > expenseIntervalSeconds)
            {
                return false;
            }

            foreach (var pair in upgrades)
            {
                var level = restoredLevels.TryGetValue(pair.Key, out var restoredLevel)
                    ? restoredLevel
                    : 0;
                if (level < 0 || level > pair.Value.MaximumLevel) return false;
            }

            return true;
        }

        private static LocalizationKey GetUpgradeKey(ParkingUpgradeId id)
        {
            return id switch
            {
                ParkingUpgradeId.CartCapacity => CartCapacityKey,
                ParkingUpgradeId.CartSpeed => CartSpeedKey,
                ParkingUpgradeId.ConstructionSpeed => ConstructionSpeedKey,
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
            };
        }

        private double GetEffectPerLevel(ParkingUpgradeId id)
        {
            return id switch
            {
                ParkingUpgradeId.CartCapacity => cartCapacityPerLevel,
                ParkingUpgradeId.CartSpeed => cartSpeedPerLevel,
                ParkingUpgradeId.ConstructionSpeed => constructionSpeedPerLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
            };
        }
    }
}
