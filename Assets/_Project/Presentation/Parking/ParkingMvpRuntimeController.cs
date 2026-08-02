using System;
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
        [SerializeField] private RiderParkingSequencePresenter riderSequence = null!;
        [SerializeField] private float paymentReadyAfterSeconds = 5f;
        [SerializeField] private float paymentApproachFailsafeSeconds = 8f;

        private bool paymentRequested;
        private bool approachingPayment;
        private bool paymentCollected;
        private bool clientParked;
        private bool riderReadyForDeparture;
        private bool exitStarted;
        private bool initialized;
        private int collectedGold;
        private double parkedAtSeconds;
        private double paymentApproachStartedAtSeconds;
        private double nextClientArrivalAtSeconds;
        private bool waitingForNextClient;
        private ParkingClientArchetype? currentArchetype;

        public bool CanCollectPayment => initialized && paymentRequested && !paymentCollected;

        public bool CanOpenExit => initialized && paymentCollected && collectedGold > 0 && !exitStarted;
        public bool CanTalkToClient =>
            initialized
            && clientParked
            && !waitingForNextClient
            && !exitStarted
            && currentArchetype != null
            && clientVisual.activeInHierarchy;
        public ParkingClientArchetype? CurrentArchetype => currentArchetype;
        public event Action<ParkingClientArchetype>? ClientArchetypeChanged;
        public event Action<ParkingClientArchetype, ParkingClientDialogueMoment>? ClientDialogueRequested;

        public void Configure(GameCompositionRoot root, GameObject client, GameObject sack, MountedClientRoutePresenter route, Transform bagAnchor, RiderParkingSequencePresenter sequence)
        {
            compositionRoot = root;
            clientVisual = client;
            paymentSackVisual = sack;
            routePresenter = route;
            paymentBagAnchor = bagAnchor;
            riderSequence = sequence;
        }

        private void Start()
        {
            if (compositionRoot == null || clientVisual == null || paymentSackVisual == null || paymentBagAnchor == null || riderSequence == null)
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
            riderSequence.BindReadyForDeparture(NotifyRiderReadyForDeparture);
            SelectNextArchetype();
            routePresenter.BeginArrival();
            RequestDialogue(ParkingClientDialogueMoment.Arriving);
            Debug.Log("Parking: client is arriving.");
        }

        private void Update()
        {
            if (waitingForNextClient)
            {
                if (compositionRoot.GameClock.ElapsedSeconds >= nextClientArrivalAtSeconds)
                {
                    BeginNextClient();
                }
                return;
            }

            if (!initialized || !clientParked || !riderReadyForDeparture || paymentRequested)
            {
                return;
            }

            var elapsed = compositionRoot.GameClock.ElapsedSeconds;
            var visitSeconds = currentArchetype?.ParkingDurationSeconds ?? paymentReadyAfterSeconds;
            if (!approachingPayment && elapsed - parkedAtSeconds >= visitSeconds)
            {
                approachingPayment = true;
                paymentApproachStartedAtSeconds = elapsed;
                routePresenter.BeginPaymentApproach();
                RequestDialogue(ParkingClientDialogueMoment.Returning);
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
            if (!CanCollectPayment)
            {
                return false;
            }

            ParkingPayment payment;
            var collected = currentArchetype != null
                ? compositionRoot.ParkingLifecycleUseCase.TryCollectPayment(currentArchetype.Tariff, out payment)
                : compositionRoot.ParkingLifecycleUseCase.TryCollectPayment(out payment);
            if (!collected)
            {
                return false;
            }

            collectedGold = payment.Gold;
            paymentCollected = true;
            if (compositionRoot.HasLogisticsInventory)
            {
                compositionRoot.LogisticsInventoryUseCase.AddGold(payment.Gold);
            }
            else
            {
                Debug.LogError("Parking payment was collected, but the shared gold balance is not configured.", this);
            }

            paymentSackVisual.SetActive(false);
            RequestDialogue(ParkingClientDialogueMoment.PaymentReceived);
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
            RequestDialogue(ParkingClientDialogueMoment.Leaving);
            routePresenter.BeginExit();
            Debug.Log("Parking: gate opened; client is leaving.");
            return true;
        }

        public bool TryTalkToClient()
        {
            if (!CanTalkToClient)
                return false;

            RequestDialogue(ParkingClientDialogueMoment.PlayerGreeting);
            return true;
        }

        public void NotifyClientParked()
        {
            var clientId = currentArchetype != null
                ? "client-" + currentArchetype.Id
                : "client-mounted-01";
            if (!compositionRoot.ParkingLifecycleUseCase.TryPark(clientId))
            {
                Debug.LogError("Parking MVP could not park the arriving client.", this);
                return;
            }
            clientParked = true;
            riderReadyForDeparture = false;
            parkedAtSeconds = compositionRoot.GameClock.ElapsedSeconds;
            riderSequence.BeginParkingVisit();
            RequestDialogue(ParkingClientDialogueMoment.Parked);
            Debug.Log("Parking: horse is parked; rider is dismounting and leaving on foot.");
        }

        private void NotifyRiderReadyForDeparture()
        {
            riderReadyForDeparture = true;
            Debug.Log("Parking: rider returned and mounted the horse.");
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

            paymentSackVisual.transform.SetParent(paymentBagAnchor, false);
            paymentSackVisual.transform.localPosition = Vector3.zero;
            paymentSackVisual.transform.localRotation = Quaternion.identity;
            paymentSackVisual.transform.localScale = Vector3.one;
            paymentSackVisual.SetActive(true);
            RequestDialogue(ParkingClientDialogueMoment.WaitingForPayment);
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
            waitingForNextClient = true;
            nextClientArrivalAtSeconds = compositionRoot.GameClock.ElapsedSeconds
                                         + compositionRoot.ClientRespawnDelaySeconds;
            Debug.Log("Parking: client left. Slot is free.");
        }

        private void SelectNextArchetype()
        {
            if (!compositionRoot.HasParkingClientArchetypes)
            {
                return;
            }

            currentArchetype = compositionRoot.ParkingClientArchetypeSelectionUseCase.SelectNext();
            ClientArchetypeChanged?.Invoke(currentArchetype);
        }

        private void BeginNextClient()
        {
            waitingForNextClient = false;
            paymentRequested = false;
            approachingPayment = false;
            paymentCollected = false;
            clientParked = false;
            riderReadyForDeparture = false;
            exitStarted = false;
            collectedGold = 0;
            paymentSackVisual.SetActive(false);
            routePresenter.ResetToSpawn();
            SelectNextArchetype();
            clientVisual.SetActive(true);
            routePresenter.BeginArrival();
            RequestDialogue(ParkingClientDialogueMoment.Arriving);
            Debug.Log("Parking: next client archetype is arriving.");
        }

        private void RequestDialogue(ParkingClientDialogueMoment moment)
        {
            if (currentArchetype == null || !compositionRoot.HasParkingClientDialogue)
                return;

            ClientDialogueRequested?.Invoke(currentArchetype, moment);
        }
    }
}
