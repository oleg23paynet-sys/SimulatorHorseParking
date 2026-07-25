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
        [SerializeField] private GameObject completedVisual = null!;
        [SerializeField] private GameObject workersRoot = null!;
        [SerializeField] private Transform[] workers = System.Array.Empty<Transform>();
        [SerializeField] private GameObject progressHudRoot = null!;
        [SerializeField] private Image progressFill = null!;
        [SerializeField] private Text progressText = null!;
        [SerializeField] private GameObject constructionSign = null!;

        private readonly List<float> workerSurfaceWorldHeights = new();
        private readonly List<ConstructionWorkerMotionPresenter> workerMotions = new();
        private ConstructionRequirementsUseCase useCase = null!;
        private Vector3 progressFullScale;
        private Vector3 progressBasePosition;
        private bool isInitialized;
        private bool plannedPreviewVisible;
        private bool wasBuilding;
        private float keepWorkersVisibleUntil = -1f;

        public void Configure(
            GameCompositionRoot root,
            GameObject ghost,
            Transform fillVisual,
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
                || workersRoot == null || progressHudRoot == null || progressFill == null
                || progressText == null)
            {
                Debug.LogError("Construction process presenter is not configured.", this);
                enabled = false;
                return;
            }

            useCase = compositionRoot.ConstructionRequirementsUseCase;
            progressFullScale = progressVisual.localScale;
            progressBasePosition = progressVisual.localPosition;
            workerSurfaceWorldHeights.Clear();
            workerMotions.Clear();
            foreach (var worker in workers)
            {
                // The presenter root is placed on the sampled terrain surface by the scene builder.
                workerSurfaceWorldHeights.Add(transform.position.y);
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
                foreach (var motion in workerMotions)
                {
                    if (!motion.HasReachedBuildPoint) return;
                }

                useCase.AdvanceConstruction(Time.deltaTime);
            }
            else if (state == ConstructionState.Completed
                     && workersRoot.activeSelf
                     && keepWorkersVisibleUntil >= 0f
                     && Time.time >= keepWorkersVisibleUntil)
            {
                workersRoot.SetActive(false);
                keepWorkersVisibleUntil = -1f;
            }
        }

        private void LateUpdate()
        {
            if (workersRoot == null || !workersRoot.activeInHierarchy) return;

            for (var index = 0; index < workers.Length && index < workerSurfaceWorldHeights.Count; index++)
            {
                KeepWorkerOnSurface(workers[index], workerSurfaceWorldHeights[index]);
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

            ghostPreview.SetActive(isBuilding || (isPlanned && plannedPreviewVisible));
            progressVisual.gameObject.SetActive(isBuilding);
            completedVisual.SetActive(isCompleted);
            if (isBuilding && !wasBuilding)
            {
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
                keepWorkersVisibleUntil = Time.time + 1.5f;
                workersRoot.SetActive(true);
            }
            else if (!isBuilding && !isCompleted)
            {
                workersRoot.SetActive(false);
            }
            progressHudRoot.SetActive(isBuilding);
            if (constructionSign != null) constructionSign.SetActive(!isCompleted);

            if (isBuilding)
            {
                var visibleHeight = Mathf.Max(0.025f, progress);
                progressVisual.localScale = new Vector3(
                    progressFullScale.x,
                    progressFullScale.y * visibleHeight,
                    progressFullScale.z);
                // The editor builder places this wrapper's pivot at ground level,
                // so Y scaling reveals the ready FBX from the ground upward.
                progressVisual.localPosition = progressBasePosition;

                progressFill.fillAmount = progress;
                progressText.text = compositionRoot.LocalizationService.Translate(
                    new LocalizationKey("ui.construction.progress"),
                    new Dictionary<string, object>
                    {
                        ["progress"] = Mathf.RoundToInt(progress * 100f)
                    });
            }
            else
            {
                progressVisual.localScale = progressFullScale;
                progressVisual.localPosition = progressBasePosition;
            }

            wasBuilding = isBuilding;
        }

        private static void KeepWorkerOnSurface(Transform worker, float surfaceWorldY)
        {
            // Only the animated body defines the worker's contact with the ground.
            // The hammer deliberately travels below the hands and must not lift the whole worker.
            var renderers = worker.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var correction = surfaceWorldY - bounds.min.y;
            if (Mathf.Abs(correction) < 0.001f) return;

            var position = worker.position;
            position.y += correction;
            worker.position = position;
        }
    }
}
