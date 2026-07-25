#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Construction;
using HorseParking.Application.Logistics;
using HorseParking.Core.Construction;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Construction
{
    /// <summary>
    /// Requirements screen and Stage 3.6 start command. Visual progress is presented
    /// independently so closing this window never pauses construction.
    /// </summary>
    public sealed class ConstructionRequirementsPanel : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private FirstPersonPlayerController playerController = null!;
        [SerializeField] private ConstructionSignInteractionTarget interactionTarget = null!;
        [SerializeField] private GameObject panelRoot = null!;
        [SerializeField] private Text titleText = null!;
        [SerializeField] private Text projectText = null!;
        [SerializeField] private Button projectOptionButton = null!;
        [SerializeField] private Text projectOptionText = null!;
        [SerializeField] private Text requirementsHeadingText = null!;
        [SerializeField] private Text feedbackText = null!;
        [SerializeField] private Text requirementRowTemplate = null!;
        [SerializeField] private Button startButton = null!;
        [SerializeField] private Text startButtonText = null!;
        [SerializeField] private Button closeButton = null!;
        [SerializeField] private Text closeButtonText = null!;

        private readonly List<Text> requirementRows = new();
        private ConstructionRequirementsUseCase requirementsUseCase = null!;
        private LogisticsInventoryUseCase inventoryUseCase = null!;
        private ConstructionProcessPresenter? previewPresenter;
        private bool isOpen;
        private bool isProjectSelected;
        private string? feedbackOverrideKey;

        public void Configure(
            GameCompositionRoot root,
            FirstPersonPlayerController player,
            ConstructionSignInteractionTarget target,
            GameObject panel,
            Text title,
            Text project,
            Button projectOption,
            Text projectOptionLabel,
            Text requirementsHeading,
            Text feedback,
            Text rowTemplate,
            Button start,
            Text startLabel,
            Button close,
            Text closeLabel)
        {
            compositionRoot = root;
            playerController = player;
            interactionTarget = target;
            panelRoot = panel;
            titleText = title;
            projectText = project;
            projectOptionButton = projectOption;
            projectOptionText = projectOptionLabel;
            requirementsHeadingText = requirementsHeading;
            feedbackText = feedback;
            requirementRowTemplate = rowTemplate;
            startButton = start;
            startButtonText = startLabel;
            closeButton = close;
            closeButtonText = closeLabel;
        }

        public void BindPreviewPresenter(ConstructionProcessPresenter presenter)
        {
            previewPresenter = presenter;
        }

        private void Start()
        {
            if (compositionRoot == null || playerController == null || interactionTarget == null
                || panelRoot == null || requirementRowTemplate == null
                || projectOptionButton == null || projectOptionText == null)
            {
                Debug.LogError("Construction requirements panel is not configured.", this);
                enabled = false;
                return;
            }

            requirementsUseCase = compositionRoot.ConstructionRequirementsUseCase;
            inventoryUseCase = compositionRoot.LogisticsInventoryUseCase;
            interactionTarget.InteractionRequested += Open;
            inventoryUseCase.WarehouseInventoryChanged += Refresh;
            projectOptionButton.onClick.AddListener(SelectProject);
            startButton.onClick.AddListener(ConfirmRequirements);
            closeButton.onClick.AddListener(Close);
            requirementRowTemplate.gameObject.SetActive(false);
            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (interactionTarget != null) interactionTarget.InteractionRequested -= Open;
            if (inventoryUseCase != null) inventoryUseCase.WarehouseInventoryChanged -= Refresh;
        }

        private void Update()
        {
            if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void Open()
        {
            feedbackOverrideKey = null;
            isProjectSelected = false;
            previewPresenter?.SetPlannedPreviewVisible(false);
            isOpen = true;
            panelRoot.SetActive(true);
            playerController.SetUiInputBlocked(true);
            Refresh();
        }

        private void Close()
        {
            isOpen = false;
            isProjectSelected = false;
            previewPresenter?.SetPlannedPreviewVisible(false);
            panelRoot.SetActive(false);
            playerController.SetUiInputBlocked(false);
        }

        private void ConfirmRequirements()
        {
            if (!isProjectSelected)
            {
                feedbackOverrideKey = "ui.construction.status.choose";
                Refresh();
                return;
            }

            var result = requirementsUseCase.TryStartConstruction();
            feedbackOverrideKey = result.Succeeded
                ? "ui.construction.status.started"
                : result.FailureReason == ConstructionStartFailureReason.MissingResources
                    ? "ui.construction.status.missing"
                    : result.FailureReason == ConstructionStartFailureReason.AlreadyCompleted
                        ? "ui.construction.status.completed"
                        : "ui.construction.status.in_progress";
            Refresh();
            if (result.Succeeded) Close();
        }

        private void SelectProject()
        {
            var snapshot = requirementsUseCase.GetSnapshot();
            if (!snapshot.CanStart) return;

            isProjectSelected = true;
            feedbackOverrideKey = null;
            previewPresenter?.SetPlannedPreviewVisible(true);
            Refresh();
        }

        private void Refresh()
        {
            if (!isOpen) return;

            var snapshot = requirementsUseCase.GetSnapshot();
            if (!snapshot.CanStart && isProjectSelected)
            {
                isProjectSelected = false;
                previewPresenter?.SetPlannedPreviewVisible(false);
            }

            EnsureRowCount(snapshot.Requirements.Count);
            var localization = compositionRoot.LocalizationService;

            titleText.text = localization.Translate(new LocalizationKey("ui.construction.title"));
            projectText.text = localization.Translate(
                new LocalizationKey("ui.construction.choose_project"));
            projectOptionText.text = localization.Translate(
                new LocalizationKey(snapshot.CanStart
                    ? "ui.construction.option.available"
                    : "ui.construction.option.locked"),
                new Dictionary<string, object>
                {
                    ["project"] = localization.Translate(snapshot.PlanDisplayNameKey)
                });
            projectOptionButton.interactable = snapshot.CanStart
                && snapshot.State == ConstructionState.Planned;
            requirementsHeadingText.text = localization.Translate(
                new LocalizationKey("ui.construction.requirements"));
            startButtonText.text = localization.Translate(new LocalizationKey("ui.construction.start"));
            closeButtonText.text = localization.Translate(new LocalizationKey("ui.common.close"));
            startButton.interactable = snapshot.CanStart && isProjectSelected;

            for (var index = 0; index < snapshot.Requirements.Count; index++)
            {
                var requirement = snapshot.Requirements[index];
                var row = requirementRows[index];
                row.gameObject.SetActive(true);
                row.color = requirement.IsSatisfied
                    ? new Color(0.55f, 1f, 0.55f, 1f)
                    : new Color(1f, 0.65f, 0.38f, 1f);
                row.text = localization.Translate(
                    new LocalizationKey("ui.construction.requirement"),
                    new Dictionary<string, object>
                    {
                        ["resource"] = localization.Translate(requirement.DisplayNameKey),
                        ["available"] = requirement.AvailableQuantity,
                        ["required"] = requirement.RequiredQuantity,
                        ["missing"] = requirement.MissingQuantity
                    });
            }

            for (var index = snapshot.Requirements.Count; index < requirementRows.Count; index++)
            {
                requirementRows[index].gameObject.SetActive(false);
            }

            var feedbackKey = feedbackOverrideKey
                ?? (snapshot.State == ConstructionState.Completed
                    ? "ui.construction.status.completed"
                    : snapshot.State == ConstructionState.InProgress
                        ? "ui.construction.status.in_progress"
                        : snapshot.CanStart && !isProjectSelected
                            ? "ui.construction.status.choose"
                            : snapshot.CanStart
                            ? "ui.construction.status.ready"
                            : "ui.construction.status.missing");
            feedbackText.text = localization.Translate(new LocalizationKey(feedbackKey));
            feedbackText.color = snapshot.CanStart || snapshot.State == ConstructionState.Completed
                ? new Color(0.55f, 1f, 0.55f, 1f)
                : new Color(1f, 0.7f, 0.3f, 1f);
        }

        private void EnsureRowCount(int count)
        {
            while (requirementRows.Count < count)
            {
                var row = Instantiate(requirementRowTemplate, requirementRowTemplate.transform.parent);
                row.name = "Requirement_" + requirementRows.Count;
                var rect = row.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, 25f - requirementRows.Count * 58f);
                rect.sizeDelta = new Vector2(740f, 48f);
                requirementRows.Add(row);
            }
        }
    }
}
