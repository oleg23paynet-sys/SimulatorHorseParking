#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace HorseParking.Presentation.Construction
{
    /// <summary>
    /// Replaceable navigation/presentation adapter for a construction worker.
    /// NavMesh owns world movement, ready-made clips own body motion, and a ready-made
    /// VFX prefab owns emergence/disappearance. Construction Core has no Unity dependency.
    /// </summary>
    public sealed class ConstructionWorkerMotionPresenter : MonoBehaviour
    {
        private static readonly int IsWalking = Animator.StringToHash("isWalking");
        private static readonly int IsBuilding = Animator.StringToHash("isBuilding");
        private static readonly int IsEmerging = Animator.StringToHash("isEmerging");
        private static readonly int IsDisappearing = Animator.StringToHash("isDisappearing");
        private static readonly HashSet<EntityId> ReservedSpawnPoints = new();

        [SerializeField] private Animator workerAnimator = null!;
        [SerializeField] private NavMeshAgent navigationAgent = null!;
        [SerializeField] private GameObject hammerVisual = null!;
        [SerializeField] private GameObject groundVortex = null!;
        [SerializeField] private Transform[] spawnPoints = System.Array.Empty<Transform>();
        [SerializeField] private int preferredSpawnPointIndex;
        [SerializeField] private Transform workPoint = null!;
        [SerializeField] private Transform exitPoint = null!;
        [SerializeField] private Transform constructionSite = null!;
        [SerializeField] private LayerMask spawnBlockingMask = -1;
        [Min(0.1f)] [SerializeField] private float moveSpeed = 1.35f;
        [Min(0.05f)] [SerializeField] private float emergenceDuration = 1.2f;
        [Min(0.05f)] [SerializeField] private float disappearanceDuration = 1f;
        [Min(0.01f)] [SerializeField] private float stoppingDistance = 0.12f;
        [Min(0.1f)] [SerializeField] private float routePointSampleRadius = 0.7f;
        [Min(0.05f)] [SerializeField] private float workPointSampleRadius = 0.5f;
        [Min(0.05f)] [SerializeField] private float spawnCheckRadius = 0.42f;
        [Min(0.1f)] [SerializeField] private float spawnCheckBottomHeight = 0.52f;
        [Min(0.2f)] [SerializeField] private float spawnCheckTopHeight = 1.58f;
        [Min(0.1f)] [SerializeField] private float minimumSiteDistance = 1.45f;
        [Min(0.1f)] [SerializeField] private float spawnRetryDelay = 1.25f;
        [Min(0.05f)] [SerializeField] private float vortexLeadInDuration = 0.18f;

        private WorkerPhase phase = WorkerPhase.Hidden;
        private float phaseEndsAt;
        private float nextSpawnRetryAt;
        private Collider[] workerColliders = System.Array.Empty<Collider>();
        private Renderer[] workerRenderers = System.Array.Empty<Renderer>();
        private ParticleSystem[] vortexSystems = System.Array.Empty<ParticleSystem>();
        private Vector3 sampledSpawnPoint;
        private Vector3 sampledBuildPoint;
        private Vector3 sampledExitPoint;
        private EntityId reservedSpawnPointId;
        private bool hasSpawnReservation;
        private readonly List<int> shuffledSpawnPointIndices = new();

        public bool HasReachedBuildPoint { get; private set; }
        public bool IsBuildingNow { get; private set; }
        public bool HasExited { get; private set; }

        public void Configure(
            Animator animator,
            NavMeshAgent agent,
            GameObject hammer,
            GameObject vortex,
            Transform[] emergencePoints,
            int preferredEmergencePointIndex,
            Transform constructionWorkPoint,
            Transform disappearancePoint,
            Transform site)
        {
            workerAnimator = animator;
            navigationAgent = agent;
            hammerVisual = hammer;
            groundVortex = vortex;
            spawnPoints = emergencePoints;
            preferredSpawnPointIndex = preferredEmergencePointIndex;
            workPoint = constructionWorkPoint;
            exitPoint = disappearancePoint;
            constructionSite = site;
            navigationAgent.speed = moveSpeed;
            navigationAgent.stoppingDistance = stoppingDistance;

            CachePresentationParts();
            SetWorkerVisible(false);
            SetWorkerCollidersEnabled(false);
            SetHammerVisible(false);
            StopGroundVortex();
        }

        public void BeginApproach()
        {
            ReleaseSpawnReservation();
            HasReachedBuildPoint = false;
            IsBuildingNow = false;
            HasExited = false;
            phase = WorkerPhase.WaitingForSpawn;
            nextSpawnRetryAt = Time.time;

            CachePresentationParts();
            SetHammerVisible(false);
            SetWorkerVisible(false);
            SetWorkerCollidersEnabled(false);
            StopGroundVortex();
            if (navigationAgent.enabled)
            {
                navigationAgent.enabled = false;
            }

            TryStartApproach();
        }

        public void BeginDeparture()
        {
            if (phase == WorkerPhase.WalkingToExit
                || phase == WorkerPhase.Disappearing
                || phase == WorkerPhase.Exited)
            {
                return;
            }

            IsBuildingNow = false;
            SetHammerVisible(false);
            workerAnimator.speed = 1f;
            navigationAgent.nextPosition = sampledBuildPoint;
            transform.position = sampledBuildPoint;
            navigationAgent.updatePosition = true;
            navigationAgent.updateRotation = true;
            SetAnimatorState(
                isWalking: true,
                isBuilding: false,
                isEmerging: false,
                isDisappearing: false);
            phase = WorkerPhase.WalkingToExit;
            SetDestination(sampledExitPoint);
        }

        private void Update()
        {
            switch (phase)
            {
                case WorkerPhase.WaitingForSpawn:
                    if (Time.time >= nextSpawnRetryAt)
                    {
                        TryStartApproach();
                    }
                    break;

                case WorkerPhase.VortexLeadIn:
                    if (Time.time >= phaseEndsAt)
                    {
                        SetWorkerVisible(true);
                        phase = WorkerPhase.Emerging;
                        phaseEndsAt = Time.time + Mathf.Max(
                            0.05f,
                            emergenceDuration - vortexLeadInDuration);
                    }
                    break;

                case WorkerPhase.Emerging:
                    if (Time.time >= phaseEndsAt)
                    {
                        StopGroundVortex();
                        if (!EnableNavigationAt(sampledSpawnPoint))
                        {
                            SetWorkerVisible(false);
                            phase = WorkerPhase.WaitingForSpawn;
                            nextSpawnRetryAt = Time.time + spawnRetryDelay;
                            return;
                        }

                        ReleaseSpawnReservation();
                        SetWorkerCollidersEnabled(true);
                        phase = WorkerPhase.WalkingToBuild;
                        SetAnimatorState(
                            isWalking: true,
                            isBuilding: false,
                            isEmerging: false,
                            isDisappearing: false);
                        SetDestination(sampledBuildPoint);
                    }
                    break;

                case WorkerPhase.WalkingToBuild:
                    if (HasArrived())
                    {
                        BeginBuilding();
                    }
                    break;

                case WorkerPhase.WalkingToExit:
                    if (HasArrived())
                    {
                        navigationAgent.isStopped = true;
                        navigationAgent.ResetPath();
                        navigationAgent.enabled = false;
                        SetWorkerCollidersEnabled(false);
                        SetAnimatorState(
                            isWalking: false,
                            isBuilding: false,
                            isEmerging: false,
                            isDisappearing: true);
                        PlayGroundVortex();
                        phase = WorkerPhase.Disappearing;
                        phaseEndsAt = Time.time + disappearanceDuration;
                    }
                    break;

                case WorkerPhase.Disappearing:
                    if (Time.time >= phaseEndsAt)
                    {
                        SetAnimatorState(
                            isWalking: false,
                            isBuilding: false,
                            isEmerging: false,
                            isDisappearing: false);
                        SetWorkerVisible(false);
                        StopGroundVortex();
                        navigationAgent.enabled = false;
                        HasExited = true;
                        phase = WorkerPhase.Exited;
                    }
                    break;
            }
        }

        private void OnDisable()
        {
            ReleaseSpawnReservation();
        }

        private void TryStartApproach()
        {
            if (!TrySelectValidRoute())
            {
                phase = WorkerPhase.WaitingForSpawn;
                nextSpawnRetryAt = Time.time + spawnRetryDelay;
                return;
            }

            transform.SetPositionAndRotation(
                sampledSpawnPoint,
                LookTowards(sampledSpawnPoint, sampledBuildPoint));
            workerAnimator.speed = 1f;
            SetAnimatorState(
                isWalking: false,
                isBuilding: false,
                isEmerging: true,
                isDisappearing: false);

            // The effect is started first. Only then is the goblin made visible, while
            // navigation and interaction remain disabled until emergence is complete.
            SetWorkerVisible(false);
            PlayGroundVortex();
            SetWorkerCollidersEnabled(false);
            phase = WorkerPhase.VortexLeadIn;
            phaseEndsAt = Time.time + vortexLeadInDuration;
        }

        private bool TrySelectValidRoute()
        {
            if (spawnPoints.Length == 0 || workPoint == null || exitPoint == null)
            {
                return false;
            }

            if (!TrySamplePoint(workPoint.position, workPointSampleRadius, out sampledBuildPoint)
                || !TrySamplePoint(exitPoint.position, routePointSampleRadius, out sampledExitPoint)
                || !IsSpawnAreaFree(sampledExitPoint))
            {
                return false;
            }

            PrepareShuffledSpawnPointIndices();
            foreach (var index in shuffledSpawnPointIndices)
            {
                var candidate = spawnPoints[index];
                if (candidate == null || ReservedSpawnPoints.Contains(candidate.GetEntityId()))
                {
                    continue;
                }

                if (!TrySamplePoint(candidate.position, routePointSampleRadius, out var sampledCandidate)
                    || !IsFarEnoughFromSite(sampledCandidate)
                    || !IsSpawnAreaFree(sampledCandidate)
                    || !HasCompletePath(sampledCandidate, sampledBuildPoint)
                    || !HasCompletePath(sampledBuildPoint, sampledExitPoint))
                {
                    continue;
                }

                sampledSpawnPoint = sampledCandidate;
                reservedSpawnPointId = candidate.GetEntityId();
                ReservedSpawnPoints.Add(reservedSpawnPointId);
                hasSpawnReservation = true;
                return true;
            }

            return false;
        }

        private bool IsSpawnAreaFree(Vector3 position)
        {
            var capsuleBottom = position + Vector3.up * spawnCheckBottomHeight;
            var capsuleTop = position + Vector3.up * Mathf.Max(
                spawnCheckTopHeight,
                spawnCheckBottomHeight + 0.1f);
            var hits = Physics.OverlapCapsule(
                capsuleBottom,
                capsuleTop,
                spawnCheckRadius,
                spawnBlockingMask,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit != null && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void PrepareShuffledSpawnPointIndices()
        {
            shuffledSpawnPointIndices.Clear();
            if (spawnPoints.Length == 0)
            {
                return;
            }

            var normalizedPreferredIndex = Mathf.Clamp(
                preferredSpawnPointIndex,
                0,
                spawnPoints.Length - 1);
            shuffledSpawnPointIndices.Add(normalizedPreferredIndex);
            for (var index = 0; index < spawnPoints.Length; index++)
            {
                if (index != normalizedPreferredIndex)
                {
                    shuffledSpawnPointIndices.Add(index);
                }
            }

            // Fisher-Yates: every retry checks the authored points in a new order.
            for (var index = shuffledSpawnPointIndices.Count - 1; index > 0; index--)
            {
                var swapIndex = Random.Range(0, index + 1);
                var temporary = shuffledSpawnPointIndices[index];
                shuffledSpawnPointIndices[index] = shuffledSpawnPointIndices[swapIndex];
                shuffledSpawnPointIndices[swapIndex] = temporary;
            }
        }

        private bool IsFarEnoughFromSite(Vector3 position)
        {
            if (constructionSite == null) return true;
            var offset = position - constructionSite.position;
            offset.y = 0f;
            return offset.sqrMagnitude >= minimumSiteDistance * minimumSiteDistance;
        }

        private static bool HasCompletePath(Vector3 origin, Vector3 target)
        {
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(origin, target, NavMesh.AllAreas, path)
                   && path.status == NavMeshPathStatus.PathComplete;
        }

        private void BeginBuilding()
        {
            navigationAgent.isStopped = true;
            navigationAgent.ResetPath();
            navigationAgent.updatePosition = false;
            navigationAgent.updateRotation = false;
            navigationAgent.nextPosition = sampledBuildPoint;
            transform.SetPositionAndRotation(workPoint.position, workPoint.rotation);
            HasReachedBuildPoint = true;
            IsBuildingNow = true;
            SetHammerVisible(true);
            SetAnimatorState(
                isWalking: false,
                isBuilding: true,
                isEmerging: false,
                isDisappearing: false);
            phase = WorkerPhase.Building;
        }

        private void SetDestination(Vector3 destination)
        {
            if (!navigationAgent.enabled)
            {
                return;
            }

            navigationAgent.isStopped = false;
            if (!navigationAgent.SetDestination(destination))
            {
                Debug.LogWarning("Construction worker could not set its NavMesh destination.", this);
            }
        }

        private bool HasArrived()
        {
            if (!navigationAgent.enabled
                || navigationAgent.pathPending
                || navigationAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }

            return navigationAgent.remainingDistance <= navigationAgent.stoppingDistance + 0.03f
                   && (!navigationAgent.hasPath || navigationAgent.velocity.sqrMagnitude < 0.01f);
        }

        private static bool TrySamplePoint(
            Vector3 requestedPoint,
            float sampleRadius,
            out Vector3 sampledPoint)
        {
            if (NavMesh.SamplePosition(
                    requestedPoint,
                    out var hit,
                    sampleRadius,
                    NavMesh.AllAreas))
            {
                sampledPoint = hit.position;
                return true;
            }

            sampledPoint = requestedPoint;
            return false;
        }

        private bool EnableNavigationAt(Vector3 worldPoint)
        {
            navigationAgent.enabled = true;
            navigationAgent.updatePosition = true;
            navigationAgent.updateRotation = true;
            if (navigationAgent.Warp(worldPoint))
            {
                navigationAgent.isStopped = true;
                navigationAgent.ResetPath();
                return true;
            }

            navigationAgent.enabled = false;
            return false;
        }

        private void CachePresentationParts()
        {
            workerColliders = GetComponentsInChildren<Collider>(true);
            var allRenderers = GetComponentsInChildren<Renderer>(true);
            var visibleRenderers = new List<Renderer>(allRenderers.Length);
            foreach (var renderer in allRenderers)
            {
                if (groundVortex == null || !renderer.transform.IsChildOf(groundVortex.transform))
                {
                    visibleRenderers.Add(renderer);
                }
            }

            workerRenderers = visibleRenderers.ToArray();
            vortexSystems = groundVortex != null
                ? groundVortex.GetComponentsInChildren<ParticleSystem>(true)
                : System.Array.Empty<ParticleSystem>();
        }

        private void PlayGroundVortex()
        {
            if (groundVortex == null) return;
            groundVortex.transform.position = transform.position + Vector3.up * 0.025f;
            groundVortex.SetActive(true);
            foreach (var system in vortexSystems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(true);
            }
        }

        private void StopGroundVortex()
        {
            if (groundVortex == null) return;
            foreach (var system in vortexSystems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            groundVortex.SetActive(false);
        }

        private void SetHammerVisible(bool visible)
        {
            if (hammerVisual != null)
            {
                hammerVisual.SetActive(visible);
            }
        }

        private void SetWorkerVisible(bool visible)
        {
            foreach (var workerRenderer in workerRenderers)
            {
                if (workerRenderer != null)
                {
                    workerRenderer.enabled = visible;
                }
            }
        }

        private void SetWorkerCollidersEnabled(bool enabledState)
        {
            foreach (var workerCollider in workerColliders)
            {
                if (workerCollider != null)
                {
                    workerCollider.enabled = enabledState;
                }
            }
        }

        private void ReleaseSpawnReservation()
        {
            if (!hasSpawnReservation) return;
            ReservedSpawnPoints.Remove(reservedSpawnPointId);
            reservedSpawnPointId = default;
            hasSpawnReservation = false;
        }

        private static Quaternion LookTowards(Vector3 origin, Vector3 target)
        {
            var direction = target - origin;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : Quaternion.identity;
        }

        private void SetAnimatorState(
            bool isWalking,
            bool isBuilding,
            bool isEmerging,
            bool isDisappearing)
        {
            if (workerAnimator == null) return;
            workerAnimator.SetBool(IsWalking, isWalking);
            workerAnimator.SetBool(IsBuilding, isBuilding);
            workerAnimator.SetBool(IsEmerging, isEmerging);
            workerAnimator.SetBool(IsDisappearing, isDisappearing);
        }

        private enum WorkerPhase
        {
            Hidden,
            WaitingForSpawn,
            VortexLeadIn,
            Emerging,
            WalkingToBuild,
            Building,
            WalkingToExit,
            Disappearing,
            Exited
        }
    }
}
