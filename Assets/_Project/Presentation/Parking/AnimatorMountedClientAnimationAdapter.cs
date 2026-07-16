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
        [SerializeField] private Animator[] animators = System.Array.Empty<Animator>();
        [SerializeField] private string walkingParameter = "Walking";

        private int walkingParameterId;

        public void Configure(Animator[] configuredAnimators, string parameterName = "Walking")
        {
            animators = configuredAnimators;
            walkingParameter = parameterName;
            walkingParameterId = Animator.StringToHash(walkingParameter);
        }

        private void Awake()
        {
            walkingParameterId = Animator.StringToHash(walkingParameter);
            if (animators == null || animators.Length == 0)
            {
                Debug.LogError("Mounted client animation adapter has no Animators.", this);
                enabled = false;
            }
        }

        public void SetWalking(bool isWalking)
        {
            if (animators == null)
            {
                return;
            }

            foreach (var animator in animators)
            {
                if (animator != null)
                {
                    animator.SetBool(walkingParameterId, isWalking);
                }
            }
        }
    }
}
