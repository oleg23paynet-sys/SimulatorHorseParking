#nullable enable

using System;
using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    /// <summary>Interaction boundary on the physical cart at the warehouse.</summary>
    public sealed class CartUnloadInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;

        public event Action? InteractionRequested;

        public string Id => "cart-unload-at-warehouse";

        public InteractionAvailability Availability
        {
            get
            {
                if (compositionRoot == null || !compositionRoot.HasCartJourney
                    || !compositionRoot.HasLogisticsInventory)
                {
                    return InteractionAvailability.Unavailable;
                }

                var atWarehouse = compositionRoot.CartJourneyUseCase.GetSnapshot().State
                    == CartJourneyState.AtWarehouse;
                var hasCargo = compositionRoot.LogisticsInventoryUseCase.GetCartSnapshot().UsedCapacityUnits > 0;
                return atWarehouse && hasCargo
                    ? InteractionAvailability.Available
                    : InteractionAvailability.Unavailable;
            }
        }

        public InteractionPrompt Prompt => new InteractionPrompt(
            new LocalizationKey("interaction.cart.unload"),
            new LocalizationKey("interaction.cart.cargo"));

        public void Configure(GameCompositionRoot root) => compositionRoot = root;

        public InteractionResult Interact()
        {
            if (Availability != InteractionAvailability.Available)
            {
                return InteractionResult.Failure(new LocalizationKey("interaction.unavailable"));
            }

            InteractionRequested?.Invoke();
            return InteractionResult.Success(new LocalizationKey("interaction.cart.unload_opened"));
        }
    }
}
