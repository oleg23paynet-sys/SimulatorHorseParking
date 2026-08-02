#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Construction;
using HorseParking.Application.Economy;
using HorseParking.Application.Interaction;
using HorseParking.Application.Logistics;
using HorseParking.Application.Parking;
using HorseParking.Core.Localization;
using HorseParking.Core.Parking;
using HorseParking.Core.Randomness;
using HorseParking.Core.Time;
using HorseParking.Infrastructure.Localization;
using HorseParking.Infrastructure.Randomness;
using HorseParking.Infrastructure.Time;
using HorseParking.Presentation.Logistics;
using HorseParking.Presentation.Construction;
using HorseParking.Presentation.Economy;
using HorseParking.Presentation.Localization;
using HorseParking.Presentation.Parking;
using UnityEngine;

namespace HorseParking.Presentation.Composition
{
    /// <summary>
    /// Single Composition Root: creates concrete services and injects them into Presentation systems.
    /// Attach to the Bootstrap scene when that scene is created.
    /// </summary>
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        private ILocalizationService localizationService = null!;
        private IGameClock gameClock = null!;
        private IRandomSource randomSource = null!;
        private InteractWithTargetUseCase interactWithTargetUseCase = null!;
        private ParkingLifecycleUseCase parkingLifecycleUseCase = null!;
        private LogisticsInventoryUseCase? logisticsInventoryUseCase;
        private CartJourneyUseCase? cartJourneyUseCase;
        private ConstructionRequirementsUseCase? constructionRequirementsUseCase;
        private ParkingEconomyUseCase? parkingEconomyUseCase;
        private ParkingClientArchetypeSelectionUseCase? parkingClientArchetypeSelectionUseCase;
        private ParkingClientDialogueUseCase? parkingClientDialogueUseCase;

        [SerializeField] private LogisticsInventorySettings? logisticsInventorySettings;
        [SerializeField] private GameLocalizationSettings? localizationSettings;
        [SerializeField] private ConstructionRequirementsSettings? constructionRequirementsSettings;
        [SerializeField] private ParkingEconomySettings? parkingEconomySettings;
        [SerializeField] private ParkingClientArchetypeSettings? parkingClientArchetypeSettings;
        [SerializeField] private ParkingClientDialogueSettings? parkingClientDialogueSettings;

        public ILocalizationService LocalizationService => localizationService;

        public IGameClock GameClock => gameClock;

        public IRandomSource RandomSource => randomSource;

        public InteractWithTargetUseCase InteractWithTargetUseCase => interactWithTargetUseCase;

        /// <summary>Injected application boundary for the single-slot parking MVP.</summary>
        public ParkingLifecycleUseCase ParkingLifecycleUseCase => parkingLifecycleUseCase;

        public bool HasLogisticsInventory => logisticsInventoryUseCase != null;

        /// <summary>Injected application boundary for the Stage 3 warehouse and starter cart.</summary>
        public LogisticsInventoryUseCase LogisticsInventoryUseCase => logisticsInventoryUseCase
            ?? throw new System.InvalidOperationException("LogisticsInventorySettings is not assigned to the Composition Root.");

        public bool HasCartJourney => cartJourneyUseCase != null;

        public CartJourneyUseCase CartJourneyUseCase => cartJourneyUseCase
            ?? throw new System.InvalidOperationException("Cart journey services are not configured in the Composition Root.");

        public bool HasConstructionRequirements => constructionRequirementsUseCase != null;

        public ConstructionRequirementsUseCase ConstructionRequirementsUseCase => constructionRequirementsUseCase
            ?? throw new System.InvalidOperationException("Construction requirements are not configured in the Composition Root.");

        public bool HasParkingEconomy => parkingEconomyUseCase != null;

        public ParkingEconomyUseCase ParkingEconomyUseCase => parkingEconomyUseCase
            ?? throw new System.InvalidOperationException("Parking economy is not configured in the Composition Root.");

        public bool HasParkingClientArchetypes => parkingClientArchetypeSelectionUseCase != null;

        public ParkingClientArchetypeSelectionUseCase ParkingClientArchetypeSelectionUseCase =>
            parkingClientArchetypeSelectionUseCase
            ?? throw new System.InvalidOperationException("Parking client archetypes are not configured.");

        public double ClientRespawnDelaySeconds => parkingClientArchetypeSettings?.DelayBetweenClientsSeconds ?? 3d;

        public bool HasParkingClientDialogue => parkingClientDialogueUseCase != null;

        public ParkingClientDialogueUseCase ParkingClientDialogueUseCase =>
            parkingClientDialogueUseCase
            ?? throw new System.InvalidOperationException("Parking client dialogue is not configured.");

        public void ConfigureLogisticsInventory(LogisticsInventorySettings settings)
        {
            logisticsInventorySettings = settings;
        }

        public void ConfigureLocalization(GameLocalizationSettings settings)
        {
            localizationSettings = settings;
        }

        public void ConfigureConstructionRequirements(ConstructionRequirementsSettings settings)
        {
            constructionRequirementsSettings = settings;
        }

        public void ConfigureParkingEconomy(ParkingEconomySettings settings)
        {
            parkingEconomySettings = settings;
        }

        public void ConfigureParkingClientArchetypes(ParkingClientArchetypeSettings settings)
        {
            parkingClientArchetypeSettings = settings;
        }

        public void ConfigureParkingClientDialogue(ParkingClientDialogueSettings settings)
        {
            parkingClientDialogueSettings = settings;
        }

        private void Awake()
        {
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            localizationService = localizationSettings != null
                ? localizationSettings.CreateService()
                : new DictionaryLocalizationService("en", new Dictionary<string, string>());
            gameClock = new StopwatchGameClock();
            randomSource = new SeededRandomSource(12345);
            parkingClientArchetypeSelectionUseCase = parkingClientArchetypeSettings != null
                ? parkingClientArchetypeSettings.CreateUseCase(randomSource)
                : null;
            parkingClientDialogueUseCase = parkingClientDialogueSettings != null
                ? parkingClientDialogueSettings.CreateUseCase(randomSource)
                : null;
            interactWithTargetUseCase = new InteractWithTargetUseCase();
            var parkingSlot = new ParkingSlot("parking-slot-01");
            var tariff = parkingEconomySettings != null
                ? new ParkingTariff(
                    parkingEconomySettings.BillingPeriodSeconds,
                    parkingEconomySettings.GoldPerBillingPeriod)
                : new ParkingTariff(billingPeriodSeconds: 20d, goldPerPeriod: 3);
            parkingLifecycleUseCase = new ParkingLifecycleUseCase(parkingSlot, tariff, gameClock);
            if (logisticsInventorySettings != null)
            {
                logisticsInventorySettings.CreateUseCases(out logisticsInventoryUseCase, out cartJourneyUseCase);
                constructionRequirementsUseCase = constructionRequirementsSettings != null
                    ? constructionRequirementsSettings.CreateUseCase(logisticsInventoryUseCase)
                    : null;
                parkingEconomyUseCase = parkingEconomySettings != null
                    ? parkingEconomySettings.CreateUseCase(logisticsInventoryUseCase)
                    : null;
            }
            else
            {
                logisticsInventoryUseCase = null;
                cartJourneyUseCase = null;
                constructionRequirementsUseCase = null;
                parkingEconomyUseCase = null;
            }
        }
    }
}
