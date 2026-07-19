#nullable enable

using System;
using System.Collections.Generic;

namespace HorseParking.Core.Logistics
{
    public enum InventoryFailureReason
    {
        None = 0,
        UnknownResource = 1,
        InsufficientQuantity = 2,
        InsufficientCapacity = 3,
        ConflictingResourceDefinition = 4,
        SameInventory = 5
    }

    /// <summary>Result suitable for translating into UI feedback outside Core.</summary>
    public readonly struct InventoryOperationResult
    {
        private InventoryOperationResult(InventoryFailureReason failureReason)
        {
            FailureReason = failureReason;
        }

        public bool Succeeded => FailureReason == InventoryFailureReason.None;
        public InventoryFailureReason FailureReason { get; }
        public static InventoryOperationResult Success() => new InventoryOperationResult(InventoryFailureReason.None);
        public static InventoryOperationResult Failure(InventoryFailureReason reason) => new InventoryOperationResult(reason);
    }

    /// <summary>Capacity-limited resource storage with atomic transfers.</summary>
    public sealed class ResourceInventory
    {
        private sealed class Entry
        {
            public Entry(ResourceDefinition definition, int quantity)
            {
                Definition = definition;
                Quantity = quantity;
            }

            public ResourceDefinition Definition { get; }
            public int Quantity { get; set; }
        }

        private readonly Dictionary<ResourceId, Entry> entries = new Dictionary<ResourceId, Entry>();

        public ResourceInventory(string id, int capacityUnits)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Inventory id is required.", nameof(id));
            if (capacityUnits <= 0) throw new ArgumentOutOfRangeException(nameof(capacityUnits));

            Id = id.Trim();
            CapacityUnits = capacityUnits;
        }

        public string Id { get; }
        public int CapacityUnits { get; }
        public int UsedCapacityUnits { get; private set; }
        public int AvailableCapacityUnits => CapacityUnits - UsedCapacityUnits;

        public int GetQuantity(ResourceId resourceId)
        {
            return entries.TryGetValue(resourceId, out var entry) ? entry.Quantity : 0;
        }

        public InventoryOperationResult TryAdd(ResourceDefinition definition, int quantity)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ValidateQuantity(quantity);

            if (entries.TryGetValue(definition.Id, out var existing) && !DefinitionsMatch(existing.Definition, definition))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.ConflictingResourceDefinition);
            }

            var requiredCapacity = checked(definition.CapacityPerUnit * quantity);
            if (requiredCapacity > AvailableCapacityUnits)
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.InsufficientCapacity);
            }

            AddUnchecked(definition, quantity, requiredCapacity);
            return InventoryOperationResult.Success();
        }

        public InventoryOperationResult TryRemove(ResourceId resourceId, int quantity)
        {
            ValidateQuantity(quantity);
            if (!entries.TryGetValue(resourceId, out var entry) || entry.Quantity < quantity)
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.InsufficientQuantity);
            }

            RemoveUnchecked(resourceId, entry, quantity);
            return InventoryOperationResult.Success();
        }

        public InventoryOperationResult TryTransferTo(ResourceInventory target, ResourceId resourceId, int quantity)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            ValidateQuantity(quantity);
            if (ReferenceEquals(this, target))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.SameInventory);
            }

            if (!entries.TryGetValue(resourceId, out var sourceEntry) || sourceEntry.Quantity < quantity)
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.InsufficientQuantity);
            }

            if (target.entries.TryGetValue(resourceId, out var targetEntry)
                && !DefinitionsMatch(sourceEntry.Definition, targetEntry.Definition))
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.ConflictingResourceDefinition);
            }

            var requiredCapacity = checked(sourceEntry.Definition.CapacityPerUnit * quantity);
            if (requiredCapacity > target.AvailableCapacityUnits)
            {
                return InventoryOperationResult.Failure(InventoryFailureReason.InsufficientCapacity);
            }

            target.AddUnchecked(sourceEntry.Definition, quantity, requiredCapacity);
            RemoveUnchecked(resourceId, sourceEntry, quantity);
            return InventoryOperationResult.Success();
        }

        private static bool DefinitionsMatch(ResourceDefinition left, ResourceDefinition right)
        {
            return left.Id == right.Id
                && left.CapacityPerUnit == right.CapacityPerUnit
                && string.Equals(left.DisplayNameKey.Value, right.DisplayNameKey.Value, StringComparison.Ordinal);
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        private void AddUnchecked(ResourceDefinition definition, int quantity, int occupiedCapacity)
        {
            if (entries.TryGetValue(definition.Id, out var entry))
            {
                entry.Quantity = checked(entry.Quantity + quantity);
            }
            else
            {
                entries.Add(definition.Id, new Entry(definition, quantity));
            }

            UsedCapacityUnits = checked(UsedCapacityUnits + occupiedCapacity);
        }

        private void RemoveUnchecked(ResourceId resourceId, Entry entry, int quantity)
        {
            entry.Quantity -= quantity;
            UsedCapacityUnits -= checked(entry.Definition.CapacityPerUnit * quantity);
            if (entry.Quantity == 0)
            {
                entries.Remove(resourceId);
            }
        }
    }

    public sealed class Warehouse
    {
        public Warehouse(string id, ResourceInventory inventory)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Warehouse id is required.", nameof(id));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Id = id.Trim();
        }

        public string Id { get; }
        public ResourceInventory Inventory { get; }
    }
}
