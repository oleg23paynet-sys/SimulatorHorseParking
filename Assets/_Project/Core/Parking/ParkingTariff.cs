using System;

namespace HorseParking.Core.Parking
{
    public sealed class ParkingTariff
    {
        public ParkingTariff(double billingPeriodSeconds, int goldPerPeriod)
        {
            if (billingPeriodSeconds <= 0d) throw new ArgumentOutOfRangeException(nameof(billingPeriodSeconds));
            if (goldPerPeriod <= 0) throw new ArgumentOutOfRangeException(nameof(goldPerPeriod));
            BillingPeriodSeconds = billingPeriodSeconds;
            GoldPerPeriod = goldPerPeriod;
        }

        public double BillingPeriodSeconds { get; }
        public int GoldPerPeriod { get; }

        public int CalculateGold(double parkedSeconds)
        {
            var periods = Math.Max(1d, Math.Ceiling(Math.Max(0d, parkedSeconds) / BillingPeriodSeconds));
            return checked((int)periods * GoldPerPeriod);
        }
    }
}
