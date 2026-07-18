using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
        /// <summary>Visual route only. It delegates all game-state changes to callbacks.</summary>
    public sealed class MountedClientRoutePresenter : MonoBehaviour
    {
        [SerializeField] private Transform clientRoot = null!;
        [SerializeField] private Transform entryLanePoint = null!;
        [SerializeField] private Transform parkingPoint = null!;
        [SerializeField] private Transform paymentPoint = null!;
        [SerializeField] private Transform exitPoint = null!;
        [SerializeField] private MonoBehaviour animationAdapterBehaviour = null!;
        [SerializeField] private float speed = 2.2f;

        private IMountedClientAnimation animationAdapter = null!;
        private Action? parked;
        private Action? arrivedAtPayment;
        private Action? exited;
        private Transform? destination;
        private readonly Queue<Transform> waypoints = new Queue<Transform>();
        private RouteState state;

        private enum RouteState { None, Arriving, GoingToPayment, Exiting }

        private void Awake()
        {
            animationAdapter = animationAdapterBehaviour as IMountedClientAnimation;
            if (animationAdapter == null)
            {
                Debug.LogError("Mounted client route is missing an IMountedClientAnimation adapter.", this);
                enabled = false;
            }
        }

        public void Configure(Transform client, Transform entryLane, Transform parking, Transform payment, Transform exit, MonoBehaviour adapter, Action onParked, Action onArrivedAtPayment, Action onExited)
        {
            clientRoot = client;
            entryLanePoint = entryLane;
            parkingPoint = parking;
            paymentPoint = payment;
            exitPoint = exit;
            animationAdapterBehaviour = adapter;
            animationAdapter = adapter as IMountedClientAnimation ?? throw new ArgumentException("Animation adapter must implement IMountedClientAnimation.");
            parked = onParked;
            arrivedAtPayment = onArrivedAtPayment;
            exited = onExited;
        }

        /// <summary>
        /// Delegates are runtime-only and are not serialized by Unity. The runtime
        /// controller must bind them on every Play, even when the scene was built in Editor.
        /// </summary>
        public void BindCallbacks(Action onParked, Action onArrivedAtPayment, Action onExited)
        {
            parked = onParked;
            arrivedAtPayment = onArrivedAtPayment;
            exited = onExited;
        }

        // The client never takes a direct diagonal through the fences. Each route has
        // explicit road points, kept here rather than in parking Core rules.
        public void BeginArrival() => BeginRoute(RouteState.Arriving, entryLanePoint, parkingPoint);
        public void BeginPaymentApproach() => BeginRoute(RouteState.GoingToPayment, entryLanePoint, paymentPoint);
        public void BeginExit() => BeginRoute(RouteState.Exiting, exitPoint);

        private void BeginRoute(RouteState nextState, params Transform[] routePoints)
        {
            state = nextState;
            waypoints.Clear();
            foreach (var routePoint in routePoints)
            {
                waypoints.Enqueue(routePoint);
            }
            destination = waypoints.Dequeue();
            animationAdapter.SetWalking(true);
        }

        private void Update()
        {
            if (destination == null || clientRoot == null)
            {
                return;
            }

            var target = destination.position;
            target.y = clientRoot.position.y;
            var offset = target - clientRoot.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.0025f)
            {
                clientRoot.rotation = Quaternion.RotateTowards(clientRoot.rotation, Quaternion.LookRotation(offset.normalized, Vector3.up), 300f * Time.deltaTime);
                clientRoot.position = Vector3.MoveTowards(clientRoot.position, target, speed * Time.deltaTime);
                return;
            }

            destination = waypoints.Count > 0 ? waypoints.Dequeue() : null;
            if (destination != null)
            {
                return;
            }

            animationAdapter.SetWalking(false);
            switch (state)
            {
                case RouteState.Arriving: parked?.Invoke(); break;
                case RouteState.GoingToPayment: arrivedAtPayment?.Invoke(); break;
                case RouteState.Exiting: exited?.Invoke(); break;
            }
            state = RouteState.None;
        }
    }
}
