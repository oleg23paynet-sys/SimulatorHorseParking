namespace HorseParking.Core.Parking
{
    public readonly struct ParkingPayment
    {
        public ParkingPayment(string clientId, int gold, double parkedSeconds)
        {
            ClientId = clientId;
            Gold = gold;
            ParkedSeconds = parkedSeconds;
        }

        public string ClientId { get; }
        public int Gold { get; }
        public double ParkedSeconds { get; }
    }
}
