#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Logistics
{
    /// <summary>Manual, inventory-backed cart-to-warehouse unloading UI.</summary>
    public sealed class CartUnloadPanel : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private FirstPersonPlayerController playerController = null!;
        [SerializeField] private CartUnloadInteractionTarget interactionTarget = null!;
        [SerializeField] private GameObject panelRoot = null!;
        [SerializeField] private Text titleText = null!;
        [SerializeField] private Text capacityText = null!;
        [SerializeField] private Text feedbackText = null!;
        [SerializeField] private Button resourceButtonTemplate = null!;
        [SerializeField] private Button unloadAllButton = null!;
        [SerializeField] private Text unloadAllButtonText = null!;
        [SerializeField] private Button closeButton = null!;
        [SerializeField] private Text closeButtonText = null!;
        [SerializeField] private string materialStoreId = "material-store";

        private readonly List<ResourceRow> rows = new();
        private LogisticsInventoryUseCase inventoryUseCase = null!;
        private CartJourneyUseCase journeyUseCase = null!;
        private bool isOpen;
        private string feedbackKey = "ui.cart_unload.feedback.ready";

        private sealed class ResourceRow
        {
            public ResourceRow(ResourceId resourceId, LocalizationKey nameKey, Button button, Text label)
            {
                ResourceId = resourceId;
                NameKey = nameKey;
                Button = button;
                Label = label;
            }

            public ResourceId ResourceId { get; }
            public LocalizationKey NameKey { get; }
            public Button Button { get; }
            public Text Label { get; }
        }

        public void Configure(
            GameCompositionRoot root,
            FirstPersonPlayerController player,
            CartUnloadInteractionTarget target,
            GameObject panel,
            Text title,
            Text capacity,
            Text feedback,
            Button rowTemplate,
            Button unloadAll,
            Text unloadAllLabel,
            Button close,
            Text closeLabel,
            string destinationId)
        {
            compositionRoot = root;
            playerController = player;
            interactionTarget = target;
            panelRoot = panel;
            titleText = title;
            capacityText = capacity;
            feedbackText = feedback;
            resourceButtonTemplate = rowTemplate;
            unloadAllButton = unloadAll;
            unloadAllButtonText = unloadAllLabel;
            closeButton = close;
            closeButtonText = closeLabel;
            materialStoreId = destinationId;
        }

        private void Start()
        {
            if (compositionRoot == null || playerController == null || interactionTarget == null
                || panelRoot == null || resourceButtonTemplate == null)
            {
                Debug.LogError("Cart unload panel is not configured.", this);
                enabled = false;
                return;
            }

            inventoryUseCase = compositionRoot.LogisticsInventoryUseCase;
            journeyUseCase = compositionRoot.CartJourneyUseCase;
            interactionTarget.InteractionRequested += Open;
            inventoryUseCase.CartInventoryChanged += Refresh;
            inventoryUseCase.WarehouseInventoryChanged += Refresh;
            unloadAllButton.onClick.AddListener(UnloadAll);
            closeButton.onClick.AddListener(Close);
            CreateResourceRows();
            resourceButtonTemplate.gameObject.SetActive(false);
            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (interactionTarget != null) interactionTarget.InteractionRequested -= Open;
            if (inventoryUseCase != null)
            {
                inventoryUseCase.CartInventoryChanged -= Refresh;
                inventoryUseCase.WarehouseInventoryChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (!isOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void CreateResourceRows()
        {
            var items = inventoryUseCase.GetCartSnapshot().Items;
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var button = Instantiate(resourceButtonTemplate, resourceButtonTemplate.transform.parent);
                button.name = "Unload_" + item.ResourceId.Value;
                button.onClick = new Button.ButtonClickedEvent();
                var capturedId = item.ResourceId;
                button.onClick.AddListener(() => UnloadResource(capturedId));
                var rect = button.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, 70f - index * 66f);
                rect.sizeDelta = new Vector2(620f, 54f);
                rows.Add(new ResourceRow(
                    item.ResourceId,
                    item.DisplayNameKey,
                    button,
                    button.GetComponentInChildren<Text>(true)));
            }
        }

        private void Open()
        {
            if (journeyUseCase.GetSnapshot().State != CartJourneyState.AtWarehouse) return;
            feedbackKey = "ui.cart_unload.feedback.ready";
            isOpen = true;
            panelRoot.SetActive(true);
            playerController.SetUiInputBlocked(true);
            Refresh();
        }

        private void Close()
        {
            isOpen = false;
            panelRoot.SetActive(false);
            playerController.SetUiInputBlocked(false);
        }

        private void UnloadResource(ResourceId resourceId)
        {
            var cart = inventoryUseCase.GetCartSnapshot();
            var quantity = FindQuantity(cart, resourceId);
            if (quantity <= 0) return;
            var result = inventoryUseCase.TryUnloadCartUpToCapacity(resourceId, quantity);
            feedbackKey = result.FailureReason == CartUnloadFailureReason.WarehouseHasNoSpace
                ? "ui.cart_unload.feedback.no_space"
                : result.Succeeded
                    ? "ui.cart_unload.feedback.transferred"
                    : "ui.cart_unload.feedback.empty";
            CompleteIfCartIsEmpty();
            Refresh();
        }

        private void UnloadAll()
        {
            var result = inventoryUseCase.TryUnloadAllCartCargo();
            feedbackKey = result.FailureReason == CartUnloadFailureReason.WarehouseHasNoSpace
                ? "ui.cart_unload.feedback.no_space"
                : result.Succeeded
                    ? "ui.cart_unload.feedback.transferred_all"
                    : "ui.cart_unload.feedback.empty";
            CompleteIfCartIsEmpty();
            Refresh();
        }

        private void CompleteIfCartIsEmpty()
        {
            if (inventoryUseCase.GetCartSnapshot().UsedCapacityUnits > 0) return;
            Close();
            journeyUseCase.TryDispatch(materialStoreId);
        }

        private void Refresh()
        {
            if (!isOpen) return;
            var localization = compositionRoot.LocalizationService;
            var cart = inventoryUseCase.GetCartSnapshot();
            var warehouse = inventoryUseCase.GetWarehouseSnapshot();
            titleText.text = localization.Translate(new LocalizationKey("ui.cart_unload.title"));
            capacityText.text = localization.Translate(
                new LocalizationKey("ui.cart_unload.capacity"),
                new Dictionary<string, object>
                {
                    ["used"] = warehouse.UsedCapacityUnits,
                    ["free"] = warehouse.AvailableCapacityUnits,
                    ["capacity"] = warehouse.CapacityUnits
                });
            feedbackText.text = localization.Translate(new LocalizationKey(feedbackKey));
            unloadAllButtonText.text = localization.Translate(new LocalizationKey("ui.cart_unload.unload_all"));
            closeButtonText.text = localization.Translate(new LocalizationKey("ui.common.close"));
            unloadAllButton.interactable = cart.UsedCapacityUnits > 0
                && cart.UsedCapacityUnits <= warehouse.AvailableCapacityUnits;

            foreach (var row in rows)
            {
                var quantity = FindQuantity(cart, row.ResourceId);
                var resourceName = localization.Translate(row.NameKey);
                row.Label.text = localization.Translate(
                    new LocalizationKey("ui.cart_unload.resource_button"),
                    new Dictionary<string, object>
                    {
                        ["resource"] = resourceName,
                        ["quantity"] = quantity
                    });
                row.Button.interactable = quantity > 0 && warehouse.AvailableCapacityUnits > 0;
            }
        }

        private static int FindQuantity(InventorySnapshot snapshot, ResourceId resourceId)
        {
            foreach (var item in snapshot.Items)
            {
                if (item.ResourceId == resourceId) return item.Quantity;
            }
            return 0;
        }
    }
}
