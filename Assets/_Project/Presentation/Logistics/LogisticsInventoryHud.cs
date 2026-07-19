#nullable enable

using System.Collections.Generic;
using System.Text;
using HorseParking.Application.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Logistics
{
    /// <summary>Read-only localized HUD for the Stage 3.1 warehouse and starter cart.</summary>
    [DisallowMultipleComponent]
    public sealed class LogisticsInventoryHud : MonoBehaviour
    {
        private static readonly LocalizationKey TitleKey = new LocalizationKey("ui.logistics.title");
        private static readonly LocalizationKey WarehouseKey = new LocalizationKey("ui.inventory.warehouse");
        private static readonly LocalizationKey CartKey = new LocalizationKey("ui.inventory.cart");
        private static readonly LocalizationKey CapacityKey = new LocalizationKey("ui.inventory.capacity");
        private static readonly LocalizationKey ResourceLineKey = new LocalizationKey("ui.inventory.resource_line");
        private static readonly LocalizationKey JourneyStatusKey = new LocalizationKey("ui.cart.status");

        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private Text headerText = null!;
        [SerializeField] private Text warehouseText = null!;
        [SerializeField] private Text cartText = null!;
        [SerializeField] private Text journeyText = null!;
        [Min(0.05f)] [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private float nextRefreshTime;

        public void Configure(
            GameCompositionRoot root,
            Text header,
            Text warehouse,
            Text cart,
            Text journey)
        {
            compositionRoot = root;
            headerText = header;
            warehouseText = warehouse;
            cartText = cart;
            journeyText = journey;
        }

        private void Start()
        {
            if (compositionRoot == null || !compositionRoot.HasLogisticsInventory)
            {
                Debug.LogError("Logistics HUD is missing its configured inventory use case.", this);
                enabled = false;
                return;
            }

            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
            var localization = compositionRoot.LocalizationService;
            var inventory = compositionRoot.LogisticsInventoryUseCase;

            headerText.text = localization.Translate(TitleKey);
            warehouseText.text = BuildInventoryText(
                localization,
                WarehouseKey,
                inventory.GetWarehouseSnapshot());
            cartText.text = BuildInventoryText(
                localization,
                CartKey,
                inventory.GetCartSnapshot());

            if (journeyText != null && compositionRoot.HasCartJourney)
            {
                var journey = compositionRoot.CartJourneyUseCase.GetSnapshot();
                journeyText.text = localization.Translate(
                    JourneyStatusKey,
                    new Dictionary<string, object>
                    {
                        ["state"] = localization.Translate(CartJourneyLocalization.GetStateKey(journey.State))
                    });
            }
        }

        private static string BuildInventoryText(
            ILocalizationService localization,
            LocalizationKey titleKey,
            InventorySnapshot snapshot)
        {
            var builder = new StringBuilder();
            builder.AppendLine(localization.Translate(titleKey));
            builder.AppendLine(localization.Translate(
                CapacityKey,
                new Dictionary<string, object>
                {
                    ["used"] = snapshot.UsedCapacityUnits,
                    ["capacity"] = snapshot.CapacityUnits
                }));

            foreach (var item in snapshot.Items)
            {
                builder.AppendLine(localization.Translate(
                    ResourceLineKey,
                    new Dictionary<string, object>
                    {
                        ["resource"] = localization.Translate(item.DisplayNameKey),
                        ["quantity"] = item.Quantity
                    }));
            }

            return builder.ToString().TrimEnd();
        }
    }
}
