#nullable enable

using HorseParking.Application.Economy;
using HorseParking.Application.Logistics;
using UnityEngine;

namespace HorseParking.Presentation.Economy
{
    /// <summary>Editable Stage 4 balance values; no economic number is hard-coded in UI.</summary>
    [CreateAssetMenu(fileName = "ParkingEconomySettings", menuName = "Horse Parking/Parking Economy Settings")]
    public sealed class ParkingEconomySettings : ScriptableObject
    {
        [Header("Parking tariff")]
        [Min(1f)] [SerializeField] private float billingPeriodSeconds = 20f;
        [Min(1)] [SerializeField] private int goldPerBillingPeriod = 3;

        [Header("Regular expenses")]
        [Min(10f)] [SerializeField] private float expenseIntervalSeconds = 180f;
        [Min(1)] [SerializeField] private int driverSalary = 8;
        [Min(1)] [SerializeField] private int royalTribute = 12;

        [Header("Upgrade progression")]
        [Min(1)] [SerializeField] private int maximumUpgradeLevel = 3;
        [Min(1)] [SerializeField] private int cartCapacityBaseCost = 30;
        [Min(0)] [SerializeField] private int cartCapacityCostStep = 20;
        [Min(1)] [SerializeField] private int cartCapacityPerLevel = 8;
        [Min(1)] [SerializeField] private int cartSpeedBaseCost = 40;
        [Min(0)] [SerializeField] private int cartSpeedCostStep = 25;
        [Min(0.01f)] [SerializeField] private float cartSpeedPerLevel = 0.20f;
        [Min(1)] [SerializeField] private int constructionSpeedBaseCost = 35;
        [Min(0)] [SerializeField] private int constructionSpeedCostStep = 25;
        [Min(0.01f)] [SerializeField] private float constructionSpeedPerLevel = 0.25f;

        public double BillingPeriodSeconds => billingPeriodSeconds;
        public int GoldPerBillingPeriod => goldPerBillingPeriod;

        public ParkingEconomyUseCase CreateUseCase(LogisticsInventoryUseCase logistics)
        {
            return new ParkingEconomyUseCase(
                logistics.Wallet,
                logistics,
                driverSalary,
                royalTribute,
                expenseIntervalSeconds,
                maximumUpgradeLevel,
                cartCapacityBaseCost,
                cartCapacityCostStep,
                cartCapacityPerLevel,
                cartSpeedBaseCost,
                cartSpeedCostStep,
                cartSpeedPerLevel,
                constructionSpeedBaseCost,
                constructionSpeedCostStep,
                constructionSpeedPerLevel);
        }
    }
}
