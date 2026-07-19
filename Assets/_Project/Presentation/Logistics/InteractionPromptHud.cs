#nullable enable

using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Logistics
{
    public sealed class InteractionPromptHud : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera = null!;
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private Text promptText = null!;
        [Min(0.5f)] [SerializeField] private float interactionDistance = 3f;

        public void Configure(Camera cameraComponent, GameCompositionRoot root, Text text, float distance)
        {
            playerCamera = cameraComponent;
            compositionRoot = root;
            promptText = text;
            interactionDistance = distance;
        }

        private void Update()
        {
            if (playerCamera == null || compositionRoot == null || promptText == null || Cursor.visible)
            {
                if (promptText != null) promptText.text = string.Empty;
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, interactionDistance)
                && hit.collider.GetComponentInParent(typeof(IInteractionTarget)) is IInteractionTarget target
                && target.Availability == InteractionAvailability.Available)
            {
                SetPrompt(target);
                return;
            }

            if (!compositionRoot.HasCartJourney)
            {
                promptText.text = string.Empty;
                return;
            }

            var state = compositionRoot.CartJourneyUseCase.GetSnapshot().State;
            var guideKey = state switch
            {
                CartJourneyState.AtWarehouse => "ui.cart.guide.warehouse",
                CartJourneyState.AtDestination => "ui.cart.guide.store",
                CartJourneyState.TravelingToDestination => "cart.state.traveling_to_store",
                CartJourneyState.ReturningToWarehouse => "cart.state.returning_to_warehouse",
                _ => "cart.state.unknown"
            };
            promptText.text = compositionRoot.LocalizationService.Translate(new LocalizationKey(guideKey));
        }

        private void SetPrompt(IInteractionTarget target)
        {
            var localization = compositionRoot.LocalizationService;
            if (target is CartUnloadInteractionTarget)
            {
                promptText.text = localization.Translate(new LocalizationKey("ui.cart_unload.prompt"));
                return;
            }

            promptText.text = localization.Translate(
                new LocalizationKey("ui.interaction.prompt"),
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["action"] = localization.Translate(target.Prompt.ActionKey),
                    ["target"] = localization.Translate(target.Prompt.TargetKey)
                });
        }
    }
}
