#nullable enable

using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>Unity interaction boundary for talking to the active parking client.</summary>
    [DisallowMultipleComponent]
    public sealed class ParkingClientInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private ParkingMvpRuntimeController runtimeController = null!;

        public string Id => "parking-client-dialogue";

        public InteractionAvailability Availability =>
            runtimeController != null && runtimeController.CanTalkToClient
                ? InteractionAvailability.Available
                : InteractionAvailability.Unavailable;

        public InteractionPrompt Prompt => new InteractionPrompt(
            new LocalizationKey("interaction.client.talk"),
            runtimeController != null && runtimeController.CurrentArchetype != null
                ? runtimeController.CurrentArchetype.NameKey
                : new LocalizationKey("interaction.client.target"));

        public void Configure(ParkingMvpRuntimeController runtime)
        {
            runtimeController = runtime;
        }

        public InteractionResult Interact()
        {
            return runtimeController != null && runtimeController.TryTalkToClient()
                ? InteractionResult.Success(new LocalizationKey("interaction.client.spoke"))
                : InteractionResult.Failure(new LocalizationKey("interaction.unavailable"));
        }
    }
}
