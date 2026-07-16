using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    public sealed class ParkingExitGateInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private ParkingMvpRuntimeController runtimeController = null!;

        public string Id => "parking-exit-gate-01";
        public InteractionAvailability Availability => runtimeController != null && runtimeController.CanOpenExit
            ? InteractionAvailability.Available
            : InteractionAvailability.Unavailable;
        public InteractionPrompt Prompt => new(new LocalizationKey("parking.exit.open"), new LocalizationKey("parking.exit.gate"));

        public void Configure(ParkingMvpRuntimeController controller) => runtimeController = controller;

        public InteractionResult Interact() => runtimeController.TryOpenExit()
            ? InteractionResult.Success(new LocalizationKey("parking.exit.opened"))
            : InteractionResult.Failure(new LocalizationKey("parking.exit.unavailable"));
    }
}
