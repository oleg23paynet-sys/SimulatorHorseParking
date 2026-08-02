#nullable enable

using System.Collections.Generic;
using HorseParking.Application.WorldEvents;
using HorseParking.Core.Localization;
using HorseParking.Core.Parking;
using HorseParking.Core.WorldEvents;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Parking;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.WorldEvents
{
    /// <summary>Localized modal adapter for optional living-world choices.</summary>
    [DisallowMultipleComponent]
    public sealed class LivingWorldEventPresenter : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private ParkingMvpRuntimeController runtimeController = null!;
        [SerializeField] private FirstPersonPlayerController playerController = null!;
        [SerializeField] private GameObject panel = null!;
        [SerializeField] private Text titleText = null!;
        [SerializeField] private Text descriptionText = null!;
        [SerializeField] private Text outcomeText = null!;
        [SerializeField] private Button firstOptionButton = null!;
        [SerializeField] private Text firstOptionText = null!;
        [SerializeField] private Button secondOptionButton = null!;
        [SerializeField] private Text secondOptionText = null!;
        [SerializeField] private Button closeButton = null!;
        [SerializeField] private Text closeButtonText = null!;

        private string firstOptionId = string.Empty;
        private string secondOptionId = string.Empty;
        private int openedEncounterId;
        private int observedEncounterId;
        private bool isOpen;

        public bool CanOpenForCurrentClient
        {
            get
            {
                var archetype = runtimeController != null ? runtimeController.CurrentArchetype : null;
                return archetype != null
                       && runtimeController.CanTalkToClient
                       && compositionRoot != null
                       && compositionRoot.HasLivingWorldEvents
                       && compositionRoot.LivingWorldEventUseCase.CanOffer(
                           archetype.Id,
                           runtimeController.CurrentClientSequence);
            }
        }

        public void Configure(
            GameCompositionRoot root,
            ParkingMvpRuntimeController runtime,
            FirstPersonPlayerController player,
            GameObject eventPanel,
            Text title,
            Text description,
            Text outcome,
            Button firstButton,
            Text firstLabel,
            Button secondButton,
            Text secondLabel,
            Button close,
            Text closeLabel)
        {
            compositionRoot = root;
            runtimeController = runtime;
            playerController = player;
            panel = eventPanel;
            titleText = title;
            descriptionText = description;
            outcomeText = outcome;
            firstOptionButton = firstButton;
            firstOptionText = firstLabel;
            secondOptionButton = secondButton;
            secondOptionText = secondLabel;
            closeButton = close;
            closeButtonText = closeLabel;
        }

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("Living-world event presenter is not configured.", this);
                enabled = false;
                return;
            }

            panel.SetActive(false);
            firstOptionButton.onClick.AddListener(() => Resolve(firstOptionId));
            secondOptionButton.onClick.AddListener(() => Resolve(secondOptionId));
            closeButton.onClick.AddListener(Close);
            runtimeController.ClientArchetypeChanged += HandleClientChanged;
        }

        private void OnDisable()
        {
            if (runtimeController != null)
                runtimeController.ClientArchetypeChanged -= HandleClientChanged;
            if (isOpen && playerController != null)
                playerController.SetUiInputBlocked(false);
            isOpen = false;
        }

        private void Update()
        {
            if (!isOpen)
                return;

            if (runtimeController.CurrentClientSequence != openedEncounterId
                || !runtimeController.CanTalkToClient)
            {
                Close();
            }
        }

        public bool TryOpenForCurrentClient()
        {
            var archetype = runtimeController.CurrentArchetype;
            if (archetype == null
                || !runtimeController.CanTalkToClient
                || !compositionRoot.HasLivingWorldEvents
                || !compositionRoot.LivingWorldEventUseCase.TryOffer(
                    archetype.Id,
                    runtimeController.CurrentClientSequence,
                    out var snapshot))
            {
                return false;
            }

            if (snapshot.Definition.Options.Count < 2)
            {
                Debug.LogError("The current living-world event does not contain two UI choices.", this);
                return false;
            }

            openedEncounterId = snapshot.EncounterId;
            observedEncounterId = snapshot.EncounterId;
            Populate(snapshot);
            panel.SetActive(true);
            isOpen = true;
            playerController.SetUiInputBlocked(true);
            return true;
        }

        public void Close()
        {
            if (!isOpen)
                return;

            isOpen = false;
            panel.SetActive(false);
            playerController.SetUiInputBlocked(false);
        }

        private void Resolve(string optionId)
        {
            if (string.IsNullOrWhiteSpace(optionId))
                return;

            var result = compositionRoot.LivingWorldEventUseCase.TryResolve(optionId);
            var localization = compositionRoot.LocalizationService;
            if (!result.Succeeded)
            {
                outcomeText.color = new Color(1f, 0.55f, 0.35f, 1f);
                outcomeText.text = localization.Translate(GetFailureKey(result.FailureReason));
                return;
            }

            outcomeText.color = result.GoldDelta >= 0
                ? new Color(0.55f, 0.92f, 0.48f, 1f)
                : new Color(1f, 0.69f, 0.35f, 1f);
            outcomeText.text = localization.Translate(
                new LocalizationKey("ui.world_event.outcome"),
                new Dictionary<string, object>
                {
                    ["outcome"] = localization.Translate(result.OutcomeKey!.Value),
                    ["gold"] = FormatSigned(result.GoldDelta)
                });
            firstOptionButton.interactable = false;
            secondOptionButton.interactable = false;
        }

        private void Populate(LivingWorldEventSnapshot snapshot)
        {
            var localization = compositionRoot.LocalizationService;
            var options = snapshot.Definition.Options;
            titleText.text = localization.Translate(snapshot.Definition.TitleKey);
            descriptionText.text = localization.Translate(snapshot.Definition.DescriptionKey);
            outcomeText.text = localization.Translate(new LocalizationKey("ui.world_event.choose"));
            outcomeText.color = new Color(0.88f, 0.82f, 0.68f, 1f);

            firstOptionId = options[0].Id;
            secondOptionId = options[1].Id;
            firstOptionText.text = localization.Translate(options[0].LabelKey);
            secondOptionText.text = localization.Translate(options[1].LabelKey);
            closeButtonText.text = localization.Translate(new LocalizationKey("ui.common.close"));
            firstOptionButton.interactable = true;
            secondOptionButton.interactable = true;
        }

        private void HandleClientChanged(ParkingClientArchetype _)
        {
            if (isOpen)
                Close();
            if (observedEncounterId > 0 && compositionRoot.HasLivingWorldEvents)
                compositionRoot.LivingWorldEventUseCase.EndEncounter(observedEncounterId);
            observedEncounterId = runtimeController.CurrentClientSequence;
        }

        private bool HasRequiredReferences() =>
            compositionRoot != null
            && runtimeController != null
            && playerController != null
            && panel != null
            && titleText != null
            && descriptionText != null
            && outcomeText != null
            && firstOptionButton != null
            && firstOptionText != null
            && secondOptionButton != null
            && secondOptionText != null
            && closeButton != null
            && closeButtonText != null;

        private static LocalizationKey GetFailureKey(LivingWorldEventFailureReason reason) => reason switch
        {
            LivingWorldEventFailureReason.InsufficientGold =>
                new LocalizationKey("event.notice.insufficient_gold"),
            _ => new LocalizationKey("event.notice.unavailable")
        };

        private static string FormatSigned(int amount) => amount > 0 ? "+" + amount : amount.ToString();
    }
}
