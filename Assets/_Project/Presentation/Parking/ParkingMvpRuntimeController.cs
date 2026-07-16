using HorseParking.Core.Parking;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>
    /// Scene adapter for the one-client MVP. It contains no tariff or slot rules: those
    /// stay in Core/Application and arrive here through the composition root.
    /// </summary>
    public sealed class ParkingMvpRuntimeController : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private GameObject clientVisual = null!;
        [SerializeField] private GameObject paymentSackVisual = null!;
        [SerializeField] private Transform paymentBagAnchor = null!;
        [SerializeField] private MountedClientRoutePresenter routePresenter = null!;
        [SerializeField] private float paymentReadyAfterSeconds = 5f;
        [SerializeField] private float paymentApproachFailsafeSeconds = 8f;

        private bool paymentRequested;
        private bool approachingPayment;
        private bool paymentCollected;
        private bool clientParked;
        private bool exitStarted;
        private bool initialized;
        private int collectedGold;
        private double parkedAtSeconds;
        private double paymentApproachStartedAtSeconds;

        public bool CanCollectPayment => initialized && paymentRequested && !paymentCollected;

        public bool CanOpenExit => initialized && paymentCollected && collectedGold > 0 && !exitStarted;

        public void Configure(GameCompositionRoot root, GameObject client, GameObject sack, MountedClientRoutePresenter route, Transform bagAnchor)
        {
            compositionRoot = root;
            clientVisual = client;
            paymentSackVisual = sack;
            routePresenter = route;
            paymentBagAnchor = bagAnchor;
        }

        private void Start()
        {
            if (compositionRoot == null || clientVisual == null || paymentSackVisual == null || paymentBagAnchor == null)
            {
                Debug.LogError("Parking MVP runtime is missing scene references.", this);
                enabled = false;
                return;
            }

            paymentSackVisual.SetActive(false);
            clientVisual.SetActive(true);
            initialized = true;
            if (routePresenter == null)
            {
                Debug.LogError("Parking MVP route presenter is missing.", this);
                enabled = false;
                return;
            }

            routePresenter.BindCallbacks(NotifyClientParked, NotifyClientAtPaymentGate, NotifyClientExited);
            routePresenter.BeginArrival();
            Debug.Log("Parking: client is arriving.");
        }

        private void Update()
        {
            if (!initialized || !clientParked || paymentRequested)
            {
                return;
            }

            var elapsed = compositionRoot.GameClock.ElapsedSeconds;
            if (!approachingPayment && elapsed - parkedAtSeconds >= paymentReadyAfterSeconds)
            {
                approachingPayment = true;
                paymentApproachStartedAtSeconds = elapsed;
                routePresenter.BeginPaymentApproach();
                Debug.Log("Parking: client returned to the exit gate with payment.");
                return;
            }

            // Visual routing must never block the playable parking loop. If a future
            // asset adapter fails to signal arrival, payment still becomes available.
            if (approachingPayment && elapsed - paymentApproachStartedAtSeconds >= paymentApproachFailsafeSeconds)
            {
                Debug.LogWarning("Parking: payment-route callback timed out; exposing payment bag as a gameplay fallback.", this);
                NotifyClientAtPaymentGate();
            }
        }

        public bool TryCollectPayment()
        {
            if (!CanCollectPayment || !compositionRoot.ParkingLifecycleUseCase.TryCollectPayment(out var payment))
            {
                return false;
            }

            collectedGold = payment.Gold;
            paymentCollected = true;
            paymentSackVisual.SetActive(false);
            Debug.Log("Parking: collected " + payment.Gold + " gold. Go to the exit gate and left-click it.");
            return true;
        }

        public bool TryOpenExit()
        {
            if (!CanOpenExit)
            {
                return false;
            }

            exitStarted = true;
            routePresenter.BeginExit();
            Debug.Log("Parking: gate opened; client is leaving.");
            return true;
        }

        public void NotifyClientParked()
        {
            if (!compositionRoot.ParkingLifecycleUseCase.TryPark("client-mounted-01"))
            {
                Debug.LogError("Parking MVP could not park the arriving client.", this);
                return;
            }
            clientParked = true;
            parkedAtSeconds = compositionRoot.GameClock.ElapsedSeconds;
            Debug.Log("Parking: client occupies the slot. Payment will be requested in " + paymentReadyAfterSeconds + " seconds.");
        }

        public void NotifyClientAtPaymentGate()
        {
            if (paymentRequested)
            {
                return;
            }

            paymentRequested = compositionRoot.ParkingLifecycleUseCase.TryRequestPayment();
            if (!paymentRequested)
            {
                Debug.LogError("Parking MVP could not request payment.", this);
                return;
            }

            // Preserve the sack's world scale when parenting under the scaled FBX rig.
            paymentSackVisual.transform.SetParent(paymentBagAnchor, true);
            paymentSackVisual.transform.position = paymentBagAnchor.position;
            paymentSackVisual.transform.rotation = paymentBagAnchor.rotation;
            paymentSackVisual.SetActive(true);
            Debug.Log("Parking: payment bag is at the horse. Look at it and left-click.");
        }

        public void NotifyClientExited()
        {
            if (!compositionRoot.ParkingLifecycleUseCase.TryReleaseClient())
            {
                Debug.LogError("Parking MVP could not release the paid client.", this);
                return;
            }
            clientVisual.SetActive(false);
            Debug.Log("Parking: client left. Slot is free.");
        }
    }
}
