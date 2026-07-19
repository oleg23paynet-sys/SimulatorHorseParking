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

    /// <summary>Application boundary for warehouse and cart inventory operations.</summary>
    public sealed class LogisticsInventoryUseCase
    {
        private readonly ResourceCatalog resourceCatalog;
        private readonly Warehouse warehouse;
        private readonly DeliveryCart cart;

        public LogisticsInventoryUseCase(ResourceCatalog resourceCatalog, Warehouse warehouse, DeliveryCart cart)
        {
            this.resourceCatalog = resourceCatalog ?? throw new ArgumentNullException(nameof(resourceCatalog));
            this.warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            this.cart = cart ?? throw new ArgumentNullException(nameof(cart));
        }

        public string WarehouseId => warehouse.Id;
        public string CartId => cart.Id;

        public InventorySnapshot GetWarehouseSnapshot() => CreateSnapshot(warehouse.Inventory);

        public InventorySnapshot GetCartSnapshot() => CreateSnapshot(cart.Cargo);

        public InventoryOperationResult TryLoadCart(ResourceId resourceId, int quantity)
        {
            if (!resourceCatalog.TryGet(resourceId, out _))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.UnknownResource);
            }

            return warehouse.Inventory.TryTransferTo(cart.Cargo, resourceId, quantity);
        }

        public InventoryOperationResult TryUnloadCart(ResourceId resourceId, int quantity)
        {
            if (!resourceCatalog.TryGet(resourceId, out _))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.UnknownResource);
            }

            return cart.Cargo.TryTransferTo(warehouse.Inventory, resourceId, quantity);
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
    }
}
