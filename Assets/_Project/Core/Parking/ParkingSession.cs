using System;

namespace HorseParking.Core.Parking
{
    public sealed class ParkingSession
    {
        public ParkingSession(string clientId, double parkedAtSeconds)
        {
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client id is required.", nameof(clientId));
            ClientId = clientId;
            ParkedAtSeconds = parkedAtSeconds;
            State = ParkingClientState.Parked;
        }

        public string ClientId { get; }
        public double ParkedAtSeconds { get; }
        public ParkingClientState State { get; private set; }

        public void RequestPayment()
        {
            if (State != ParkingClientState.Parked) throw new InvalidOperationException("Client cannot request payment now.");
            State = ParkingClientState.AwaitingPayment;
        }

        public ParkingPayment CollectPayment(double currentSeconds, ParkingTariff tariff)
        {
            if (State != ParkingClientState.AwaitingPayment) throw new InvalidOperationException("Payment is not available.");
            var duration = Math.Max(0d, currentSeconds - ParkedAtSeconds);
            State = ParkingClientState.Paid;
            return new ParkingPayment(ClientId, tariff.CalculateGold(duration), duration);
        }

        public void Exit()
        {
            if (State != ParkingClientState.Paid) throw new InvalidOperationException("Client cannot exit before payment.");
            State = ParkingClientState.Exited;
        }
    }
}
