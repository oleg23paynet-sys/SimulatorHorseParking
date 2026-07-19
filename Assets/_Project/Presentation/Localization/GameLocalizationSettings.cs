#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Core.Localization;
using HorseParking.Infrastructure.Localization;
using UnityEngine;

namespace HorseParking.Presentation.Localization
{
    /// <summary>External translation table used by the Composition Root.</summary>
    [CreateAssetMenu(fileName = "GameLocalizationSettings", menuName = "Horse Parking/Game Localization Settings")]
    public sealed class GameLocalizationSettings : ScriptableObject
    {
        [Serializable]
        private sealed class TranslationEntry
        {
            [SerializeField] private string locale = "en";
            [SerializeField] private string key = "localization.key";
            [TextArea] [SerializeField] private string text = "Translation";

            public TranslationEntry(string locale, string key, string text)
            {
                this.locale = locale;
                this.key = key;
                this.text = text;
            }

            public string Locale => locale;
            public string Key => key;
            public string Text => text;

            public void SetText(string value) => text = value;
        }

        [SerializeField] private string currentLocale = "ru";
        [SerializeField] private List<TranslationEntry> translations = new List<TranslationEntry>
        {
            new TranslationEntry("ru", "ui.logistics.title", "Логистика"),
            new TranslationEntry("ru", "ui.inventory.warehouse", "Склад"),
            new TranslationEntry("ru", "ui.inventory.cart", "Телега"),
            new TranslationEntry("ru", "ui.inventory.capacity", "Вместимость: {used}/{capacity}"),
            new TranslationEntry("ru", "ui.inventory.resource_line", "{resource}: {quantity}"),
            new TranslationEntry("ru", "resource.wood", "Дерево"),
            new TranslationEntry("ru", "resource.stone", "Камень"),
            new TranslationEntry("ru", "resource.iron", "Железо"),
            new TranslationEntry("en", "ui.logistics.title", "Logistics"),
            new TranslationEntry("en", "ui.inventory.warehouse", "Warehouse"),
            new TranslationEntry("en", "ui.inventory.cart", "Cart"),
            new TranslationEntry("en", "ui.inventory.capacity", "Capacity: {used}/{capacity}"),
            new TranslationEntry("en", "ui.inventory.resource_line", "{resource}: {quantity}"),
            new TranslationEntry("en", "resource.wood", "Wood"),
            new TranslationEntry("en", "resource.stone", "Stone"),
            new TranslationEntry("en", "resource.iron", "Iron"),
            new TranslationEntry("ru", "ui.cart.status", "Телега: {state}"),
            new TranslationEntry("ru", "cart.state.at_warehouse", "на складе"),
            new TranslationEntry("ru", "cart.state.traveling_to_store", "едет в магазин"),
            new TranslationEntry("ru", "cart.state.at_store", "у магазина"),
            new TranslationEntry("ru", "cart.state.returning_to_warehouse", "возвращается на склад"),
            new TranslationEntry("ru", "cart.state.unknown", "неизвестно"),
            new TranslationEntry("ru", "ui.cart_dispatch.title", "Управление телегой"),
            new TranslationEntry("ru", "ui.cart_dispatch.current_station", "Вы у: {station}"),
            new TranslationEntry("ru", "ui.cart_dispatch.send_to_store", "Отправить в магазин материалов"),
            new TranslationEntry("ru", "ui.cart_dispatch.return_to_warehouse", "Отправить обратно на склад"),
            new TranslationEntry("ru", "ui.cart_dispatch.instruction.ready_warehouse", "Телега стоит рядом со складом и пока пуста. Со склада сейчас ничего грузить не нужно: отправьте телегу в магазин. Покупка и загрузка будут на следующем этапе."),
            new TranslationEntry("ru", "ui.cart_dispatch.instruction.ready_store", "Телега готова. Нажмите кнопку, чтобы вернуть её на склад."),
            new TranslationEntry("ru", "ui.cart_dispatch.instruction.go_warehouse", "Телега находится НА СКЛАДЕ. Закройте окно и подойдите к высокой постройке с краном справа."),
            new TranslationEntry("ru", "ui.cart_dispatch.instruction.go_store", "Телега находится У МАГАЗИНА. Закройте окно и подойдите к лавке с товарами слева."),
            new TranslationEntry("ru", "ui.cart_dispatch.instruction.in_transit", "Телега уже в пути. Дождитесь её прибытия."),
            new TranslationEntry("ru", "ui.common.close", "Закрыть"),
            new TranslationEntry("ru", "location.warehouse", "Склад"),
            new TranslationEntry("ru", "location.material_store", "Магазин материалов"),
            new TranslationEntry("ru", "interaction.cart.manage", "Управлять телегой"),
            new TranslationEntry("ru", "interaction.cart.opened", "Управление телегой открыто"),
            new TranslationEntry("ru", "ui.interaction.prompt", "[E] {action}: {target}"),
            new TranslationEntry("ru", "ui.cart.guide.warehouse", "ЦЕЛЬ: СКЛАД — высокая постройка с краном справа. Телега с меткой «ТЕЛЕГА» стоит справа от склада. Подойдите к зданию и нажмите E."),
            new TranslationEntry("ru", "ui.cart.guide.store", "ЦЕЛЬ: МАГАЗИН МАТЕРИАЛОВ — лавка с товарами слева. Подойдите к ней и нажмите E."),
            new TranslationEntry("ru", "ui.cart.world_label", "ТЕЛЕГА"),
            new TranslationEntry("en", "ui.cart.status", "Cart: {state}"),
            new TranslationEntry("en", "cart.state.at_warehouse", "at warehouse"),
            new TranslationEntry("en", "cart.state.traveling_to_store", "traveling to material store"),
            new TranslationEntry("en", "cart.state.at_store", "at material store"),
            new TranslationEntry("en", "cart.state.returning_to_warehouse", "returning to warehouse"),
            new TranslationEntry("en", "cart.state.unknown", "unknown"),
            new TranslationEntry("en", "ui.cart_dispatch.title", "Cart management"),
            new TranslationEntry("en", "ui.cart_dispatch.current_station", "You are at: {station}"),
            new TranslationEntry("en", "ui.cart_dispatch.send_to_store", "Send to material store"),
            new TranslationEntry("en", "ui.cart_dispatch.return_to_warehouse", "Return to warehouse"),
            new TranslationEntry("en", "ui.cart_dispatch.instruction.ready_warehouse", "The empty cart is beside the warehouse. Nothing is loaded here yet: send it to the store. Purchasing and loading are handled in the next stage."),
            new TranslationEntry("en", "ui.cart_dispatch.instruction.ready_store", "The cart is ready. Use the button to return it to the warehouse."),
            new TranslationEntry("en", "ui.cart_dispatch.instruction.go_warehouse", "The cart is AT THE WAREHOUSE. Close this window and approach the tall crane building on the right."),
            new TranslationEntry("en", "ui.cart_dispatch.instruction.go_store", "The cart is AT THE STORE. Close this window and approach the market stall on the left."),
            new TranslationEntry("en", "ui.cart_dispatch.instruction.in_transit", "The cart is already traveling. Wait for it to arrive."),
            new TranslationEntry("en", "ui.common.close", "Close"),
            new TranslationEntry("en", "location.warehouse", "Warehouse"),
            new TranslationEntry("en", "location.material_store", "Material store"),
            new TranslationEntry("en", "interaction.cart.manage", "Manage cart"),
            new TranslationEntry("en", "interaction.cart.opened", "Cart management opened"),
            new TranslationEntry("en", "ui.interaction.prompt", "[E] {action}: {target}"),
            new TranslationEntry("en", "ui.cart.guide.warehouse", "OBJECTIVE: WAREHOUSE — the tall crane building on the right. The cart marked CART is parked beside it. Approach the building and press E."),
            new TranslationEntry("en", "ui.cart.guide.store", "OBJECTIVE: MATERIAL STORE — the market stall on the left. Approach it and press E."),
            new TranslationEntry("en", "ui.cart.world_label", "CART")
        };

        public void EnsureTranslation(string locale, string key, string text)
        {
            foreach (var entry in translations)
            {
                if (string.Equals(entry.Locale, locale, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    entry.SetText(text);
                    return;
                }
            }

            translations.Add(new TranslationEntry(locale, key, text));
        }

        public void RemoveTranslation(string locale, string key)
        {
            translations.RemoveAll(entry =>
                string.Equals(entry.Locale, locale, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.Key, key, StringComparison.Ordinal));
        }

        public ILocalizationService CreateService()
        {
            if (string.IsNullOrWhiteSpace(currentLocale))
            {
                throw new InvalidOperationException("Current locale is required.");
            }

            var selectedTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in translations)
            {
                if (!string.Equals(entry.Locale, currentLocale, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    throw new InvalidOperationException("Localization entry contains an empty key.");
                }

                if (!selectedTranslations.TryAdd(entry.Key, entry.Text))
                {
                    throw new InvalidOperationException(
                        "Duplicate localization key for locale '" + currentLocale + "': " + entry.Key);
                }
            }

            return new DictionaryLocalizationService(currentLocale, selectedTranslations);
        }
    }
}
