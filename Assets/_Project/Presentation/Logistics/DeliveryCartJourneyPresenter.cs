#nullable enable

using HorseParking.Application.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    public static class CartJourneyLocalization
    {
        public static LocalizationKey GetStateKey(CartJourneyState state)
        {
            return new LocalizationKey(state switch
            {
                CartJourneyState.AtWarehouse => "cart.state.at_warehouse",
                CartJourneyState.TravelingToDestination => "cart.state.traveling_to_store",
                CartJourneyState.AtDestination => "cart.state.at_store",
                CartJourneyState.ReturningToWarehouse => "cart.state.returning_to_warehouse",
                _ => "cart.state.unknown"
            });
        }
    }

    public sealed class DeliveryCartJourneyPresenter : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private Transform vehicleRoot = null!;
        [SerializeField] private Transform[] outboundRoute = System.Array.Empty<Transform>();
        [SerializeField] private DeliveryCartVisualAdapter visualAdapter = null!;
        [Min(0.1f)] [SerializeField] private float travelSpeedMetersPerSecond = 2.2f;
        [Min(1f)] [SerializeField] private float turnSpeedDegreesPerSecond = 140f;

        private CartJourneyUseCase journeyUseCase = null!;
        private CartJourneyState observedState = (CartJourneyState)(-1);
        private int routeIndex;

        public void Configure(
            GameCompositionRoot root,
            Transform vehicle,
            Transform[] route,
            DeliveryCartVisualAdapter visuals,
            float travelSpeed)
        {
            compositionRoot = root;
            vehicleRoot = vehicle;
            outboundRoute = route;
            visualAdapter = visuals;
            travelSpeedMetersPerSecond = travelSpeed;
        }

        private void Start()
        {
            if (compositionRoot == null || !compositionRoot.HasCartJourney || vehicleRoot == null
                || visualAdapter == null || outboundRoute.Length < 2)
            {
                Debug.LogError("Delivery cart journey presenter is not configured.", this);
                enabled = false;
                return;
            }

            journeyUseCase = compositionRoot.CartJourneyUseCase;
            var initialState = journeyUseCase.GetSnapshot().State;
            BeginState(initialState);
            observedState = initialState;
        }

        private void Update()
        {
            var snapshot = journeyUseCase.GetSnapshot();
            if (snapshot.State != observedState)
            {
                BeginState(snapshot.State);
                observedState = snapshot.State;
            }

            if (snapshot.State == CartJourneyState.TravelingToDestination) AdvanceOutbound();
            else if (snapshot.State == CartJourneyState.ReturningToWarehouse) AdvanceReturn();
        }

        private void BeginState(CartJourneyState state)
        {
            switch (state)
            {
                case CartJourneyState.AtWarehouse:
                    vehicleRoot.SetPositionAndRotation(outboundRoute[0].position, outboundRoute[0].rotation);
                    visualAdapter.SetTraveling(false);
                    break;
                case CartJourneyState.TravelingToDestination:
                    routeIndex = 1;
                    visualAdapter.SetTraveling(true);
                    break;
                case CartJourneyState.AtDestination:
                    vehicleRoot.SetPositionAndRotation(
                        outboundRoute[outboundRoute.Length - 1].position,
                        outboundRoute[outboundRoute.Length - 1].rotation);
                    visualAdapter.SetTraveling(false);
                    break;
                case CartJourneyState.ReturningToWarehouse:
                    routeIndex = outboundRoute.Length - 2;
                    visualAdapter.SetTraveling(true);
                    break;
            }
        }

        private void AdvanceOutbound()
        {
            if (!MoveTowards(outboundRoute[routeIndex])) return;
            routeIndex++;
            if (routeIndex >= outboundRoute.Length)
            {
                visualAdapter.SetTraveling(false);
                journeyUseCase.NotifyArrivedAtDestination();
            }
        }

        private void AdvanceReturn()
        {
            if (!MoveTowards(outboundRoute[routeIndex])) return;
            routeIndex--;
            if (routeIndex < 0)
            {
                visualAdapter.SetTraveling(false);
                journeyUseCase.NotifyArrivedAtWarehouse();
            }
        }

        private bool MoveTowards(Transform target)
        {
            var current = vehicleRoot.position;
            vehicleRoot.position = Vector3.MoveTowards(
                current,
                target.position,
                travelSpeedMetersPerSecond * Time.deltaTime);

            var heading = target.position - current;
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0001f)
            {
                var desiredRotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                vehicleRoot.rotation = Quaternion.RotateTowards(
                    vehicleRoot.rotation,
                    desiredRotation,
                    turnSpeedDegreesPerSecond * Time.deltaTime);
            }

            return Vector3.SqrMagnitude(vehicleRoot.position - target.position) <= 0.0004f;
        }
    }
}
