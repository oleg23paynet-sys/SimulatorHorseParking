using HorseParking.Core.Parking;
using HorseParking.Core.Time;

namespace HorseParking.Application.Parking
{
    public sealed class ParkingLifecycleUseCase
    {
        private readonly ParkingSlot slot;
        private readonly ParkingTariff tariff;
        private readonly IGameClock clock;

        public ParkingLifecycleUseCase(ParkingSlot slot, ParkingTariff tariff, IGameClock clock)
        {
            this.slot = slot;
            this.tariff = tariff;
            this.clock = clock;
        }

        public bool TryPark(string clientId) => slot.TryPark(clientId, clock.ElapsedSeconds);
        public bool TryRequestPayment() => slot.TryRequestPayment();
        public bool TryCollectPayment(out ParkingPayment payment) => slot.TryCollectPayment(clock.ElapsedSeconds, tariff, out payment);
        public bool TryReleaseClient() => slot.TryRelease();
    }
}
