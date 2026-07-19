#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    public sealed class DeliveryCartVisualAdapter : MonoBehaviour
    {
        [SerializeField] private Animator cartAnimator = null!;
        [SerializeField] private Animator driverAnimator = null!;
        [SerializeField] private GameObject woodCargoPrefab = null!;
        [SerializeField] private GameObject stoneCargoPrefab = null!;
        [SerializeField] private GameObject ironCargoPrefab = null!;
        [SerializeField] private GameObject[] woodCargoVisuals = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] stoneCargoVisuals = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] ironCargoVisuals = System.Array.Empty<GameObject>();
        private readonly Dictionary<ResourceId, GameObject[]> cargoVisuals = new();
        private Transform? cargoAnchor;
        private GameCompositionRoot? pendingCompositionRoot;
        private LogisticsInventoryUseCase? inventoryUseCase;
        private int observedCargoSignature = int.MinValue;

        public void Configure(Animator cart, Animator driver)
        {
            cartAnimator = cart;
            driverAnimator = driver;
        }

        public void ConfigureCargoPrefabs(GameObject wood, GameObject stone, GameObject iron)
        {
            woodCargoPrefab = wood;
            stoneCargoPrefab = stone;
            ironCargoPrefab = iron;
        }

        public void ConfigureCargoVisuals(GameObject[] wood, GameObject[] stone, GameObject[] iron)
        {
            woodCargoVisuals = wood ?? System.Array.Empty<GameObject>();
            stoneCargoVisuals = stone ?? System.Array.Empty<GameObject>();
            ironCargoVisuals = iron ?? System.Array.Empty<GameObject>();
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
            pendingCompositionRoot = compositionRoot;
            if (compositionRoot == null || !compositionRoot.HasLogisticsInventory) return;
            if (inventoryUseCase != null)
            {
                inventoryUseCase.CartInventoryChanged -= RefreshCargo;
            }
            inventoryUseCase = compositionRoot.LogisticsInventoryUseCase;
            inventoryUseCase.CartInventoryChanged += RefreshCargo;
            CreateCargoMarkers();
            observedCargoSignature = int.MinValue;
            RefreshCargo();
        }

        private void LateUpdate()
        {
            if (inventoryUseCase == null && pendingCompositionRoot != null
                && pendingCompositionRoot.HasLogisticsInventory)
            {
                BindInventory(pendingCompositionRoot);
            }
            RefreshCargo();
        }

        private void OnDestroy()
        {
            if (inventoryUseCase != null)
            {
                inventoryUseCase.CartInventoryChanged -= RefreshCargo;
            }
        }

        private void RefreshCargo()
        {
            if (inventoryUseCase == null || cargoVisuals.Count == 0) return;
            var snapshot = inventoryUseCase.GetCartSnapshot();
            var cargoSignature = 17;
            foreach (var item in snapshot.Items)
            {
                cargoSignature = unchecked(cargoSignature * 31 + item.ResourceId.GetHashCode());
                cargoSignature = unchecked(cargoSignature * 31 + item.Quantity);
            }

            if (cargoSignature == observedCargoSignature) return;
            observedCargoSignature = cargoSignature;
            foreach (var item in snapshot.Items)
            {
                if (cargoVisuals.TryGetValue(item.ResourceId, out var visuals))
                {
                    var visibleCount = item.Quantity <= 0
                        ? 0
                        : Mathf.Clamp(Mathf.CeilToInt(item.Quantity / 2f), 1, visuals.Length);
                    for (var index = 0; index < visuals.Length; index++)
                    {
                        visuals[index].SetActive(index < visibleCount);
                    }
                }
            }
        }

        private void CreateCargoMarkers()
        {
            if (cartAnimator == null || cargoVisuals.Count > 0) return;
            EnsureCargoAnchor();
            ArrangeCargoVisuals(woodCargoVisuals, "Wood", 0f);
            ArrangeCargoVisuals(stoneCargoVisuals, "Stone", 0.46f);
            ArrangeCargoVisuals(ironCargoVisuals, "Iron", -0.46f);
            RegisterCargoVisuals(
                new ResourceId("wood"), woodCargoVisuals, woodCargoPrefab, "DeliveryCartCargo_Wood", -0.42f, 0.68f);
            RegisterCargoVisuals(
                new ResourceId("stone"), stoneCargoVisuals, stoneCargoPrefab, "DeliveryCartCargo_Stone", 0f, 0.58f);
            RegisterCargoVisuals(
                new ResourceId("iron"), ironCargoVisuals, ironCargoPrefab, "DeliveryCartCargo_Iron", 0.42f, 0.60f);
        }

        private void EnsureCargoAnchor()
        {
            cargoAnchor = transform.Find("DeliveryCartCargoAnchor");
            if (cargoAnchor == null)
            {
                cargoAnchor = new GameObject("DeliveryCartCargoAnchor").transform;
                cargoAnchor.SetParent(transform, false);
            }

            var cartBounds = GetBounds(cartAnimator.gameObject);
            var cartBedWorld = new Vector3(transform.position.x, cartBounds.max.y - 0.2f, transform.position.z);
            var cartBedLocal = transform.InverseTransformPoint(cartBedWorld);
            cargoAnchor.localPosition = new Vector3(0f, cartBedLocal.y, 0.08f);
            cargoAnchor.localRotation = Quaternion.identity;
            cargoAnchor.localScale = Vector3.one;
        }

        private void ArrangeCargoVisuals(GameObject[] visuals, string resourceName, float localZ)
        {
            if (cargoAnchor == null) return;
            for (var index = 0; index < visuals.Length; index++)
            {
                var visual = visuals[index];
                if (visual == null) continue;
                var slot = new GameObject("CargoSlot_" + resourceName + "_" + (index + 1)).transform;
                slot.SetParent(cargoAnchor, false);
                slot.localPosition = new Vector3(index == 0 ? -0.3f : 0.3f, 0f, localZ);
                slot.localRotation = Quaternion.identity;
                slot.localScale = Vector3.one;

                visual.transform.SetParent(slot, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                FitVisualInsideSlot(visual, 0.5f, 0.42f, 0.48f);
                var bounds = GetBounds(visual);
                var slotWorld = slot.position;
                var worldCorrection = new Vector3(
                    slotWorld.x - bounds.center.x,
                    slotWorld.y - bounds.min.y,
                    slotWorld.z - bounds.center.z);
                visual.transform.localPosition += slot.InverseTransformVector(worldCorrection);
            }
        }

        private static void FitVisualInsideSlot(
            GameObject visual,
            float maxWidth,
            float maxDepth,
            float maxHeight)
        {
            var bounds = GetBounds(visual);
            if (bounds.size.x <= 0.001f || bounds.size.y <= 0.001f || bounds.size.z <= 0.001f) return;
            var scale = Mathf.Min(
                1f,
                Mathf.Min(
                    maxWidth / bounds.size.x,
                    Mathf.Min(maxDepth / bounds.size.z, maxHeight / bounds.size.y)));
            visual.transform.localScale *= scale;
        }

        private void RegisterCargoVisuals(
            ResourceId resourceId,
            GameObject[] configuredVisuals,
            GameObject prefab,
            string objectName,
            float localX,
            float targetHeight)
        {
            var validVisuals = new List<GameObject>();
            foreach (var visual in configuredVisuals)
            {
                if (visual != null) validVisuals.Add(visual);
            }

            if (validVisuals.Count == 0)
            {
                var fallback = CreateCargoVisual(resourceId, prefab, objectName, localX, targetHeight);
                if (fallback != null) validVisuals.Add(fallback);
            }

            if (validVisuals.Count == 0) return;
            foreach (var visual in validVisuals)
            {
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                visual.SetActive(false);
            }
            cargoVisuals.Add(resourceId, validVisuals.ToArray());
        }

        private GameObject? CreateCargoVisual(
            ResourceId resourceId,
            GameObject prefab,
            string objectName,
            float localX,
            float targetHeight)
        {
            if (prefab == null)
            {
                Debug.LogError("Cargo prefab is missing for resource '" + resourceId.Value + "'.", this);
                return null;
            }
            var visual = Instantiate(prefab, transform);
            visual.name = objectName;
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var initialBounds = GetBounds(visual);
            var scale = initialBounds.size.y > 0.01f ? targetHeight / initialBounds.size.y : 1f;
            visual.transform.localScale *= scale;

            // Anchor cargo from the actual rendered cart height. The previous fixed
            // height placed the props inside the wooden body on this imported FBX.
            var cartBounds = GetBounds(cartAnimator.gameObject);
            var desiredBottomCenter = transform.TransformPoint(new Vector3(localX, 0f, 0.12f));
            desiredBottomCenter.y = cartBounds.max.y - 0.18f;
            var scaledBounds = GetBounds(visual);
            visual.transform.position += new Vector3(
                desiredBottomCenter.x - scaledBounds.center.x,
                desiredBottomCenter.y - scaledBounds.min.y,
                desiredBottomCenter.z - scaledBounds.center.z);
            visual.SetActive(false);
            return visual;
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
