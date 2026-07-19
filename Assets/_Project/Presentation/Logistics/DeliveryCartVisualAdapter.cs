using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    public sealed class DeliveryCartVisualAdapter : MonoBehaviour
    {
        [SerializeField] private Animator cartAnimator = null!;
        [SerializeField] private Animator driverAnimator = null!;

        public void Configure(Animator cart, Animator driver)
        {
            cartAnimator = cart;
            driverAnimator = driver;
        }

        public void SetTraveling(bool isTraveling)
        {
            if (cartAnimator != null)
            {
                cartAnimator.speed = isTraveling ? 1f : 0f;
            }

            if (driverAnimator != null)
            {
                driverAnimator.SetBool("IsMoving", isTraveling);
            }
        }
    }
}
