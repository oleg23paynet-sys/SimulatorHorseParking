#nullable enable

using System;
using System.Collections.Generic;
using HorseParking.Application.Construction;
using HorseParking.Application.Economy;
using HorseParking.Application.Logistics;
using HorseParking.Application.Onboarding;
using HorseParking.Core.Construction;
using HorseParking.Core.Logistics;

namespace HorseParking.Application.Progress
{
    public enum GameProgressFailureReason
    {
        None = 0,
        NoSave = 1,
        InvalidOrUnsupportedSave = 2,
        StorageError = 3
    }

    public readonly struct GameProgressOperationResult
    {
        private GameProgressOperationResult(GameProgressFailureReason failureReason)
        {
            FailureReason = failureReason;
        }

        public bool Succeeded => FailureReason == GameProgressFailureReason.None;
        public GameProgressFailureReason FailureReason { get; }
        public static GameProgressOperationResult Success() => new(GameProgressFailureReason.None);
        public static GameProgressOperationResult Failure(GameProgressFailureReason reason) => new(reason);
    }

    /// <summary>
    /// Coordinates a single persistent snapshot. Transient actors and route animation
    /// are deliberately normalized to safe cart endpoints on save.
    /// </summary>
    public sealed class GameProgressUseCase
    {
        private readonly IGameProgressRepository repository;
        private readonly LogisticsInventoryUseCase logistics;
        private readonly CartJourneyUseCase cartJourney;
        private readonly ConstructionRequirementsUseCase construction;
        private readonly ParkingEconomyUseCase economy;
        private readonly TutorialFlowUseCase tutorial;

        public GameProgressUseCase(
            IGameProgressRepository repository,
            LogisticsInventoryUseCase logistics,
            CartJourneyUseCase cartJourney,
            ConstructionRequirementsUseCase construction,
            ParkingEconomyUseCase economy,
            TutorialFlowUseCase tutorial)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.logistics = logistics ?? throw new ArgumentNullException(nameof(logistics));
            this.cartJourney = cartJourney ?? throw new ArgumentNullException(nameof(cartJourney));
            this.construction = construction ?? throw new ArgumentNullException(nameof(construction));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
        }

        public bool HasSave => repository.Exists;

        public GameProgressOperationResult Save()
        {
            try
            {
                repository.Save(Capture());
                return GameProgressOperationResult.Success();
            }
            catch (Exception)
            {
                return GameProgressOperationResult.Failure(GameProgressFailureReason.StorageError);
            }
        }

        public GameProgressOperationResult Load()
        {
            if (!repository.Exists)
                return GameProgressOperationResult.Failure(GameProgressFailureReason.NoSave);

            try
            {
                if (!repository.TryLoad(out var progress)
                    || progress == null
                    || progress.Version != GameProgressData.CurrentVersion
                    || !TryPrepareInventory(progress.Warehouse, out var warehouse)
                    || !TryPrepareInventory(progress.Cart, out var cart)
                    || !TryPrepareUpgrades(progress.Upgrades, out var upgrades)
                    || !Enum.IsDefined(typeof(ConstructionState), progress.ConstructionState)
                    || !Enum.IsDefined(typeof(CartJourneyState), progress.CartJourneyState)
                    || !Enum.IsDefined(typeof(TutorialStep), progress.TutorialStep))
                {
                    return GameProgressOperationResult.Failure(GameProgressFailureReason.InvalidOrUnsupportedSave);
                }

                var constructionState = (ConstructionState)progress.ConstructionState;
                var cartState = (CartJourneyState)progress.CartJourneyState;
                var tutorialStep = (TutorialStep)progress.TutorialStep;
                if (!logistics.CanRestoreProgress(
                        progress.Gold,
                        progress.Warehouse.CapacityUnits,
                        warehouse,
                        progress.Cart.CapacityUnits,
                        cart)
                    || !economy.CanRestoreProgress(upgrades, progress.SecondsUntilExpenses)
                    || !construction.CanRestoreProgress(constructionState, progress.ConstructionProgress)
                    || !cartJourney.CanRestoreStableState(cartState, progress.CartDestinationId)
                    || !tutorial.CanRestore(tutorialStep))
                {
                    return GameProgressOperationResult.Failure(GameProgressFailureReason.InvalidOrUnsupportedSave);
                }

                logistics.TryRestoreProgress(
                    progress.Gold,
                    progress.Warehouse.CapacityUnits,
                    warehouse,
                    progress.Cart.CapacityUnits,
                    cart);
                economy.TryRestoreProgress(upgrades, progress.SecondsUntilExpenses);
                construction.TryRestoreProgress(constructionState, progress.ConstructionProgress);
                cartJourney.TryRestoreStableState(cartState, progress.CartDestinationId);
                tutorial.TryRestore(tutorialStep);
                return GameProgressOperationResult.Success();
            }
            catch (Exception)
            {
                return GameProgressOperationResult.Failure(GameProgressFailureReason.StorageError);
            }
        }

        private GameProgressData Capture()
        {
            var warehouse = logistics.GetWarehouseSnapshot();
            var cart = logistics.GetCartSnapshot();
            var economySnapshot = economy.GetSnapshot();
            var constructionSnapshot = construction.GetSnapshot();
            var journey = cartJourney.GetSnapshot();
            var stableCartState = journey.State == CartJourneyState.AtDestination
                                  || journey.State == CartJourneyState.TravelingToDestination
                ? CartJourneyState.AtDestination
                : CartJourneyState.AtWarehouse;

            var data = new GameProgressData
            {
                SavedAtUtcTicks = DateTime.UtcNow.Ticks,
                Gold = logistics.Gold,
                Warehouse = CaptureInventory(warehouse),
                Cart = CaptureInventory(cart),
                SecondsUntilExpenses = economySnapshot.SecondsUntilExpenses,
                ConstructionState = (int)constructionSnapshot.State,
                ConstructionProgress = constructionSnapshot.NormalizedProgress,
                CartJourneyState = (int)stableCartState,
                CartDestinationId = stableCartState == CartJourneyState.AtDestination
                    ? journey.DestinationId
                    : null,
                TutorialStep = (int)tutorial.CurrentStep
            };

            foreach (var upgrade in economySnapshot.Upgrades)
            {
                data.Upgrades.Add(new UpgradeLevelProgress
                {
                    UpgradeId = (int)upgrade.Id,
                    Level = upgrade.Level
                });
            }

            return data;
        }

        private static InventoryProgress CaptureInventory(InventorySnapshot snapshot)
        {
            var progress = new InventoryProgress { CapacityUnits = snapshot.CapacityUnits };
            foreach (var item in snapshot.Items)
            {
                if (item.Quantity <= 0) continue;
                progress.Items.Add(new ResourceQuantityProgress
                {
                    ResourceId = item.ResourceId.Value,
                    Quantity = item.Quantity
                });
            }

            return progress;
        }

        private static bool TryPrepareInventory(
            InventoryProgress? progress,
            out IReadOnlyDictionary<ResourceId, int> contents)
        {
            var prepared = new Dictionary<ResourceId, int>();
            contents = prepared;
            if (progress == null || progress.CapacityUnits <= 0 || progress.Items == null) return false;

            foreach (var item in progress.Items)
            {
                if (item == null || item.Quantity < 0 || string.IsNullOrWhiteSpace(item.ResourceId)) return false;
                var id = new ResourceId(item.ResourceId);
                if (!prepared.TryAdd(id, item.Quantity)) return false;
            }

            return true;
        }

        private static bool TryPrepareUpgrades(
            IReadOnlyList<UpgradeLevelProgress>? progress,
            out IReadOnlyDictionary<ParkingUpgradeId, int> levels)
        {
            var prepared = new Dictionary<ParkingUpgradeId, int>();
            levels = prepared;
            if (progress == null) return false;
            foreach (var item in progress)
            {
                if (item == null
                    || !Enum.IsDefined(typeof(ParkingUpgradeId), item.UpgradeId)
                    || item.Level < 0)
                {
                    return false;
                }

                if (!prepared.TryAdd((ParkingUpgradeId)item.UpgradeId, item.Level)) return false;
            }

            return true;
        }
    }
}
