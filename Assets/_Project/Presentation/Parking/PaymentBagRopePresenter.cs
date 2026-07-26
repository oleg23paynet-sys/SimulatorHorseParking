#nullable enable

using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>
    /// Visual-only flexible rope between the horse bridle and the top knot of the
    /// payment bag. Gameplay and payment state remain outside this presenter.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PaymentBagRopePresenter : MonoBehaviour
    {
        [SerializeField] private Transform attachmentPoint = null!;
        [SerializeField] private Transform bagTop = null!;
        [SerializeField] private LineRenderer ropeRenderer = null!;
        [Range(3, 16)] [SerializeField] private int segmentCount = 8;
        [Min(0f)] [SerializeField] private float slack = 0.025f;

        public void Configure(Transform attachment, Transform top)
        {
            attachmentPoint = attachment;
            bagTop = top;
            ropeRenderer = GetComponent<LineRenderer>();
            RefreshRope();
        }

        private void Awake()
        {
            if (ropeRenderer == null)
            {
                ropeRenderer = GetComponent<LineRenderer>();
            }
        }

        private void LateUpdate() => RefreshRope();

        private void RefreshRope()
        {
            if (attachmentPoint == null || bagTop == null || ropeRenderer == null)
            {
                return;
            }

            var start = attachmentPoint.position;
            var end = bagTop.position;
            var length = Vector3.Distance(start, end);
            var downwardSag = Mathf.Max(slack, length * 0.08f);
            var count = Mathf.Max(3, segmentCount);
            ropeRenderer.positionCount = count;

            for (var index = 0; index < count; index++)
            {
                var t = index / (float)(count - 1);
                var curve = 4f * t * (1f - t);
                ropeRenderer.SetPosition(
                    index,
                    Vector3.Lerp(start, end, t) + (Vector3.down * downwardSag * curve));
            }
        }
    }
}
