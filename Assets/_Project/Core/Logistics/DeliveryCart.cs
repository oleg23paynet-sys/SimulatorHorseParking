#nullable enable

using System;
using HorseParking.Core.Localization;

namespace HorseParking.Core.Logistics
{
    public enum CartJourneyState
    {
        AtWarehouse = 0,
        TravelingToDestination = 1,
        AtDestination = 2,
        ReturningToWarehouse = 3
    }

    public enum CartJourneyFailureReason
    {
        None = 0,
        UnknownDestination = 1,
        CartIsNotAtWarehouse = 2,
        CartIsNotAtDestination = 3,
        InvalidTransition = 4
    }

    public readonly struct CartJourneyResult
    {
        private CartJourneyResult(bool succeeded, CartJourneyFailureReason failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public bool Succeeded { get; }
        public CartJourneyFailureReason FailureReason { get; }

        public static CartJourneyResult Success() => new CartJourneyResult(true, CartJourneyFailureReason.None);
        public static CartJourneyResult Failure(CartJourneyFailureReason reason) => new CartJourneyResult(false, reason);
    }

    public sealed class CartDestination
    {
        public CartDestination(string id, LocalizationKey displayNameKey)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Destination id is required.", nameof(id));
            Id = id.Trim();
            DisplayNameKey = displayNameKey;
        }

        public string Id { get; }
        public LocalizationKey DisplayNameKey { get; }
    }

    public sealed class DeliveryCart
    {
        private CartDestination? destination;

        public DeliveryCart(string id, ResourceInventory cargo)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Cart id is required.", nameof(id));
            Cargo = cargo ?? throw new ArgumentNullException(nameof(cargo));
            Id = id.Trim();
            JourneyState = CartJourneyState.AtWarehouse;
        }

        public string Id { get; }
        public ResourceInventory Cargo { get; }
        public CartJourneyState JourneyState { get; private set; }
        public CartDestination? Destination => destination;

        public CartJourneyResult TryDispatch(CartDestination requestedDestination)
        {
            if (requestedDestination == null) throw new ArgumentNullException(nameof(requestedDestination));
            if (JourneyState != CartJourneyState.AtWarehouse)
            {
                return CartJourneyResult.Failure(CartJourneyFailureReason.CartIsNotAtWarehouse);
            }

            destination = requestedDestination;
            JourneyState = CartJourneyState.TravelingToDestination;
            return CartJourneyResult.Success();
        }

        public CartJourneyResult NotifyArrivedAtDestination()
        {
            if (JourneyState != CartJourneyState.TravelingToDestination || destination == null)
            {
                return CartJourneyResult.Failure(CartJourneyFailureReason.InvalidTransition);
            }

            JourneyState = CartJourneyState.AtDestination;
            return CartJourneyResult.Success();
        }

        public CartJourneyResult TryBeginReturn()
        {
            if (JourneyState != CartJourneyState.AtDestination)
            {
                return CartJourneyResult.Failure(CartJourneyFailureReason.CartIsNotAtDestination);
            }

            JourneyState = CartJourneyState.ReturningToWarehouse;
            return CartJourneyResult.Success();
        }

        public CartJourneyResult NotifyArrivedAtWarehouse()
        {
            if (JourneyState != CartJourneyState.ReturningToWarehouse)
            {
                return CartJourneyResult.Failure(CartJourneyFailureReason.InvalidTransition);
            }

            destination = null;
            JourneyState = CartJourneyState.AtWarehouse;
            return CartJourneyResult.Success();
        }
    }
}
