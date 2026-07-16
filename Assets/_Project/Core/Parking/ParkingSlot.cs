using System;

namespace HorseParking.Core.Parking
{
    public sealed class ParkingSlot
    {
        public ParkingSlot(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Slot id is required.", nameof(id));
            Id = id;
            State = ParkingSlotState.Free;
        }

        public string Id { get; }
        public ParkingSlotState State { get; private set; }
        public ParkingSession CurrentSession { get; private set; }

        public bool TryPark(string clientId, double currentSeconds)
        {
            if (State != ParkingSlotState.Free) return false;
            CurrentSession = new ParkingSession(clientId, currentSeconds);
            State = ParkingSlotState.Occupied;
            return true;
        }

        public bool TryRequestPayment()
        {
            if (State != ParkingSlotState.Occupied) return false;
            CurrentSession.RequestPayment();
            State = ParkingSlotState.AwaitingPayment;
            return true;
        }

        public bool TryCollectPayment(double currentSeconds, ParkingTariff tariff, out ParkingPayment payment)
        {
            payment = default;
            if (State != ParkingSlotState.AwaitingPayment) return false;
            payment = CurrentSession.CollectPayment(currentSeconds, tariff);
            State = ParkingSlotState.PaymentCollected;
            return true;
        }

        public bool TryRelease()
        {
            if (State != ParkingSlotState.PaymentCollected) return false;
            CurrentSession.Exit();
            CurrentSession = null;
            State = ParkingSlotState.Free;
            return true;
        }
    }
}
