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
        [SerializeField] private float parkingFenceBypassZ = -8f;
        [Min(0.1f)] [SerializeField] private float collisionProbeRadius = 0.55f;
        [Min(0.1f)] [SerializeField] private float collisionProbeHeight = 0.8f;

        private CartJourneyUseCase journeyUseCase = null!;
        private CartJourneyState observedState = (CartJourneyState)(-1);
        private Vector3[] routePositions = System.Array.Empty<Vector3>();
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
            visualAdapter.BindInventory(compositionRoot);
            ConfigureSafeRouteAroundParkingFence();
            var initialState = journeyUseCase.GetSnapshot().State;
            BeginState(initialState);
            observedState = initialState;
        }

        private void ConfigureSafeRouteAroundParkingFence()
        {
            if (outboundRoute.Length < 2) return;
            var warehouseEnd = outboundRoute[0].position;
            var storeEnd = outboundRoute[outboundRoute.Length - 1].position;
            var bypassZ = Mathf.Min(parkingFenceBypassZ, -10.5f);
            const float outsideBuildingClearance = 2.4f;
            routePositions = new[]
            {
                warehouseEnd,
                new Vector3(warehouseEnd.x + outsideBuildingClearance, warehouseEnd.y, warehouseEnd.z),
                new Vector3(warehouseEnd.x + outsideBuildingClearance, warehouseEnd.y, bypassZ),
                new Vector3(storeEnd.x - outsideBuildingClearance, storeEnd.y, bypassZ),
                new Vector3(storeEnd.x - outsideBuildingClearance, storeEnd.y, storeEnd.z),
                storeEnd
            };
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
                    vehicleRoot.SetPositionAndRotation(routePositions[0], GetEndpointRotation(0));
                    visualAdapter.SetTraveling(false);
                    break;
                case CartJourneyState.TravelingToDestination:
                    routeIndex = 1;
                    visualAdapter.SetTraveling(true);
                    break;
                case CartJourneyState.AtDestination:
                    vehicleRoot.SetPositionAndRotation(
                        routePositions[routePositions.Length - 1],
                        GetEndpointRotation(routePositions.Length - 1));
                    visualAdapter.SetTraveling(false);
                    break;
                case CartJourneyState.ReturningToWarehouse:
                    routeIndex = routePositions.Length - 2;
                    visualAdapter.SetTraveling(true);
                    break;
            }
        }

        private void AdvanceOutbound()
        {
            if (!MoveTowards(routePositions[routeIndex])) return;
            routeIndex++;
            if (routeIndex >= routePositions.Length)
            {
                visualAdapter.SetTraveling(false);
                journeyUseCase.NotifyArrivedAtDestination();
            }
        }

        private void AdvanceReturn()
        {
            if (!MoveTowards(routePositions[routeIndex])) return;
            routeIndex--;
            if (routeIndex < 0)
            {
                visualAdapter.SetTraveling(false);
                journeyUseCase.NotifyArrivedAtWarehouse();
            }
        }

        private Quaternion GetEndpointRotation(int index)
        {
            var adjacentIndex = index == 0 ? 1 : index - 1;
            var direction = index == 0
                ? routePositions[index] - routePositions[adjacentIndex]
                : routePositions[index] - routePositions[adjacentIndex];
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : vehicleRoot.rotation;
        }

        private bool MoveTowards(Vector3 target)
        {
            var current = vehicleRoot.position;
            var heading = target - current;
            heading.y = 0f;
            var remainingDistance = heading.magnitude;
            if (remainingDistance > 0.0001f)
            {
                var moveDistance = Mathf.Min(
                    travelSpeedMetersPerSecond * Time.deltaTime,
                    remainingDistance);
                if (IsRouteBlocked(current, heading.normalized, moveDistance))
                {
                    visualAdapter.SetTraveling(false);
                    return false;
                }
                visualAdapter.SetTraveling(true);
                vehicleRoot.position = current + heading.normalized * moveDistance;
            }

            if (heading.sqrMagnitude > 0.0001f)
            {
                var desiredRotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                vehicleRoot.rotation = Quaternion.RotateTowards(
                    vehicleRoot.rotation,
                    desiredRotation,
                    turnSpeedDegreesPerSecond * Time.deltaTime);
            }

            return Vector3.SqrMagnitude(vehicleRoot.position - target) <= 0.0004f;
        }

        private bool IsRouteBlocked(Vector3 current, Vector3 direction, float moveDistance)
        {
            var probeOrigin = current + Vector3.up * collisionProbeHeight;
            if (!Physics.SphereCast(
                    probeOrigin,
                    collisionProbeRadius,
                    direction,
                    out var hit,
                    moveDistance + 0.08f,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.transform != vehicleRoot && !hit.transform.IsChildOf(vehicleRoot);
        }
    }
}
