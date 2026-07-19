#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;

namespace HorseParking.Application.Logistics
{
    public sealed class CartJourneySnapshot
    {
        public CartJourneySnapshot(
            string cartId,
            CartJourneyState state,
            string? destinationId,
            LocalizationKey? destinationNameKey)
        {
            CartId = cartId;
            State = state;
            DestinationId = destinationId;
            DestinationNameKey = destinationNameKey;
        }

        public string CartId { get; }
        public CartJourneyState State { get; }
        public string? DestinationId { get; }
        public LocalizationKey? DestinationNameKey { get; }
    }

    public sealed class CartJourneyUseCase
    {
        private readonly DeliveryCart cart;
        private readonly IReadOnlyDictionary<string, CartDestination> destinations;

        public CartJourneyUseCase(DeliveryCart cart, IEnumerable<CartDestination> destinations)
        {
            this.cart = cart ?? throw new ArgumentNullException(nameof(cart));
            if (destinations == null) throw new ArgumentNullException(nameof(destinations));

            var byId = new Dictionary<string, CartDestination>(StringComparer.Ordinal);
            foreach (var destination in destinations)
            {
                if (!byId.TryAdd(destination.Id, destination))
                {
                    throw new ArgumentException("Duplicate cart destination: " + destination.Id, nameof(destinations));
                }
            }

            this.destinations = byId;
        }

        public CartJourneySnapshot GetSnapshot()
        {
            return new CartJourneySnapshot(
                cart.Id,
                cart.JourneyState,
                cart.Destination?.Id,
                cart.Destination?.DisplayNameKey);
        }

        public CartJourneyResult TryDispatch(string destinationId)
        {
            if (!destinations.TryGetValue(destinationId, out var destination))
            {
                return CartJourneyResult.Failure(CartJourneyFailureReason.UnknownDestination);
            }

            return cart.TryDispatch(destination);
        }

        public CartJourneyResult NotifyArrivedAtDestination() => cart.NotifyArrivedAtDestination();
        public CartJourneyResult TryBeginReturn() => cart.TryBeginReturn();
        public CartJourneyResult NotifyArrivedAtWarehouse() => cart.NotifyArrivedAtWarehouse();
    }
}
