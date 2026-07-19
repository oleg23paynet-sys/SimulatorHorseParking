#nullable enable

using System.Collections.Generic;
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
using HorseParking.Presentation.Localization;
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

        [SerializeField] private LogisticsInventorySettings? logisticsInventorySettings;
        [SerializeField] private GameLocalizationSettings? localizationSettings;

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

        public void ConfigureLogisticsInventory(LogisticsInventorySettings settings)
        {
            logisticsInventorySettings = settings;
        }

        public void ConfigureLocalization(GameLocalizationSettings settings)
        {
            localizationSettings = settings;
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
            interactWithTargetUseCase = new InteractWithTargetUseCase();
            var parkingSlot = new ParkingSlot("parking-slot-01");
            var tariff = new ParkingTariff(billingPeriodSeconds: 20d, goldPerPeriod: 3);
            parkingLifecycleUseCase = new ParkingLifecycleUseCase(parkingSlot, tariff, gameClock);
            if (logisticsInventorySettings != null)
            {
                logisticsInventorySettings.CreateUseCases(out logisticsInventoryUseCase, out cartJourneyUseCase);
            }
            else
            {
                logisticsInventoryUseCase = null;
                cartJourneyUseCase = null;
            }
        }
    }
}
