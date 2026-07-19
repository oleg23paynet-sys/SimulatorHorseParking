using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class CartDriverHandleIk : MonoBehaviour
    {
        [SerializeField] private Transform leftHandle = null!;
        [SerializeField] private Transform rightHandle = null!;
        [SerializeField] private Transform leftElbowHint = null!;
        [SerializeField] private Transform rightElbowHint = null!;
        [Range(0f, 1f)] [SerializeField] private float positionWeight = 1f;
        [Range(0f, 1f)] [SerializeField] private float rotationWeight = 0.65f;

        private Animator animator = null!;

        public void Configure(
            Transform leftGrip,
            Transform rightGrip,
            Transform leftElbow,
            Transform rightElbow)
        {
            leftHandle = leftGrip;
            rightHandle = rightGrip;
            leftElbowHint = leftElbow;
            rightElbowHint = rightElbow;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || !animator.isHuman || leftHandle == null || rightHandle == null)
            {
                return;
            }

            ApplyHand(AvatarIKGoal.LeftHand, leftHandle);
            ApplyHand(AvatarIKGoal.RightHand, rightHandle);
            ApplyElbow(AvatarIKHint.LeftElbow, leftElbowHint);
            ApplyElbow(AvatarIKHint.RightElbow, rightElbowHint);
        }

        private void ApplyHand(AvatarIKGoal goal, Transform target)
        {
            animator.SetIKPositionWeight(goal, positionWeight);
            animator.SetIKRotationWeight(goal, rotationWeight);
            animator.SetIKPosition(goal, target.position);
            animator.SetIKRotation(goal, target.rotation);
        }

        private void ApplyElbow(AvatarIKHint hint, Transform target)
        {
            if (target == null)
            {
                animator.SetIKHintPositionWeight(hint, 0f);
                return;
            }

            animator.SetIKHintPositionWeight(hint, 0.85f);
            animator.SetIKHintPosition(hint, target.position);
        }
    }
}
