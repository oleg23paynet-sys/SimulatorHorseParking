#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Construction;
using HorseParking.Core.Construction;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Construction
{
    /// <summary>
    /// Unity visual adapter for the construction state machine. The application use case
    /// owns time and state; this component only maps snapshots to ready-made assets.
    /// </summary>
    public sealed class ConstructionProcessPresenter : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private GameObject ghostPreview = null!;
        [SerializeField] private Transform progressVisual = null!;
        [SerializeField] private ConstructionBuildingAssemblyPresenter assemblyPresenter = null!;
        [SerializeField] private GameObject completedVisual = null!;
        [SerializeField] private GameObject workersRoot = null!;
        [SerializeField] private Transform[] workers = System.Array.Empty<Transform>();
        [SerializeField] private GameObject progressHudRoot = null!;
        [SerializeField] private Image progressFill = null!;
        [SerializeField] private Text progressText = null!;
        [SerializeField] private GameObject constructionSign = null!;

        private readonly List<ConstructionWorkerMotionPresenter> workerMotions = new();
        private ConstructionRequirementsUseCase useCase = null!;
        private bool isInitialized;
        private bool plannedPreviewVisible;
        private bool wasBuilding;
        private bool workersAreLeaving;

        public void Configure(
            GameCompositionRoot root,
            GameObject ghost,
            Transform fillVisual,
            ConstructionBuildingAssemblyPresenter buildingAssemblyPresenter,
            GameObject completed,
            GameObject workerContainer,
            Transform[] workerTransforms,
            GameObject hudRoot,
            Image fill,
            Text label,
            GameObject sign)
        {
            compositionRoot = root;
            ghostPreview = ghost;
            progressVisual = fillVisual;
            assemblyPresenter = buildingAssemblyPresenter;
            completedVisual = completed;
            workersRoot = workerContainer;
            workers = workerTransforms;
            progressHudRoot = hudRoot;
            progressFill = fill;
            progressText = label;
            constructionSign = sign;
        }

        private void Start()
        {
            if (compositionRoot == null || !compositionRoot.HasConstructionRequirements
                || ghostPreview == null || progressVisual == null || completedVisual == null
                || assemblyPresenter == null
                || workersRoot == null || progressHudRoot == null || progressFill == null
                || progressText == null)
            {
                Debug.LogError("Construction process presenter is not configured.", this);
                enabled = false;
                return;
            }

            useCase = compositionRoot.ConstructionRequirementsUseCase;
            assemblyPresenter.PrepareForConstruction();
            workerMotions.Clear();
            foreach (var worker in workers)
            {
                var motion = worker.GetComponent<ConstructionWorkerMotionPresenter>();
                if (motion == null)
                {
                    Debug.LogError("Construction worker is missing its motion presenter.", worker);
                    enabled = false;
                    return;
                }

                workerMotions.Add(motion);
            }

            useCase.ConstructionStarted += RefreshVisuals;
            useCase.ConstructionProgressChanged += RefreshVisuals;
            useCase.ConstructionCompleted += RefreshVisuals;
            isInitialized = true;
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (useCase == null) return;
            useCase.ConstructionStarted -= RefreshVisuals;
            useCase.ConstructionProgressChanged -= RefreshVisuals;
            useCase.ConstructionCompleted -= RefreshVisuals;
        }

        private void Update()
        {
            if (useCase == null) return;
            var state = useCase.GetSnapshot().State;
            if (state == ConstructionState.InProgress)
            {
                var activeWorkerCount = 0;
                foreach (var motion in workerMotions)
                {
                    if (motion.IsBuildingNow)
                    {
                        activeWorkerCount++;
                    }
                }

                useCase.AdvanceConstruction(Time.deltaTime, activeWorkerCount);
            }
            else if (state == ConstructionState.Completed
                     && workersRoot.activeSelf
                     && workersAreLeaving)
            {
                foreach (var motion in workerMotions)
                {
                    if (!motion.HasExited) return;
                }

                workersRoot.SetActive(false);
                workersAreLeaving = false;
            }
        }

        public void SetPlannedPreviewVisible(bool visible)
        {
            plannedPreviewVisible = visible;
            if (isInitialized) RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            var snapshot = useCase.GetSnapshot();
            var progress = (float)snapshot.NormalizedProgress;
            var isPlanned = snapshot.State == ConstructionState.Planned;
            var isBuilding = snapshot.State == ConstructionState.InProgress;
            var isCompleted = snapshot.State == ConstructionState.Completed;

            // The complete ghost exists only while choosing a project. During construction
            // the authored A/B/C stages are the source of truth for the visible percentage.
            ghostPreview.SetActive(isPlanned && plannedPreviewVisible);
            progressVisual.gameObject.SetActive(isBuilding);
            completedVisual.SetActive(isCompleted);
            if (isBuilding && !wasBuilding)
            {
                assemblyPresenter.PrepareForConstruction();
                workersAreLeaving = false;
                workersRoot.SetActive(true);
                foreach (var worker in workers)
                {
                    worker.gameObject.SetActive(true);
                    var animator = worker.GetComponentInChildren<Animator>();
                    if (animator == null) continue;
                    animator.speed = 1f;
                    animator.Rebind();
                    animator.Update(0f);
                }

                foreach (var motion in workerMotions)
                {
                    motion.BeginApproach();
                }
            }
            else if (!isBuilding && wasBuilding && isCompleted)
            {
                workersRoot.SetActive(true);
                foreach (var motion in workerMotions)
                {
                    motion.BeginDeparture();
                }
                workersAreLeaving = true;
            }
            else if (!isBuilding && !isCompleted)
            {
                workersRoot.SetActive(false);
            }
            progressHudRoot.SetActive(isBuilding);
            if (constructionSign != null) constructionSign.SetActive(!isCompleted);

            if (isBuilding)
            {
                assemblyPresenter.SetProgress(progress);

                progressFill.fillAmount = progress;
                progressText.text = compositionRoot.LocalizationService.Translate(
                    new LocalizationKey("ui.construction.progress"),
                    new Dictionary<string, object>
                    {
                        ["progress"] = Mathf.RoundToInt(progress * 100f)
                    });
            }
            else if (isCompleted)
            {
                assemblyPresenter.CompleteInstantly();
            }

            wasBuilding = isBuilding;
        }
    }
}
