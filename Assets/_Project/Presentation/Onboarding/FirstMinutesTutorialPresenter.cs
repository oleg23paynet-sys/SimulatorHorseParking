#nullable enable

using System.Collections;
using System.Collections.Generic;
using HorseParking.Application.Onboarding;
using HorseParking.Core.Construction;
using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Onboarding
{
    /// <summary>
    /// Unity input/UI adapter for the application-owned tutorial state. Completion is
    /// driven by real gameplay state and successful interactions, never by UI timers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstMinutesTutorialPresenter : MonoBehaviour
    {
        private const int ActionStepCount = (int)TutorialStep.Completed;

        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private FirstPersonPlayerController playerController = null!;
        [SerializeField] private GameObject panel = null!;
        [SerializeField] private Text titleText = null!;
        [SerializeField] private Text instructionText = null!;
        [SerializeField] private Text footerText = null!;
        [SerializeField] private Image progressFill = null!;

        private TutorialFlowUseCase tutorial = null!;
        private bool ready;
        private bool manuallyHidden;
        private float completedHideAt;
        private int initialWarehouseUsed;

        public void Configure(
            GameCompositionRoot root,
            FirstPersonPlayerController player,
            GameObject tutorialPanel,
            Text title,
            Text instruction,
            Text footer,
            Image fill)
        {
            compositionRoot = root;
            playerController = player;
            panel = tutorialPanel;
            titleText = title;
            instructionText = instruction;
            footerText = footer;
            progressFill = fill;
        }

        private IEnumerator Start()
        {
            if (compositionRoot == null || playerController == null || panel == null
                || titleText == null || instructionText == null || footerText == null
                || progressFill == null)
            {
                Debug.LogError("First-minutes tutorial presenter is not configured.", this);
                enabled = false;
                yield break;
            }

            tutorial = compositionRoot.TutorialFlowUseCase;
            tutorial.StepChanged += OnStepChanged;
            playerController.InteractionCompleted += OnInteractionCompleted;
            if (compositionRoot.HasLogisticsInventory)
            {
                compositionRoot.LogisticsInventoryUseCase.CartInventoryChanged += EvaluateWorldProgress;
                compositionRoot.LogisticsInventoryUseCase.WarehouseInventoryChanged += EvaluateWorldProgress;
            }
            if (compositionRoot.HasConstructionRequirements)
            {
                compositionRoot.ConstructionRequirementsUseCase.ConstructionStarted += EvaluateWorldProgress;
                compositionRoot.ConstructionRequirementsUseCase.ConstructionCompleted += EvaluateWorldProgress;
                compositionRoot.ConstructionRequirementsUseCase.WorkerHitBoostApplied += OnWorkerHit;
            }

            // GameSavePresenter restores the main snapshot after one frame. Waiting a
            // second frame makes the tutorial render the restored step, not a stale one.
            yield return null;
            yield return null;
            initialWarehouseUsed = compositionRoot.HasLogisticsInventory
                ? compositionRoot.LogisticsInventoryUseCase.GetWarehouseSnapshot().UsedCapacityUnits
                : 0;
            ready = true;
            EvaluateWorldProgress();
            Refresh();
        }

        private void OnDestroy()
        {
            if (tutorial != null) tutorial.StepChanged -= OnStepChanged;
            if (playerController != null) playerController.InteractionCompleted -= OnInteractionCompleted;
            if (compositionRoot == null) return;
            if (compositionRoot.HasLogisticsInventory)
            {
                compositionRoot.LogisticsInventoryUseCase.CartInventoryChanged -= EvaluateWorldProgress;
                compositionRoot.LogisticsInventoryUseCase.WarehouseInventoryChanged -= EvaluateWorldProgress;
            }
            if (compositionRoot.HasConstructionRequirements)
            {
                compositionRoot.ConstructionRequirementsUseCase.ConstructionStarted -= EvaluateWorldProgress;
                compositionRoot.ConstructionRequirementsUseCase.ConstructionCompleted -= EvaluateWorldProgress;
                compositionRoot.ConstructionRequirementsUseCase.WorkerHitBoostApplied -= OnWorkerHit;
            }
        }

        private void Update()
        {
            if (!ready) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                manuallyHidden = !manuallyHidden;
                panel.SetActive(!manuallyHidden);
            }

            if (tutorial.CurrentStep == TutorialStep.Controls
                && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A)
                    || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)))
            {
                tutorial.TryAdvance(TutorialStep.Controls);
            }
            else if (tutorial.CurrentStep == TutorialStep.OpenEconomy
                     && Input.GetKeyDown(KeyCode.M))
            {
                tutorial.TryAdvance(TutorialStep.OpenEconomy);
            }

            EvaluateWorldProgress();
            if (completedHideAt > 0f && Time.unscaledTime >= completedHideAt)
            {
                completedHideAt = 0f;
                manuallyHidden = true;
                panel.SetActive(false);
            }
        }

        private void OnInteractionCompleted(IInteractionTarget target, InteractionResult result)
        {
            if (!result.Succeeded) return;
            if (tutorial.CurrentStep == TutorialStep.CollectParkingPayment
                && target.Id == "parking-payment-bag-01")
            {
                tutorial.TryAdvance(TutorialStep.CollectParkingPayment);
            }
            else if (tutorial.CurrentStep == TutorialStep.OpenExitGate
                     && target.Id == "parking-exit-gate-01")
            {
                tutorial.TryAdvance(TutorialStep.OpenExitGate);
            }
        }

        private void OnWorkerHit()
        {
            if (tutorial.CurrentStep == TutorialStep.HitConstructionWorker)
                tutorial.TryAdvance(TutorialStep.HitConstructionWorker);
        }

        private void EvaluateWorldProgress()
        {
            if (!ready || !compositionRoot.HasLogisticsInventory) return;

            // Some steps complete through buttons inside existing panels. Their real
            // application state is the authoritative completion signal.
            var cart = compositionRoot.LogisticsInventoryUseCase.GetCartSnapshot();
            if (compositionRoot.HasCartJourney)
            {
                var journey = compositionRoot.CartJourneyUseCase.GetSnapshot();
                if (tutorial.CurrentStep == TutorialStep.SendCartToStore
                    && journey.State != CartJourneyState.AtWarehouse)
                    tutorial.TryAdvance(TutorialStep.SendCartToStore);

                if (tutorial.CurrentStep == TutorialStep.PurchaseMaterials
                    && cart.UsedCapacityUnits > 0)
                    tutorial.TryAdvance(TutorialStep.PurchaseMaterials);

                if (tutorial.CurrentStep == TutorialStep.ReturnCartToWarehouse
                    && (journey.State == CartJourneyState.ReturningToWarehouse
                        || journey.State == CartJourneyState.AtWarehouse))
                    tutorial.TryAdvance(TutorialStep.ReturnCartToWarehouse);

                if (tutorial.CurrentStep == TutorialStep.UnloadCart
                    && journey.State == CartJourneyState.AtWarehouse
                    && cart.UsedCapacityUnits == 0
                    && compositionRoot.LogisticsInventoryUseCase.GetWarehouseSnapshot().UsedCapacityUnits
                    > initialWarehouseUsed)
                    tutorial.TryAdvance(TutorialStep.UnloadCart);
            }

            if (!compositionRoot.HasConstructionRequirements) return;
            var construction = compositionRoot.ConstructionRequirementsUseCase.GetSnapshot();
            if (tutorial.CurrentStep == TutorialStep.StartConstruction
                && construction.State == ConstructionState.InProgress)
                tutorial.TryAdvance(TutorialStep.StartConstruction);

            // Never trap an old/completed save on the hit step when no worker remains.
            if (tutorial.CurrentStep == TutorialStep.HitConstructionWorker
                && construction.State == ConstructionState.Completed)
                tutorial.TryAdvance(TutorialStep.HitConstructionWorker);

            if (tutorial.CurrentStep == TutorialStep.WaitForConstruction
                && construction.State == ConstructionState.Completed)
                tutorial.TryAdvance(TutorialStep.WaitForConstruction);
        }

        private void OnStepChanged(TutorialStep _)
        {
            if (!ready) return;
            manuallyHidden = false;
            panel.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            var step = tutorial.CurrentStep;
            var current = Mathf.Min((int)step + 1, ActionStepCount);
            titleText.text = Translate(
                step == TutorialStep.Completed ? "ui.tutorial.completed_title" : "ui.tutorial.title",
                new Dictionary<string, object>
                {
                    ["current"] = current,
                    ["total"] = ActionStepCount
                });
            instructionText.text = Translate(GetInstructionKey(step));
            footerText.text = Translate("ui.tutorial.footer");
            progressFill.fillAmount = step == TutorialStep.Completed
                ? 1f
                : Mathf.Clamp01((float)(int)step / ActionStepCount);

            if (step == TutorialStep.Completed)
                completedHideAt = Time.unscaledTime + 8f;
        }

        private string Translate(string key, IReadOnlyDictionary<string, object>? values = null) =>
            values == null
                ? compositionRoot.LocalizationService.Translate(new LocalizationKey(key))
                : compositionRoot.LocalizationService.Translate(new LocalizationKey(key), values);

        private static string GetInstructionKey(TutorialStep step) => step switch
        {
            TutorialStep.Controls => "ui.tutorial.controls",
            TutorialStep.CollectParkingPayment => "ui.tutorial.collect_payment",
            TutorialStep.OpenExitGate => "ui.tutorial.open_gate",
            TutorialStep.SendCartToStore => "ui.tutorial.send_cart",
            TutorialStep.PurchaseMaterials => "ui.tutorial.purchase",
            TutorialStep.ReturnCartToWarehouse => "ui.tutorial.return_cart",
            TutorialStep.UnloadCart => "ui.tutorial.unload",
            TutorialStep.StartConstruction => "ui.tutorial.construction",
            TutorialStep.HitConstructionWorker => "ui.tutorial.hit_goblin",
            TutorialStep.WaitForConstruction => "ui.tutorial.wait_construction",
            TutorialStep.OpenEconomy => "ui.tutorial.economy",
            _ => "ui.tutorial.completed"
        };
    }
}
