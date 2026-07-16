using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    public sealed class ParkingPaymentBagInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private ParkingMvpRuntimeController runtimeController = null!;

        public string Id => "parking-payment-bag-01";
        public InteractionAvailability Availability => runtimeController != null && runtimeController.CanCollectPayment
            ? InteractionAvailability.Available
            : InteractionAvailability.Unavailable;
        public InteractionPrompt Prompt => new(new LocalizationKey("parking.payment.collect"), new LocalizationKey("parking.payment.bag"));

        public void Configure(ParkingMvpRuntimeController controller) => runtimeController = controller;

        public InteractionResult Interact() => runtimeController.TryCollectPayment()
            ? InteractionResult.Success(new LocalizationKey("parking.payment.collected"))
            : InteractionResult.Failure(new LocalizationKey("parking.payment.unavailable"));
    }
}
