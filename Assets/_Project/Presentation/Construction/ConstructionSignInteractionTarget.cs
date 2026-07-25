#nullable enable

using System;
using HorseParking.Core.Construction;
using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Construction
{
    /// <summary>Interaction boundary for one predetermined construction sign.</summary>
    public sealed class ConstructionSignInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;

        public event Action? InteractionRequested;

        public string Id => "construction-sign-parking-slot-02";

        public InteractionAvailability Availability =>
            compositionRoot != null
            && compositionRoot.HasConstructionRequirements
            && compositionRoot.ConstructionRequirementsUseCase.GetSnapshot().State != ConstructionState.Completed
                ? InteractionAvailability.Available
                : InteractionAvailability.Unavailable;

        public InteractionPrompt Prompt => new(
            new LocalizationKey("interaction.construction.open"),
            new LocalizationKey("construction.parking_slot"));

        public void Configure(GameCompositionRoot root) => compositionRoot = root;

        public InteractionResult Interact()
        {
            if (Availability != InteractionAvailability.Available)
            {
                return InteractionResult.Failure(new LocalizationKey("interaction.unavailable"));
            }

            InteractionRequested?.Invoke();
            return InteractionResult.Success(new LocalizationKey("interaction.construction.opened"));
        }
    }
}
