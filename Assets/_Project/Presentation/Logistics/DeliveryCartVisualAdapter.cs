#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    public sealed class DeliveryCartVisualAdapter : MonoBehaviour
    {
        [SerializeField] private Animator cartAnimator = null!;
        [SerializeField] private Animator driverAnimator = null!;
        private readonly List<GameObject> cargoMarkers = new();
        private LogisticsInventoryUseCase? inventoryUseCase;
        private int observedUsedCapacity = -1;

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

        public void BindInventory(GameCompositionRoot compositionRoot)
        {
            if (compositionRoot == null || !compositionRoot.HasLogisticsInventory) return;
            inventoryUseCase = compositionRoot.LogisticsInventoryUseCase;
            CreateCargoMarkers();
            RefreshCargo();
        }

        private void LateUpdate()
        {
            RefreshCargo();
        }

        private void RefreshCargo()
        {
            if (inventoryUseCase == null || cargoMarkers.Count == 0) return;
            var snapshot = inventoryUseCase.GetCartSnapshot();
            if (snapshot.UsedCapacityUnits == observedUsedCapacity) return;
            observedUsedCapacity = snapshot.UsedCapacityUnits;
            for (var index = 0; index < cargoMarkers.Count; index++)
            {
                var threshold = 1 + index * Mathf.Max(1, snapshot.CapacityUnits / cargoMarkers.Count);
                cargoMarkers[index].SetActive(snapshot.UsedCapacityUnits >= threshold);
            }
        }

        private void CreateCargoMarkers()
        {
            if (cartAnimator == null || cargoMarkers.Count > 0) return;
            GameObject? template = null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == "PaymentSack_01" && candidate.scene.IsValid())
                {
                    template = candidate;
                    break;
                }
            }

            if (template == null) return;
            var cartBounds = GetBounds(cartAnimator.gameObject);
            var templateBounds = GetBounds(template);
            var scale = templateBounds.size.y > 0.01f ? 0.46f / templateBounds.size.y : 0.5f;
            for (var index = 0; index < 3; index++)
            {
                var marker = Instantiate(template);
                marker.name = "DeliveryCartCargoSack_" + (index + 1);
                foreach (var behaviour in marker.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    behaviour.enabled = false;
                    Destroy(behaviour);
                }
                foreach (var collider in marker.GetComponentsInChildren<Collider>(true)) Destroy(collider);
                foreach (var body in marker.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
                marker.transform.SetParent(cartAnimator.transform, true);
                marker.transform.rotation = cartAnimator.transform.rotation;
                marker.transform.localScale = Vector3.one * scale;
                marker.transform.position = cartBounds.center
                    + Vector3.up * (cartBounds.extents.y * 0.42f)
                    + cartAnimator.transform.right * ((index - 1) * 0.34f);
                marker.SetActive(false);
                cargoMarkers.Add(marker);
            }
        }

        private static Bounds GetBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }
    }
}
