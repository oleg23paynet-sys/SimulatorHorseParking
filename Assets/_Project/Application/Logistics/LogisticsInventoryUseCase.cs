#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;

namespace HorseParking.Application.Logistics
{
    public sealed class InventoryItemSnapshot
    {
        public InventoryItemSnapshot(ResourceId resourceId, LocalizationKey displayNameKey, int quantity, int capacityPerUnit)
        {
            ResourceId = resourceId;
            DisplayNameKey = displayNameKey;
            Quantity = quantity;
            CapacityPerUnit = capacityPerUnit;
        }

        public ResourceId ResourceId { get; }
        public LocalizationKey DisplayNameKey { get; }
        public int Quantity { get; }
        public int CapacityPerUnit { get; }
    }

    public sealed class InventorySnapshot
    {
        public InventorySnapshot(
            string inventoryId,
            int capacityUnits,
            int usedCapacityUnits,
            IReadOnlyList<InventoryItemSnapshot> items)
        {
            InventoryId = inventoryId;
            CapacityUnits = capacityUnits;
            UsedCapacityUnits = usedCapacityUnits;
            Items = items;
        }

        public string InventoryId { get; }
        public int CapacityUnits { get; }
        public int UsedCapacityUnits { get; }
        public int AvailableCapacityUnits => CapacityUnits - UsedCapacityUnits;
        public IReadOnlyList<InventoryItemSnapshot> Items { get; }
    }

    public sealed class StoreOfferSnapshot
    {
        public StoreOfferSnapshot(ResourceId resourceId, LocalizationKey displayNameKey, int priceGold)
        {
            ResourceId = resourceId;
            DisplayNameKey = displayNameKey;
            PriceGold = priceGold;
        }

        public ResourceId ResourceId { get; }
        public LocalizationKey DisplayNameKey { get; }
        public int PriceGold { get; }
    }

    public enum CartUnloadFailureReason
    {
        None = 0,
        UnknownResource = 1,
        CartIsEmpty = 2,
        WarehouseHasNoSpace = 3
    }

    /// <summary>Capacity-aware result for manual cart-to-warehouse transfers.</summary>
    public readonly struct CartUnloadResult
    {
        public CartUnloadResult(
            CartUnloadFailureReason failureReason,
            int requestedQuantity,
            int transferredQuantity)
        {
            FailureReason = failureReason;
            RequestedQuantity = requestedQuantity;
            TransferredQuantity = transferredQuantity;
        }

        public CartUnloadFailureReason FailureReason { get; }
        public int RequestedQuantity { get; }
        public int TransferredQuantity { get; }
        public bool Succeeded => TransferredQuantity > 0;
        public bool FullyTransferred => RequestedQuantity > 0 && RequestedQuantity == TransferredQuantity;
    }

    /// <summary>Application boundary for warehouse and cart inventory operations.</summary>
    public sealed class LogisticsInventoryUseCase
    {
        private readonly ResourceCatalog resourceCatalog;
        private readonly Warehouse warehouse;
        private readonly DeliveryCart cart;
        private readonly IReadOnlyDictionary<ResourceId, int> storePrices;
        private int gold;

        public event Action? CartInventoryChanged;
        public event Action? WarehouseInventoryChanged;

        public LogisticsInventoryUseCase(
            ResourceCatalog resourceCatalog,
            Warehouse warehouse,
            DeliveryCart cart,
            IReadOnlyDictionary<ResourceId, int> storePrices,
            int startingGold)
        {
            this.resourceCatalog = resourceCatalog ?? throw new ArgumentNullException(nameof(resourceCatalog));
            this.warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            this.cart = cart ?? throw new ArgumentNullException(nameof(cart));
            this.storePrices = storePrices ?? throw new ArgumentNullException(nameof(storePrices));
            if (startingGold < 0) throw new ArgumentOutOfRangeException(nameof(startingGold));
            gold = startingGold;
        }

        public string WarehouseId => warehouse.Id;
        public string CartId => cart.Id;
        public int Gold => gold;

        public IReadOnlyList<StoreOfferSnapshot> GetStoreOffers()
        {
            var offers = new List<StoreOfferSnapshot>();
            foreach (var definition in resourceCatalog.Definitions)
            {
                if (storePrices.TryGetValue(definition.Id, out var price))
                {
                    offers.Add(new StoreOfferSnapshot(definition.Id, definition.DisplayNameKey, price));
                }
            }

            return offers.AsReadOnly();
        }

        public InventorySnapshot GetWarehouseSnapshot() => CreateSnapshot(warehouse.Inventory);

        public InventorySnapshot GetCartSnapshot() => CreateSnapshot(cart.Cargo);

        public InventoryOperationResult TryLoadCart(ResourceId resourceId, int quantity)
        {
            if (!resourceCatalog.TryGet(resourceId, out _))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.UnknownResource);
            }

            var result = warehouse.Inventory.TryTransferTo(cart.Cargo, resourceId, quantity);
            if (result.Succeeded) CartInventoryChanged?.Invoke();
            return result;
        }

        public InventoryOperationResult TryUnloadCart(ResourceId resourceId, int quantity)
        {
            if (!resourceCatalog.TryGet(resourceId, out _))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.UnknownResource);
            }

            var result = cart.Cargo.TryTransferTo(warehouse.Inventory, resourceId, quantity);
            if (result.Succeeded) NotifyInventoryTransfer();
            return result;
        }

        /// <summary>
        /// Transfers as much of one resource as the warehouse can accept. The operation
        /// never creates negative quantities and reports a partial transfer explicitly.
        /// </summary>
        public CartUnloadResult TryUnloadCartUpToCapacity(ResourceId resourceId, int requestedQuantity)
        {
            if (requestedQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(requestedQuantity));
            if (!resourceCatalog.TryGet(resourceId, out var definition))
            {
                return new CartUnloadResult(CartUnloadFailureReason.UnknownResource, requestedQuantity, 0);
            }

            var availableInCart = cart.Cargo.GetQuantity(resourceId);
            if (availableInCart <= 0)
            {
                return new CartUnloadResult(CartUnloadFailureReason.CartIsEmpty, requestedQuantity, 0);
            }

            var capacityForUnits = warehouse.Inventory.AvailableCapacityUnits / definition.CapacityPerUnit;
            var transferable = Math.Min(requestedQuantity, Math.Min(availableInCart, capacityForUnits));
            if (transferable <= 0)
            {
                return new CartUnloadResult(CartUnloadFailureReason.WarehouseHasNoSpace, requestedQuantity, 0);
            }

            var transfer = cart.Cargo.TryTransferTo(warehouse.Inventory, resourceId, transferable);
            if (!transfer.Succeeded)
            {
                return new CartUnloadResult(CartUnloadFailureReason.WarehouseHasNoSpace, requestedQuantity, 0);
            }

            NotifyInventoryTransfer();
            var reason = transferable < requestedQuantity
                ? CartUnloadFailureReason.WarehouseHasNoSpace
                : CartUnloadFailureReason.None;
            return new CartUnloadResult(reason, requestedQuantity, transferable);
        }

        /// <summary>Atomically checks total capacity before unloading every resource.</summary>
        public CartUnloadResult TryUnloadAllCartCargo()
        {
            var snapshot = GetCartSnapshot();
            var totalQuantity = 0;
            foreach (var item in snapshot.Items) totalQuantity = checked(totalQuantity + item.Quantity);
            if (totalQuantity <= 0)
            {
                return new CartUnloadResult(CartUnloadFailureReason.CartIsEmpty, 0, 0);
            }

            if (snapshot.UsedCapacityUnits > warehouse.Inventory.AvailableCapacityUnits)
            {
                return new CartUnloadResult(CartUnloadFailureReason.WarehouseHasNoSpace, totalQuantity, 0);
            }

            foreach (var item in snapshot.Items)
            {
                if (item.Quantity <= 0) continue;
                var transfer = cart.Cargo.TryTransferTo(warehouse.Inventory, item.ResourceId, item.Quantity);
                if (!transfer.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Cart unload-all capacity was validated, but a resource transfer failed.");
                }
            }

            NotifyInventoryTransfer();
            return new CartUnloadResult(CartUnloadFailureReason.None, totalQuantity, totalQuantity);
        }

        public PurchaseResult TryPurchaseForCart(ResourceId resourceId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (!resourceCatalog.TryGet(resourceId, out var definition)
                || !storePrices.TryGetValue(resourceId, out var unitPrice))
            {
                return PurchaseResult.Failure(PurchaseFailureReason.UnknownResource, gold);
            }

            var totalPrice = checked(unitPrice * quantity);
            if (gold < totalPrice)
            {
                return PurchaseResult.Failure(PurchaseFailureReason.InsufficientGold, gold);
            }

            var addResult = cart.Cargo.TryAdd(definition, quantity);
            if (!addResult.Succeeded)
            {
                return PurchaseResult.Failure(PurchaseFailureReason.InsufficientCartCapacity, gold);
            }

            gold -= totalPrice;
            CartInventoryChanged?.Invoke();
            return PurchaseResult.Success(gold);
        }

        private InventorySnapshot CreateSnapshot(ResourceInventory inventory)
        {
            var items = new List<InventoryItemSnapshot>(resourceCatalog.Definitions.Count);
            foreach (var definition in resourceCatalog.Definitions)
            {
                items.Add(new InventoryItemSnapshot(
                    definition.Id,
                    definition.DisplayNameKey,
                    inventory.GetQuantity(definition.Id),
                    definition.CapacityPerUnit));
            }

            return new InventorySnapshot(
                inventory.Id,
                inventory.CapacityUnits,
                inventory.UsedCapacityUnits,
                items.AsReadOnly());
        }

        private void NotifyInventoryTransfer()
        {
            CartInventoryChanged?.Invoke();
            WarehouseInventoryChanged?.Invoke();
        }
    }
}
