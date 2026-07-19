#nullable enable

using System;
using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    public enum CartStationKind
    {
        Warehouse = 0,
        MaterialStore = 1
    }

    public sealed class CartDispatchInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private CartStationKind stationKind;

        public event Action<CartStationKind>? InteractionRequested;

        public string Id => stationKind == CartStationKind.Warehouse
            ? "cart-dispatch-warehouse"
            : "cart-dispatch-material-store";

        public InteractionAvailability Availability => InteractionAvailability.Available;

        public InteractionPrompt Prompt => new InteractionPrompt(
            new LocalizationKey("interaction.cart.manage"),
            new LocalizationKey(stationKind == CartStationKind.Warehouse
                ? "location.warehouse"
                : "location.material_store"));

        public void Configure(CartStationKind kind) => stationKind = kind;

        public InteractionResult Interact()
        {
            InteractionRequested?.Invoke(stationKind);
            return InteractionResult.Success(new LocalizationKey("interaction.cart.opened"));
        }
    }
}
