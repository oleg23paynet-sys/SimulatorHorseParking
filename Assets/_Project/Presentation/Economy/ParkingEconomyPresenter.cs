#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Economy;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Economy
{
    /// <summary>Localized UI adapter for the Stage 4 business use case.</summary>
    [DisallowMultipleComponent]
    public sealed class ParkingEconomyPresenter : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private FirstPersonPlayerController playerController = null!;
        [SerializeField] private GameObject menuPanel = null!;
        [SerializeField] private Text compactText = null!;
        [SerializeField] private Text closeHintText = null!;
        [SerializeField] private GameObject tutorialPanel = null!;
        [SerializeField] private Text tutorialText = null!;
        [SerializeField] private Text summaryText = null!;
        [SerializeField] private Text feedbackText = null!;
        [SerializeField] private Button cartCapacityButton = null!;
        [SerializeField] private Text cartCapacityButtonText = null!;
        [SerializeField] private Button cartSpeedButton = null!;
        [SerializeField] private Text cartSpeedButtonText = null!;
        [SerializeField] private Button constructionSpeedButton = null!;
        [SerializeField] private Text constructionSpeedButtonText = null!;
        [Min(0.05f)] [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private float nextRefreshTime;
        private float tutorialHideTime;
        private bool menuOpen;

        public void Configure(
            GameCompositionRoot root,
            FirstPersonPlayerController player,
            GameObject panel,
            Text compact,
            Text closeHint,
            GameObject tutorial,
            Text tutorialLabel,
            Text summary,
            Text feedback,
            Button capacityButton,
            Text capacityLabel,
            Button speedButton,
            Text speedLabel,
            Button constructionButton,
            Text constructionLabel)
        {
            compositionRoot = root;
            playerController = player;
            menuPanel = panel;
            compactText = compact;
            closeHintText = closeHint;
            tutorialPanel = tutorial;
            tutorialText = tutorialLabel;
            summaryText = summary;
            feedbackText = feedback;
            cartCapacityButton = capacityButton;
            cartCapacityButtonText = capacityLabel;
            cartSpeedButton = speedButton;
            cartSpeedButtonText = speedLabel;
            constructionSpeedButton = constructionButton;
            constructionSpeedButtonText = constructionLabel;
        }

        private void Start()
        {
            if (compositionRoot == null || !compositionRoot.HasParkingEconomy
                || playerController == null || menuPanel == null || compactText == null
                || closeHintText == null
                || tutorialPanel == null || tutorialText == null
                || summaryText == null || feedbackText == null
                || cartCapacityButton == null || cartSpeedButton == null
                || constructionSpeedButton == null)
            {
                Debug.LogError("Parking economy presenter is not configured.", this);
                enabled = false;
                return;
            }

            SetMenuOpen(false);
            tutorialText.text = compositionRoot.LocalizationService.Translate(
                new LocalizationKey("ui.economy.tutorial"));
            tutorialPanel.SetActive(true);
            tutorialHideTime = Time.unscaledTime + 8f;
            cartCapacityButton.onClick.AddListener(() => Purchase(ParkingUpgradeId.CartCapacity));
            cartSpeedButton.onClick.AddListener(() => Purchase(ParkingUpgradeId.CartSpeed));
            constructionSpeedButton.onClick.AddListener(() => Purchase(ParkingUpgradeId.ConstructionSpeed));
            compositionRoot.ParkingEconomyUseCase.EconomyChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (menuOpen && playerController != null)
            {
                playerController.SetUiInputBlocked(false);
            }

            if (compositionRoot != null && compositionRoot.HasParkingEconomy)
            {
                compositionRoot.ParkingEconomyUseCase.EconomyChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (tutorialPanel.activeSelf && Time.unscaledTime >= tutorialHideTime)
            {
                tutorialPanel.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.M)
                || (menuOpen && Input.GetKeyDown(KeyCode.Escape)))
            {
                SetMenuOpen(!menuOpen);
            }

            compositionRoot.ParkingEconomyUseCase.Advance(Time.deltaTime);
            if (Time.unscaledTime >= nextRefreshTime) Refresh();
        }

        private void SetMenuOpen(bool open)
        {
            if (open && !menuOpen && Cursor.visible)
            {
                return;
            }

            menuOpen = open;
            menuPanel.SetActive(open);
            playerController.SetUiInputBlocked(open);
            if (open)
            {
                tutorialPanel.SetActive(false);
            }
        }

        private void Purchase(ParkingUpgradeId id)
        {
            compositionRoot.ParkingEconomyUseCase.TryPurchaseUpgrade(id);
            Refresh();
        }

        private void Refresh()
        {
            nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
            var localization = compositionRoot.LocalizationService;
            var snapshot = compositionRoot.ParkingEconomyUseCase.GetSnapshot();
            compactText.text = localization.Translate(
                new LocalizationKey("ui.economy.compact"),
                new Dictionary<string, object>
                {
                    ["gold"] = snapshot.Gold,
                    ["seconds"] = Mathf.CeilToInt((float)snapshot.SecondsUntilExpenses)
                });
            closeHintText.text = localization.Translate(new LocalizationKey("ui.economy.close_hint"));
            summaryText.text = localization.Translate(
                new LocalizationKey("ui.economy.summary"),
                new Dictionary<string, object>
                {
                    ["gold"] = snapshot.Gold,
                    ["salary"] = snapshot.DriverSalary,
                    ["tribute"] = snapshot.RoyalTribute,
                    ["seconds"] = Mathf.CeilToInt((float)snapshot.SecondsUntilExpenses)
                });

            if (snapshot.NoticeKey.HasValue)
            {
                feedbackText.text = localization.Translate(snapshot.NoticeKey.Value);
            }
            else if (snapshot.LastTransaction != null)
            {
                feedbackText.text = localization.Translate(
                    new LocalizationKey("ui.economy.last_operation"),
                    new Dictionary<string, object>
                    {
                        ["operation"] = localization.Translate(snapshot.LastTransaction.DescriptionKey),
                        ["amount"] = FormatSigned(snapshot.LastTransaction.SignedAmount)
                    });
            }
            else
            {
                feedbackText.text = localization.Translate(new LocalizationKey("ui.economy.ready"));
            }

            foreach (var upgrade in snapshot.Upgrades)
            {
                switch (upgrade.Id)
                {
                    case ParkingUpgradeId.CartCapacity:
                        RefreshUpgrade(
                            localization,
                            upgrade,
                            new LocalizationKey("economy.upgrade.cart_capacity"),
                            cartCapacityButton,
                            cartCapacityButtonText);
                        break;
                    case ParkingUpgradeId.CartSpeed:
                        RefreshUpgrade(
                            localization,
                            upgrade,
                            new LocalizationKey("economy.upgrade.cart_speed"),
                            cartSpeedButton,
                            cartSpeedButtonText);
                        break;
                    case ParkingUpgradeId.ConstructionSpeed:
                        RefreshUpgrade(
                            localization,
                            upgrade,
                            new LocalizationKey("economy.upgrade.construction_speed"),
                            constructionSpeedButton,
                            constructionSpeedButtonText);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void RefreshUpgrade(
            ILocalizationService localization,
            ParkingUpgradeSnapshot upgrade,
            LocalizationKey nameKey,
            Button button,
            Text label)
        {
            button.interactable = !upgrade.IsMaximumLevel;
            var name = localization.Translate(nameKey);
            var effect = GetEffectText(localization, upgrade);
            label.text = upgrade.IsMaximumLevel
                ? localization.Translate(
                    new LocalizationKey("ui.economy.upgrade.maximum"),
                    new Dictionary<string, object>
                    {
                        ["upgrade"] = name,
                        ["level"] = upgrade.Level,
                        ["effect"] = effect
                    })
                : localization.Translate(
                    new LocalizationKey("ui.economy.upgrade.buy"),
                    new Dictionary<string, object>
                    {
                        ["upgrade"] = name,
                        ["effect"] = effect,
                        ["level"] = upgrade.Level,
                        ["nextLevel"] = upgrade.Level + 1,
                        ["cost"] = upgrade.NextCost
                    });
        }

        private static string GetEffectText(
            ILocalizationService localization,
            ParkingUpgradeSnapshot upgrade)
        {
            var key = upgrade.Id == ParkingUpgradeId.CartCapacity
                ? new LocalizationKey("ui.economy.effect.capacity")
                : new LocalizationKey("ui.economy.effect.speed");
            var value = upgrade.Id == ParkingUpgradeId.CartCapacity
                ? Mathf.RoundToInt((float)upgrade.EffectPerLevel)
                : Mathf.RoundToInt((float)upgrade.EffectPerLevel * 100f);
            return localization.Translate(
                key,
                new Dictionary<string, object> { ["value"] = value });
        }

        private static string FormatSigned(int amount)
        {
            return amount > 0 ? "+" + amount : amount.ToString();
        }
    }
}
