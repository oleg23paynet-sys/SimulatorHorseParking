#nullable enable

using UnityEngine;

namespace HorseParking.Presentation.Construction
{
    /// <summary>
    /// Replaceable Unity motion adapter for a construction worker.
    /// Ready-made FBX clips stay in the Animator; this component only moves the worker
    /// between authored scene points and selects Walk/Build states.
    /// </summary>
    public sealed class ConstructionWorkerMotionPresenter : MonoBehaviour
    {
        private static readonly int IsWalking = Animator.StringToHash("isWalking");
        private static readonly int IsBuilding = Animator.StringToHash("isBuilding");

        [SerializeField] private Animator workerAnimator = null!;
        [SerializeField] private Vector3 buildPointWorld;
        [SerializeField] private Quaternion buildRotationWorld = Quaternion.identity;
        [Min(0.1f)] [SerializeField] private float moveSpeed = 1.35f;
        [Min(1f)] [SerializeField] private float turnSpeedDegrees = 540f;
        [Min(0.01f)] [SerializeField] private float stoppingDistance = 0.08f;

        private bool isApproaching;

        public bool HasReachedBuildPoint { get; private set; }
        public bool IsBuildingNow { get; private set; }

        public void Configure(Animator animator, Vector3 buildPoint, Quaternion buildRotation)
        {
            workerAnimator = animator;
            buildPointWorld = buildPoint;
            buildRotationWorld = buildRotation;
        }

        public void BeginApproach()
        {
            HasReachedBuildPoint = false;
            IsBuildingNow = false;
            isApproaching = true;
            SetAnimatorState(isWalking: true, isBuilding: false);
        }

        public void BeginBuilding()
        {
            isApproaching = false;
            HasReachedBuildPoint = true;
            IsBuildingNow = true;
            transform.rotation = buildRotationWorld;
            SetAnimatorState(isWalking: false, isBuilding: true);
        }

        private void Update()
        {
            if (!isApproaching) return;

            var position = transform.position;
            var destination = new Vector3(buildPointWorld.x, position.y, buildPointWorld.z);
            var offset = destination - position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                transform.position = destination;
                BeginBuilding();
                return;
            }

            var direction = offset.normalized;
            var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                turnSpeedDegrees * Time.deltaTime);
            transform.position = Vector3.MoveTowards(
                position,
                destination,
                moveSpeed * Time.deltaTime);
        }

        private void SetAnimatorState(bool isWalking, bool isBuilding)
        {
            if (workerAnimator == null) return;
            workerAnimator.SetBool(IsWalking, isWalking);
            workerAnimator.SetBool(IsBuilding, isBuilding);
        }
    }
}
