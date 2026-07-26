#nullable enable

using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>
    /// Visual-only damped pendulum for the payment pouch. The pivot stays attached to
    /// the horse's bit while the cord and pouch react to anchor acceleration and idle motion.
    /// </summary>
    public sealed class PaymentBagSwingPresenter : MonoBehaviour
    {
        [Min(0.1f)] [SerializeField] private float springStrength = 16f;
        [Min(0.1f)] [SerializeField] private float damping = 6.5f;
        [Min(0f)] [SerializeField] private float accelerationInfluence = 0.8f;
        [Range(1f, 30f)] [SerializeField] private float maximumAngle = 17f;
        [Range(0f, 5f)] [SerializeField] private float idleSwingAngle = 0.35f;
        [Min(0.1f)] [SerializeField] private float idleFrequency = 0.8f;
        [Min(0.1f)] [SerializeField] private float accelerationSmoothing = 10f;

        private Quaternion restLocalRotation;
        private Vector3 previousAnchorPosition;
        private Vector3 previousAnchorVelocity;
        private Vector2 angles;
        private Vector2 angularVelocity;
        private Vector3 smoothedLocalAcceleration;
        private bool initialized;

        private void Awake()
        {
            restLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            ResetMotion();
        }

        private void LateUpdate()
        {
            if (transform.parent == null) return;
            if (!initialized) ResetMotion();

            var deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            if (deltaTime <= 0f) return;

            var anchorPosition = transform.parent.position;
            var anchorVelocity = (anchorPosition - previousAnchorPosition) / deltaTime;
            var anchorAcceleration = (anchorVelocity - previousAnchorVelocity) / deltaTime;
            var localAcceleration = transform.parent.InverseTransformDirection(anchorAcceleration);
            smoothedLocalAcceleration = Vector3.Lerp(
                smoothedLocalAcceleration,
                localAcceleration,
                1f - Mathf.Exp(-accelerationSmoothing * deltaTime));

            var idlePhase = Time.time * idleFrequency * Mathf.PI * 2f;
            var targetAngles = new Vector2(
                Mathf.Clamp(-smoothedLocalAcceleration.z * accelerationInfluence, -maximumAngle, maximumAngle)
                    + Mathf.Sin(idlePhase) * idleSwingAngle,
                Mathf.Clamp(smoothedLocalAcceleration.x * accelerationInfluence, -maximumAngle, maximumAngle)
                    + Mathf.Sin(idlePhase * 0.73f + 1.1f) * idleSwingAngle * 0.55f);

            angularVelocity += (targetAngles - angles) * (springStrength * deltaTime);
            angularVelocity *= Mathf.Exp(-damping * deltaTime);
            angles += angularVelocity * deltaTime;
            angles.x = Mathf.Clamp(angles.x, -maximumAngle, maximumAngle);
            angles.y = Mathf.Clamp(angles.y, -maximumAngle, maximumAngle);

            transform.localRotation = restLocalRotation * Quaternion.Euler(angles.x, 0f, angles.y);
            previousAnchorPosition = anchorPosition;
            previousAnchorVelocity = anchorVelocity;
        }

        private void ResetMotion()
        {
            angles = Vector2.zero;
            angularVelocity = Vector2.zero;
            previousAnchorPosition = transform.parent != null
                ? transform.parent.position
                : transform.position;
            previousAnchorVelocity = Vector3.zero;
            smoothedLocalAcceleration = Vector3.zero;
            transform.localRotation = restLocalRotation;
            initialized = true;
        }
    }
}
