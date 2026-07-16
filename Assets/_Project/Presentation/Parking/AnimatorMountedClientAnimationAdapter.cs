using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>
    /// Asset-agnostic bridge between the parking route and a real Animator Controller.
    /// Replacing the horse package only requires assigning another Animator/controller;
    /// parking rules and route code remain unchanged.
    /// </summary>
    public sealed class AnimatorMountedClientAnimationAdapter : MonoBehaviour, IMountedClientAnimation
    {
        [SerializeField] private Animator horseAnimator = null!;
        [SerializeField] private string walkingParameter = "Walking";

        private int walkingParameterId;

        public void Configure(Animator animator, string parameterName = "Walking")
        {
            horseAnimator = animator;
            walkingParameter = parameterName;
            walkingParameterId = Animator.StringToHash(walkingParameter);
        }

        private void Awake()
        {
            walkingParameterId = Animator.StringToHash(walkingParameter);
            if (horseAnimator == null)
            {
                Debug.LogError("Mounted client animation adapter is missing the horse Animator.", this);
                enabled = false;
            }
        }

        public void SetWalking(bool isWalking)
        {
            if (horseAnimator == null)
            {
                return;
            }

            horseAnimator.SetBool(walkingParameterId, isWalking);
        }
    }
}
