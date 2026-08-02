#nullable enable

using HorseParking.Core.Localization;
using HorseParking.Core.Parking;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Parking
{
    /// <summary>
    /// Temporary, compact dialogue card. It renders application-selected localized
    /// lines and never owns client state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParkingClientDialoguePresenter : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private ParkingMvpRuntimeController runtimeController = null!;
        [SerializeField] private ParkingClientDialogueSettings settings = null!;
        [SerializeField] private GameObject panel = null!;
        [SerializeField] private CanvasGroup canvasGroup = null!;
        [SerializeField] private Image accent = null!;
        [SerializeField] private Text speakerText = null!;
        [SerializeField] private Text reactionText = null!;
        [SerializeField] private Text lineText = null!;

        private float visibleUntil;
        private bool subscribed;
        private bool hiding;

        public void Configure(
            GameCompositionRoot root,
            ParkingMvpRuntimeController runtime,
            ParkingClientDialogueSettings dialogueSettings,
            GameObject dialoguePanel,
            CanvasGroup group,
            Image accentImage,
            Text speaker,
            Text reaction,
            Text line)
        {
            compositionRoot = root;
            runtimeController = runtime;
            settings = dialogueSettings;
            panel = dialoguePanel;
            canvasGroup = group;
            accent = accentImage;
            speakerText = speaker;
            reactionText = reaction;
            lineText = line;
        }

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("Parking client dialogue presenter is not configured.", this);
                enabled = false;
                return;
            }

            canvasGroup.alpha = 0f;
            panel.SetActive(false);
            Subscribe();
        }

        private void OnEnable()
        {
            if (runtimeController != null)
                Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!panel.activeSelf)
                return;

            if (!hiding && Time.unscaledTime >= visibleUntil)
                hiding = true;

            var target = hiding ? 0f : 1f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.unscaledDeltaTime * 5f);
            if (hiding && canvasGroup.alpha <= 0.001f)
                panel.SetActive(false);
        }

        private void ShowDialogue(
            ParkingClientArchetype archetype,
            ParkingClientDialogueMoment moment)
        {
            if (!compositionRoot.HasParkingClientDialogue
                || !compositionRoot.ParkingClientDialogueUseCase.TrySelectLine(
                    archetype,
                    moment,
                    out var line))
            {
                return;
            }

            var localization = compositionRoot.LocalizationService;
            speakerText.text = localization.Translate(archetype.NameKey);
            reactionText.text = localization.Translate(GetReactionKey(line.Reaction));
            lineText.text = localization.Translate(line.TextKey);
            accent.color = GetReactionColor(line.Reaction);

            panel.SetActive(true);
            canvasGroup.alpha = 0f;
            hiding = false;
            visibleUntil = Time.unscaledTime
                           + (moment == ParkingClientDialogueMoment.PlayerGreeting
                               ? settings.InteractionLineDurationSeconds
                               : settings.AutomaticLineDurationSeconds);
        }

        private void Subscribe()
        {
            if (subscribed || runtimeController == null) return;
            runtimeController.ClientDialogueRequested += ShowDialogue;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || runtimeController == null) return;
            runtimeController.ClientDialogueRequested -= ShowDialogue;
            subscribed = false;
        }

        private bool HasRequiredReferences()
        {
            return compositionRoot != null
                   && runtimeController != null
                   && settings != null
                   && panel != null
                   && canvasGroup != null
                   && accent != null
                   && speakerText != null
                   && reactionText != null
                   && lineText != null;
        }

        private static LocalizationKey GetReactionKey(ParkingClientReaction reaction)
        {
            var suffix = reaction switch
            {
                ParkingClientReaction.Friendly => "friendly",
                ParkingClientReaction.Impatient => "impatient",
                ParkingClientReaction.Suspicious => "suspicious",
                ParkingClientReaction.Satisfied => "satisfied",
                _ => "neutral"
            };
            return new LocalizationKey("client.reaction." + suffix);
        }

        private static Color GetReactionColor(ParkingClientReaction reaction)
        {
            return reaction switch
            {
                ParkingClientReaction.Friendly => new Color(0.35f, 0.78f, 0.42f, 1f),
                ParkingClientReaction.Impatient => new Color(0.95f, 0.53f, 0.19f, 1f),
                ParkingClientReaction.Suspicious => new Color(0.67f, 0.48f, 0.93f, 1f),
                ParkingClientReaction.Satisfied => new Color(0.92f, 0.76f, 0.24f, 1f),
                _ => new Color(0.70f, 0.70f, 0.66f, 1f)
            };
        }
    }
}
