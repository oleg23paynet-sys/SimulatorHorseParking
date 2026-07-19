#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using UnityEngine;

namespace HorseParking.Presentation.Logistics
{
    /// <summary>Central editable values for Stage 3 logistics inventory.</summary>
    [CreateAssetMenu(fileName = "LogisticsInventorySettings", menuName = "Horse Parking/Logistics Inventory Settings")]
    public sealed class LogisticsInventorySettings : ScriptableObject
    {
        [Serializable]
        private sealed class ResourceSeed
        {
            [SerializeField] private string id = "resource";
            [SerializeField] private string displayNameKey = "resource.name";
            [Min(1)] [SerializeField] private int capacityPerUnit = 1;
            [Min(0)] [SerializeField] private int initialWarehouseQuantity;
            [Min(0)] [SerializeField] private int initialCartQuantity;
            [Min(1)] [SerializeField] private int storePriceGold = 2;

            public ResourceSeed(string id, string displayNameKey, int capacityPerUnit, int priceGold)
            {
                this.id = id;
                this.displayNameKey = displayNameKey;
                this.capacityPerUnit = capacityPerUnit;
                storePriceGold = priceGold;
            }

            public ResourceDefinition CreateDefinition()
            {
                return new ResourceDefinition(
                    new ResourceId(id),
                    new LocalizationKey(displayNameKey),
                    capacityPerUnit);
            }

            public int InitialWarehouseQuantity => initialWarehouseQuantity;
            public int InitialCartQuantity => initialCartQuantity;
            public int StorePriceGold => storePriceGold;
        }

        [SerializeField] private string warehouseId = "warehouse-main";
        [Min(1)] [SerializeField] private int warehouseCapacityUnits = 200;
        [SerializeField] private string cartId = "cart-starter";
        [Min(1)] [SerializeField] private int cartCapacityUnits = 12;
        [Min(0)] [SerializeField] private int startingGold = 30;
        [SerializeField] private string materialStoreId = "material-store";
        [SerializeField] private string materialStoreNameKey = "location.material_store";
        [Min(0.1f)] [SerializeField] private float cartTravelSpeedMetersPerSecond = 2.2f;
        [SerializeField] private List<ResourceSeed> resources = new List<ResourceSeed>
        {
            new ResourceSeed("wood", "resource.wood", 1, 2),
            new ResourceSeed("stone", "resource.stone", 1, 3),
            new ResourceSeed("iron", "resource.iron", 2, 5)
        };

        public string MaterialStoreId => materialStoreId;
        public float CartTravelSpeedMetersPerSecond => cartTravelSpeedMetersPerSecond;

        public void CreateUseCases(
            out LogisticsInventoryUseCase inventoryUseCase,
            out CartJourneyUseCase journeyUseCase)
        {
            var definitions = new List<ResourceDefinition>(resources.Count);
            var storePrices = new Dictionary<ResourceId, int>();
            foreach (var resource in resources)
            {
                var definition = resource.CreateDefinition();
                definitions.Add(definition);
                storePrices.Add(definition.Id, resource.StorePriceGold);
            }

            var catalog = new ResourceCatalog(definitions);
            var warehouseInventory = new ResourceInventory(warehouseId + ".inventory", warehouseCapacityUnits);
            var cartInventory = new ResourceInventory(cartId + ".cargo", cartCapacityUnits);

            for (var index = 0; index < definitions.Count; index++)
            {
                AddInitialQuantity(warehouseInventory, definitions[index], resources[index].InitialWarehouseQuantity);
                AddInitialQuantity(cartInventory, definitions[index], resources[index].InitialCartQuantity);
            }

            var warehouse = new Warehouse(warehouseId, warehouseInventory);
            var materialStore = new CartDestination(
                materialStoreId,
                new LocalizationKey(materialStoreNameKey));
            var cart = new DeliveryCart(cartId, cartInventory);

            // The demo begins at the loading point. Use the normal domain transitions
            // to seed that state instead of letting Presentation fake a different location.
            var initialDispatch = cart.TryDispatch(materialStore);
            var initialArrival = cart.NotifyArrivedAtDestination();
            if (!initialDispatch.Succeeded || !initialArrival.Succeeded)
            {
                throw new InvalidOperationException("Could not place the starter cart at the material store.");
            }

            inventoryUseCase = new LogisticsInventoryUseCase(catalog, warehouse, cart, storePrices, startingGold);
            journeyUseCase = new CartJourneyUseCase(
                cart,
                new[] { materialStore });
        }

        private static void AddInitialQuantity(ResourceInventory inventory, ResourceDefinition definition, int quantity)
        {
            if (quantity == 0) return;

            var result = inventory.TryAdd(definition, quantity);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Initial quantity does not fit inventory '" + inventory.Id + "': " + result.FailureReason);
            }
        }
    }
}
