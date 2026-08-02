#nullable enable

using HorseParking.Core.Construction;
using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Construction
{
    /// <summary>
    /// Replaceable presentation adapter for striking a construction worker.
    /// It requests a temporary build acceleration and plays ready-made hit feedback.
    /// </summary>
    public sealed class ConstructionWorkerInteractionTarget : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private Animator workerAnimator = null!;
        [SerializeField] private ConstructionWorkerMotionPresenter workerMotion = null!;
        [SerializeField] private ParticleSystem hitEffect = null!;
        [Min(1.01f)] [SerializeField] private float constructionSpeedMultiplier = 1.5f;
        [Min(0.1f)] [SerializeField] private float boostDurationSeconds = 5f;
        [Min(0.1f)] [SerializeField] private float hitCooldownSeconds = 15f;
        [Min(0.5f)] [SerializeField] private float interactionRadius = 5f;

        private static readonly System.Collections.Generic.HashSet<ConstructionWorkerInteractionTarget> ActiveTargets = new();
        private float animatorBoostEndsAt;

        public string Id => "construction-worker";

        public InteractionAvailability Availability =>
            compositionRoot != null
            && compositionRoot.HasConstructionRequirements
            && workerMotion != null
            && workerMotion.IsBuildingNow
            && compositionRoot.ConstructionRequirementsUseCase.CanApplyWorkerHitSpeedBoost
            && compositionRoot.ConstructionRequirementsUseCase.GetSnapshot().State == ConstructionState.InProgress
                ? InteractionAvailability.Available
                : InteractionAvailability.Unavailable;

        public InteractionPrompt Prompt => new(
            new LocalizationKey("interaction.construction_worker.hit"),
            new LocalizationKey("construction.worker"));

        public void Configure(
            GameCompositionRoot root,
            Animator animator,
            ConstructionWorkerMotionPresenter motion,
            ParticleSystem effect)
        {
            compositionRoot = root;
            workerAnimator = animator;
            workerMotion = motion;
            hitEffect = effect;
        }

        public InteractionResult Interact()
        {
            if (Availability != InteractionAvailability.Available
                || !compositionRoot.ConstructionRequirementsUseCase.TryApplyWorkerHitSpeedBoost(
                    constructionSpeedMultiplier,
                    boostDurationSeconds,
                    hitCooldownSeconds))
            {
                return InteractionResult.Failure(new LocalizationKey("interaction.unavailable"));
            }

            if (workerAnimator != null)
            {
                workerAnimator.speed = constructionSpeedMultiplier;
                workerAnimator.SetTrigger("wasHit");
                animatorBoostEndsAt = Time.time + boostDurationSeconds;
            }
            hitEffect?.Emit(10);

            return InteractionResult.Success(new LocalizationKey("interaction.construction_worker.hit_done"));
        }

        private void Update()
        {
            if (workerAnimator != null
                && animatorBoostEndsAt > 0f
                && Time.time >= animatorBoostEndsAt)
            {
                workerAnimator.speed = 1f;
                animatorBoostEndsAt = 0f;
            }
        }

        private void OnEnable()
        {
            ActiveTargets.Add(this);
        }

        private void OnDisable()
        {
            ActiveTargets.Remove(this);
        }

        public static bool TryGetNearestAvailable(Vector3 playerPosition, out IInteractionTarget target)
        {
            ConstructionWorkerInteractionTarget? nearest = null;
            var nearestSqrDistance = float.MaxValue;
            foreach (var candidate in ActiveTargets)
            {
                if (candidate == null || candidate.Availability != InteractionAvailability.Available) continue;
                var sqrDistance = (candidate.transform.position - playerPosition).sqrMagnitude;
                var radius = candidate.interactionRadius;
                if (sqrDistance > radius * radius || sqrDistance >= nearestSqrDistance) continue;
                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            target = nearest!;
            return nearest != null;
        }
    }
}
