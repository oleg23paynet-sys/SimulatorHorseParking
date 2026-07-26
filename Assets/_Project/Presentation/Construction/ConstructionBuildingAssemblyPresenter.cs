#nullable enable

using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace HorseParking.Presentation.Construction
{
    /// <summary>
    /// Replaceable visual adapter for the building assembly effect.
    /// Construction time remains owned by the application use case; this component only
    /// reveals ready-made model parts when normalized construction progress reaches them.
    /// </summary>
    public sealed class ConstructionBuildingAssemblyPresenter : MonoBehaviour
    {
        [SerializeField] private Transform finalConstructionVisual = null!;
        [SerializeField] private GameObject[] authoredStageVisuals = System.Array.Empty<GameObject>();
        [Min(0.05f)] [SerializeField] private float buriedOffset = 0.55f;
        [Range(0.1f, 1f)] [SerializeField] private float hiddenScaleFactor = 0.62f;
        [Min(0.05f)] [SerializeField] private float partRevealDuration = 0.38f;

        private readonly List<AssemblyPart> parts = new();
        private int visiblePartCount;
        private int activeAuthoredStage = int.MinValue;
        private bool isCached;

        public void Configure(
            Transform buildingVisualRoot,
            GameObject[] stageVisuals)
        {
            finalConstructionVisual = buildingVisualRoot;
            authoredStageVisuals = stageVisuals;
        }

        public void PrepareForConstruction()
        {
            EnsurePartsCached();
            visiblePartCount = 0;
            activeAuthoredStage = int.MinValue;

            foreach (var part in parts)
            {
                StopTweens(part);
                part.Target.localPosition = part.FinalLocalPosition;
                part.Target.localScale = part.FinalLocalScale;
                part.Target.gameObject.SetActive(false);
            }

            SetAuthoredStage(-1);
        }

        public void SetProgress(float normalizedProgress)
        {
            EnsurePartsCached();
            var clampedProgress = Mathf.Clamp01(normalizedProgress);

            // Ready KayKit construction stages make the percentage readable:
            // A = foundation, B = walls, C = upper structure. The cyan final-construction
            // visual is revealed only during the roof/details phases. The textured final
            // model is owned by ConstructionProcessPresenter and appears only at 100%.
            var stageIndex = clampedProgress < 0.22f
                ? 0
                : clampedProgress < 0.46f
                    ? 1
                    : clampedProgress < 0.72f
                        ? 2
                        : -1;
            SetAuthoredStage(stageIndex);

            if (parts.Count == 0 || clampedProgress < 0.72f)
            {
                HideAllAssemblyParts();
                return;
            }

            var finalPhaseProgress = Mathf.InverseLerp(0.72f, 0.995f, clampedProgress);
            var requiredVisibleParts = Mathf.Clamp(
                Mathf.CeilToInt(finalPhaseProgress * parts.Count),
                0,
                parts.Count);

            // Never show every construction renderer before the Core state reaches 100%.
            // If the FBX has only one renderer it appears late as a cyan construction
            // silhouette, so it is still visually distinct from the completed building.
            if (parts.Count > 1 && clampedProgress < 1f)
            {
                requiredVisibleParts = Mathf.Min(requiredVisibleParts, parts.Count - 1);
            }
            else if (parts.Count == 1 && finalPhaseProgress < 0.72f)
            {
                requiredVisibleParts = 0;
            }

            while (visiblePartCount < requiredVisibleParts)
            {
                Reveal(parts[visiblePartCount]);
                visiblePartCount++;
            }
        }

        public void CompleteInstantly()
        {
            EnsurePartsCached();
            SetAuthoredStage(-1);

            foreach (var part in parts)
            {
                StopTweens(part);
                part.Target.gameObject.SetActive(false);
                part.Target.localPosition = part.FinalLocalPosition;
                part.Target.localScale = part.FinalLocalScale;
            }

            visiblePartCount = 0;
        }

        private void EnsurePartsCached()
        {
            if (isCached || finalConstructionVisual == null) return;

            var renderers = finalConstructionVisual.GetComponentsInChildren<Renderer>(true);
            var uniqueTargets = new HashSet<Transform>();
            foreach (var renderer in renderers)
            {
                uniqueTargets.Add(renderer.transform);
            }

            // An imported FBX can put a renderer on its root as well as on child parts.
            // Animating both would apply the reveal twice, so prefer the child parts.
            if (uniqueTargets.Count > 1)
            {
                uniqueTargets.Remove(finalConstructionVisual);
            }

            foreach (var target in uniqueTargets)
            {
                var targetRenderer = target.GetComponent<Renderer>();
                var sortPosition = targetRenderer != null
                    ? finalConstructionVisual.InverseTransformPoint(targetRenderer.bounds.center)
                    : target.localPosition;
                parts.Add(new AssemblyPart(
                    target,
                    target.localPosition,
                    target.localScale,
                    sortPosition));
            }

            parts.Sort((left, right) =>
            {
                var heightComparison = left.SortPosition.y.CompareTo(right.SortPosition.y);
                if (heightComparison != 0) return heightComparison;

                var depthComparison = left.SortPosition.z.CompareTo(right.SortPosition.z);
                return depthComparison != 0
                    ? depthComparison
                    : left.SortPosition.x.CompareTo(right.SortPosition.x);
            });
            isCached = true;
        }

        private void SetAuthoredStage(int stageIndex)
        {
            if (activeAuthoredStage == stageIndex) return;

            for (var index = 0; index < authoredStageVisuals.Length; index++)
            {
                var stage = authoredStageVisuals[index];
                if (stage != null)
                {
                    stage.SetActive(index == stageIndex);
                }
            }

            activeAuthoredStage = stageIndex;
        }

        private void HideAllAssemblyParts()
        {
            for (var index = 0; index < visiblePartCount && index < parts.Count; index++)
            {
                var part = parts[index];
                StopTweens(part);
                part.Target.gameObject.SetActive(false);
                part.Target.localPosition = part.FinalLocalPosition;
                part.Target.localScale = part.FinalLocalScale;
            }

            visiblePartCount = 0;
        }

        private void Reveal(AssemblyPart part)
        {
            StopTweens(part);
            part.Target.gameObject.SetActive(true);
            part.Target.localPosition = part.FinalLocalPosition - Vector3.up * buriedOffset;
            part.Target.localScale = part.FinalLocalScale * hiddenScaleFactor;

            part.PositionTween = Tween.LocalPosition(
                part.Target,
                part.FinalLocalPosition,
                partRevealDuration,
                Ease.OutBack);
            part.ScaleTween = Tween.Scale(
                part.Target,
                part.FinalLocalScale,
                partRevealDuration,
                Ease.OutBack);
        }

        private static void StopTweens(AssemblyPart part)
        {
            if (part.PositionTween.isAlive) part.PositionTween.Stop();
            if (part.ScaleTween.isAlive) part.ScaleTween.Stop();
        }

        private sealed class AssemblyPart
        {
            public AssemblyPart(
                Transform target,
                Vector3 finalLocalPosition,
                Vector3 finalLocalScale,
                Vector3 sortPosition)
            {
                Target = target;
                FinalLocalPosition = finalLocalPosition;
                FinalLocalScale = finalLocalScale;
                SortPosition = sortPosition;
            }

            public Transform Target { get; }
            public Vector3 FinalLocalPosition { get; }
            public Vector3 FinalLocalScale { get; }
            public Vector3 SortPosition { get; }
            public Tween PositionTween { get; set; }
            public Tween ScaleTween { get; set; }
        }
    }
}
