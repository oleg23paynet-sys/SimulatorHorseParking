using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>
    /// Presentation-only adapter for the paid rider clips. It keeps root motion,
    /// grounding and on-foot collision outside Parking Core.
    /// </summary>
    public sealed class RiderParkingSequencePresenter : MonoBehaviour
    {
        [SerializeField] private Transform riderRoot = null!;
        [SerializeField] private Animator riderAnimator = null!;
        [SerializeField] private Transform saddleParent = null!;
        [SerializeField] private Transform mountPoint = null!;
        [SerializeField] private Transform lanePoint = null!;
        [SerializeField] private Transform awayPoint = null!;
        [SerializeField] private Collider groundCollider = null!;
        [SerializeField] private Collider[] routeObstacles = Array.Empty<Collider>();
        [SerializeField] private float turnSpeedDegrees = 420f;
        [SerializeField] private float awayWaitSeconds = 1.25f;
        [SerializeField] private float dismountDurationSeconds = 1.8f;
        [SerializeField] private float mountDurationSeconds = 1.8f;
        [SerializeField] private float onFootArrivalRadius = 0.16f;
        [SerializeField] private float mountStartRadius = 0.18f;
        [SerializeField] private float mountFinishRadius = 0.45f;

        private static readonly int WalkingId = Animator.StringToHash("Walking");
        private static readonly int OnFootWalkingId = Animator.StringToHash("OnFootWalking");
        private static readonly int DismountId = Animator.StringToHash("Dismount");
        private static readonly int MountId = Animator.StringToHash("Mount");
        private static readonly int DismountStateId = Animator.StringToHash("DismountLeft");
        private static readonly int MountStateId = Animator.StringToHash("MountLeft");
        private static readonly int DismountFullPathId = Animator.StringToHash("Base Layer.DismountLeft");
        private static readonly int MountFullPathId = Animator.StringToHash("Base Layer.MountLeft");

        private Action? readyForDeparture;
        private CharacterController onFootController = null!;
        private Vector3 mountedLocalPosition;
        private Quaternion mountedLocalRotation;
        private Vector3 mountedLocalScale;
        private Vector3 onFootMountPosition;
        private Quaternion onFootMountRotation;
        private Vector3 onFootDestination;
        private string currentRouteLeg = string.Empty;
        private string lastBlockingCollider = string.Empty;
        private bool consumeTransitionRootMotion;
        private bool consumeOnFootRootMotion;
        private bool routeBlocked;
        private bool running;

        public void Configure(
            Transform rider,
            Animator animator,
            Transform saddle,
            Transform configuredMountPoint,
            Transform configuredLanePoint,
            Transform configuredAwayPoint,
            Collider configuredGroundCollider,
            Collider[] configuredRouteObstacles,
            float dismountDuration,
            float mountDuration)
        {
            riderRoot = rider;
            riderAnimator = animator;
            saddleParent = saddle;
            mountPoint = configuredMountPoint;
            lanePoint = configuredLanePoint;
            awayPoint = configuredAwayPoint;
            groundCollider = configuredGroundCollider;
            routeObstacles = configuredRouteObstacles ?? Array.Empty<Collider>();
            dismountDurationSeconds = Mathf.Max(0.1f, dismountDuration);
            mountDurationSeconds = Mathf.Max(0.1f, mountDuration);
        }

        private void Awake()
        {
            if (riderRoot == null || riderAnimator == null || saddleParent == null ||
                mountPoint == null || lanePoint == null || awayPoint == null || groundCollider == null)
            {
                Debug.LogError("Rider parking sequence is missing scene references.", this);
                enabled = false;
                return;
            }

            mountedLocalPosition = riderRoot.localPosition;
            mountedLocalRotation = riderRoot.localRotation;
            mountedLocalScale = riderRoot.localScale;
            // Unity components use a native "fake null" value; the C# ?? operator can
            // keep that missing wrapper instead of executing AddComponent.
            onFootController = riderRoot.GetComponent<CharacterController>();
            if (onFootController == null)
            {
                onFootController = riderRoot.gameObject.AddComponent<CharacterController>();
            }
            onFootController.enabled = false;
            if (routeObstacles.Length == 0)
            {
                // Migration fallback for a ParkingMvp scene built before obstacle
                // references were injected explicitly by the scene builder.
                routeObstacles = FindObjectsByType<Collider>()
                    .Where(IsPedestrianRouteObstacle)
                    .ToArray();
            }
            // OnAnimatorMove decides where authored motion is consumed. Keeping this
            // enabled prevents individual states from silently losing Root Motion.
            riderAnimator.applyRootMotion = true;
        }

        public void BindReadyForDeparture(Action callback) => readyForDeparture = callback;

        public void BeginParkingVisit()
        {
            if (enabled && !running)
            {
                StartCoroutine(RunParkingVisit());
            }
        }

        private IEnumerator RunParkingVisit()
        {
            running = true;
            riderAnimator.SetBool(WalkingId, false);
            riderAnimator.SetBool(OnFootWalkingId, false);
            riderAnimator.ResetTrigger(MountId);

            // The paid transition owns the rider root. It must start unparented;
            // keeping it below the saddle bone adds the horse height twice.
            riderRoot.SetParent(null, true);
            consumeTransitionRootMotion = true;
            riderAnimator.applyRootMotion = true;
            // Enter the paid transition explicitly. The previous AnyState trigger was
            // consumed without changing state on the generated runtime controller.
            riderAnimator.CrossFadeInFixedTime(DismountFullPathId, 0.05f, 0, 0f);
            yield return PlayTransition(DismountStateId, dismountDurationSeconds);
            consumeTransitionRootMotion = false;
            GroundFeetVertically();

            // The paid dismount clip defines the real mounting point. Reusing its
            // exact end pose makes the reverse paid clip start beside the horse,
            // rather than from a hard-coded point followed by an airborne snap.
            onFootMountPosition = riderRoot.position;
            onFootMountRotation = riderRoot.rotation;

            ConfigureOnFootController();

            // The paid clip can finish either inside or outside a side fence. First
            // leave the complete fence depth while preserving that exact X, then move
            // sideways. Clearance is derived from real world-space collider bounds,
            // not from the visual model pivot or a guessed distance.
            var outsideFenceZ = GetSafeOutsideFenceZ();
            var exitClearancePosition = GetGroundedRootPosition(new Vector3(
                onFootMountPosition.x,
                lanePoint.position.y,
                outsideFenceZ));
            var safeAwayPosition = GetGroundedRootPosition(new Vector3(
                awayPoint.position.x,
                awayPoint.position.y,
                outsideFenceZ));

            onFootController.enabled = true;
            routeBlocked = false;
            yield return WalkTo(exitClearancePosition, "leave slot parallel to fence");
            if (AbortWhenRouteBlocked()) yield break;
            yield return WalkTo(safeAwayPosition, "walk outside fence");
            if (AbortWhenRouteBlocked()) yield break;
            yield return new WaitForSeconds(awayWaitSeconds);
            yield return WalkTo(exitClearancePosition, "return outside fence");
            if (AbortWhenRouteBlocked()) yield break;
            yield return WalkTo(onFootMountPosition, "return parallel to fence");
            if (AbortWhenRouteBlocked()) yield break;

            // Mounting is allowed only where the paid dismount actually ended.
            if (HorizontalDistance(riderRoot.position, onFootMountPosition) > mountStartRadius)
            {
                Debug.LogError("Rider could not reach the mount point; mounting was cancelled.", this);
                onFootController.enabled = false;
                running = false;
                yield break;
            }

            onFootController.enabled = false;
            riderRoot.position = onFootMountPosition;
            riderRoot.rotation = onFootMountRotation;
            riderAnimator.SetBool(OnFootWalkingId, false);
            riderAnimator.ResetTrigger(DismountId);
            consumeTransitionRootMotion = true;
            riderAnimator.applyRootMotion = true;
            riderAnimator.CrossFadeInFixedTime(MountFullPathId, 0.05f, 0, 0f);
            yield return PlayTransition(MountStateId, mountDurationSeconds);
            consumeTransitionRootMotion = false;

            var expectedMountedPosition = saddleParent.TransformPoint(mountedLocalPosition);
            if (Vector3.Distance(riderRoot.position, expectedMountedPosition) > mountFinishRadius)
            {
                Debug.LogError(
                    "Paid mount Root Motion did not reach the saddle; no airborne teleport was used.",
                    this);
                running = false;
                yield break;
            }

            // Root Motion has reached the saddle. Parenting only restores the normal
            // mounted hierarchy; the remaining correction is below the guarded radius.
            riderRoot.SetParent(saddleParent, false);
            riderRoot.localPosition = mountedLocalPosition;
            riderRoot.localRotation = mountedLocalRotation;
            riderRoot.localScale = mountedLocalScale;
            riderAnimator.SetBool(WalkingId, false);
            running = false;
            readyForDeparture?.Invoke();
        }

        private IEnumerator PlayTransition(int stateId, float fallbackDuration)
        {
            var elapsed = 0f;
            while (elapsed < 0.75f && riderAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != stateId)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fallbackDuration + 0.75f)
            {
                var state = riderAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash != stateId)
                {
                    if (elapsed > 0.1f)
                    {
                        break;
                    }
                }
                else
                {
                    if (state.normalizedTime >= 0.98f)
                    {
                        break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return null;
        }

        private void OnAnimatorMove()
        {
            if (riderAnimator == null || riderRoot == null)
            {
                return;
            }

            if (consumeTransitionRootMotion)
            {
                // Paid mount/dismount clips own both translation and rotation.
                riderRoot.position += riderAnimator.deltaPosition;
                riderRoot.rotation *= riderAnimator.deltaRotation;

                var grounded = GetGroundedRootPosition(riderRoot.position);
                if (riderRoot.position.y < grounded.y)
                {
                    riderRoot.position = new Vector3(riderRoot.position.x, grounded.y, riderRoot.position.z);
                }
                return;
            }

            if (consumeOnFootRootMotion && onFootController != null && onFootController.enabled)
            {
                // The paid S_Walk_F clip supplies the stride length. Reprojecting it
                // onto the current route leg prevents FBX-axis drift from steering the
                // rider into fences while preserving authored Root Motion speed.
                var authoredDelta = riderAnimator.deltaPosition;
                authoredDelta.y = 0f;
                var remaining = onFootDestination - riderRoot.position;
                remaining.y = 0f;
                if (remaining.sqrMagnitude <= 0.00001f)
                {
                    return;
                }

                var strideDistance = Mathf.Min(authoredDelta.magnitude, remaining.magnitude);
                var routedDelta = remaining.normalized * strideDistance;
                onFootController.Move(routedDelta + (Vector3.down * 2f * Time.deltaTime));
            }
        }

        private IEnumerator WalkTo(Vector3 destination, string routeLeg)
        {
            onFootDestination = GetGroundedRootPosition(destination);
            currentRouteLeg = routeLeg;
            lastBlockingCollider = string.Empty;
            consumeOnFootRootMotion = false;
            riderAnimator.SetBool(OnFootWalkingId, false);

            // Face the next straight route segment before consuming forward Root
            // Motion. Otherwise the first strides cut the corner into a fence post.
            var turnElapsed = 0f;
            while (turnElapsed < 0.65f)
            {
                var turnOffset = onFootDestination - riderRoot.position;
                turnOffset.y = 0f;
                if (turnOffset.sqrMagnitude < 0.0001f)
                {
                    break;
                }

                var targetRotation = Quaternion.LookRotation(turnOffset.normalized, Vector3.up);
                riderRoot.rotation = Quaternion.RotateTowards(
                    riderRoot.rotation,
                    targetRotation,
                    turnSpeedDegrees * Time.deltaTime);
                if (Quaternion.Angle(riderRoot.rotation, targetRotation) < 2f)
                {
                    break;
                }

                turnElapsed += Time.deltaTime;
                yield return null;
            }

            consumeOnFootRootMotion = true;
            riderAnimator.SetBool(OnFootWalkingId, true);
            var elapsed = 0f;
            var previousPosition = riderRoot.position;
            var stuckSeconds = 0f;

            while (HorizontalDistance(riderRoot.position, onFootDestination) > onFootArrivalRadius && elapsed < 12f)
            {
                var offset = onFootDestination - riderRoot.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > 0.0001f)
                {
                    var targetRotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
                    riderRoot.rotation = Quaternion.RotateTowards(
                        riderRoot.rotation,
                        targetRotation,
                        turnSpeedDegrees * Time.deltaTime);
                }

                // Root Motion is evaluated after Update/this coroutine and consumed in
                // OnAnimatorMove. Check its result on the following frame.
                yield return null;

                var moved = HorizontalDistance(previousPosition, riderRoot.position);
                stuckSeconds = moved < 0.001f ? stuckSeconds + Time.deltaTime : 0f;
                previousPosition = riderRoot.position;
                if (stuckSeconds > 1f)
                {
                    var colliderDetails = string.IsNullOrEmpty(lastBlockingCollider)
                        ? "unknown collider"
                        : lastBlockingCollider;
                    Debug.LogError(
                        $"Rider route '{currentRouteLeg}' was blocked by {colliderDetails}; " +
                        $"position={riderRoot.position}, target={onFootDestination}.",
                        this);
                    routeBlocked = true;
                    break;
                }

                elapsed += Time.deltaTime;
            }

            consumeOnFootRootMotion = false;
            if (HorizontalDistance(riderRoot.position, onFootDestination) > onFootArrivalRadius)
            {
                routeBlocked = true;
            }

            riderAnimator.SetBool(OnFootWalkingId, false);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!consumeOnFootRootMotion || hit.collider == null || hit.normal.y > 0.5f)
            {
                return;
            }

            lastBlockingCollider = hit.collider.name;
        }

        private void GroundFeetVertically()
        {
            var grounded = GetGroundedRootPosition(riderRoot.position);
            riderRoot.position = new Vector3(riderRoot.position.x, grounded.y, riderRoot.position.z);
        }

        private bool AbortWhenRouteBlocked()
        {
            if (!routeBlocked)
            {
                return false;
            }

            riderAnimator.SetBool(OnFootWalkingId, false);
            onFootController.enabled = false;
            consumeOnFootRootMotion = false;
            running = false;
            return true;
        }

        private void ConfigureOnFootController()
        {
            var bounds = GetEnabledRendererBounds();
            var scale = riderRoot.lossyScale;
            var verticalScale = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
            var horizontalScale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
            onFootController.height = Mathf.Max(0.5f, bounds.size.y / verticalScale);
            onFootController.radius = Mathf.Min(onFootController.height * 0.22f, 0.28f / horizontalScale);
            onFootController.center = riderRoot.InverseTransformPoint(bounds.center);
            onFootController.skinWidth = Mathf.Max(0.01f, 0.04f / horizontalScale);
            onFootController.stepOffset = Mathf.Min(onFootController.height * 0.2f, 0.3f / verticalScale);
            onFootController.slopeLimit = 45f;
            onFootController.minMoveDistance = 0f;
            onFootController.detectCollisions = true;
        }

        private float GetSafeOutsideFenceZ()
        {
            var fallback = Mathf.Min(lanePoint.position.z, awayPoint.position.z) - 1.25f;
            var validObstacles = routeObstacles
                .Where(collider => collider != null && collider.enabled && !collider.isTrigger)
                .ToArray();
            if (validObstacles.Length == 0)
            {
                return fallback;
            }

            var fenceFrontZ = validObstacles.Min(collider => collider.bounds.min.z);
            var scale = riderRoot.lossyScale;
            var worldRadius = onFootController.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            return Mathf.Min(fallback, fenceFrontZ - worldRadius - 0.6f);
        }

        private static bool IsPedestrianRouteObstacle(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            var current = collider.transform;
            while (current != null)
            {
                if ((current.name.StartsWith("ParkingSlot_01_", StringComparison.Ordinal) &&
                     current.name.EndsWith("Fence", StringComparison.Ordinal)) ||
                    current.name.Equals("ExitGate_01", StringComparison.Ordinal))
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private Vector3 GetGroundedRootPosition(Vector3 requestedPosition)
        {
            var ray = new Ray(new Vector3(requestedPosition.x, groundCollider.bounds.max.y + 5f, requestedPosition.z), Vector3.down);
            if (!groundCollider.Raycast(ray, out var hit, groundCollider.bounds.size.y + 10f))
            {
                return requestedPosition;
            }

            var bounds = GetEnabledRendererBounds();
            var footOffsetFromRoot = bounds.min.y - riderRoot.position.y;
            return new Vector3(requestedPosition.x, hit.point.y - footOffsetFromRoot, requestedPosition.z);
        }

        private Bounds GetEnabledRendererBounds()
        {
            var renderers = riderRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return new Bounds(riderRoot.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}
