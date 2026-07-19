#nullable enable

using System.Collections.Generic;
using HorseParking.Application.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Logistics
{
    public sealed class CartDispatchPanel : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private FirstPersonPlayerController playerController = null!;
        [SerializeField] private CartDispatchInteractionTarget warehouseTarget = null!;
        [SerializeField] private CartDispatchInteractionTarget storeTarget = null!;
        [SerializeField] private GameObject panelRoot = null!;
        [SerializeField] private Text titleText = null!;
        [SerializeField] private Text stationText = null!;
        [SerializeField] private Text statusText = null!;
        [SerializeField] private Text instructionText = null!;
        [SerializeField] private Button actionButton = null!;
        [SerializeField] private Text actionButtonText = null!;
        [SerializeField] private Button closeButton = null!;
        [SerializeField] private Text closeButtonText = null!;
        [SerializeField] private string materialStoreId = "material-store";

        private CartJourneyUseCase journeyUseCase = null!;
        private CartStationKind currentStation;
        private bool isOpen;

        public void Configure(
            GameCompositionRoot root,
            FirstPersonPlayerController player,
            CartDispatchInteractionTarget warehouse,
            CartDispatchInteractionTarget store,
            GameObject panel,
            Text title,
            Text station,
            Text status,
            Text instruction,
            Button action,
            Text actionLabel,
            Button close,
            Text closeLabel,
            string destinationId)
        {
            compositionRoot = root;
            playerController = player;
            warehouseTarget = warehouse;
            storeTarget = store;
            panelRoot = panel;
            titleText = title;
            stationText = station;
            statusText = status;
            instructionText = instruction;
            actionButton = action;
            actionButtonText = actionLabel;
            closeButton = close;
            closeButtonText = closeLabel;
            materialStoreId = destinationId;
        }

        private void Start()
        {
            if (compositionRoot == null || !compositionRoot.HasCartJourney || playerController == null
                || warehouseTarget == null || storeTarget == null || panelRoot == null || instructionText == null)
            {
                Debug.LogError("Cart dispatch panel is not configured.", this);
                enabled = false;
                return;
            }

            journeyUseCase = compositionRoot.CartJourneyUseCase;
            warehouseTarget.InteractionRequested += Open;
            storeTarget.InteractionRequested += Open;
            actionButton.onClick.AddListener(ExecuteAction);
            closeButton.onClick.AddListener(Close);
            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (warehouseTarget != null) warehouseTarget.InteractionRequested -= Open;
            if (storeTarget != null) storeTarget.InteractionRequested -= Open;
        }

        private void Update()
        {
            if (!isOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            Refresh();
        }

        private void Open(CartStationKind station)
        {
            currentStation = station;
            isOpen = true;
            panelRoot.SetActive(true);
            playerController.SetUiInputBlocked(true);
            Refresh();
        }

        private void Close()
        {
            isOpen = false;
            panelRoot.SetActive(false);
            playerController.SetUiInputBlocked(false);
        }

        private void ExecuteAction()
        {
            var state = journeyUseCase.GetSnapshot().State;
            CartJourneyResult result;
            if (currentStation == CartStationKind.Warehouse && state == CartJourneyState.AtWarehouse)
            {
                result = journeyUseCase.TryDispatch(materialStoreId);
            }
            else if (currentStation == CartStationKind.MaterialStore && state == CartJourneyState.AtDestination)
            {
                result = journeyUseCase.TryBeginReturn();
            }
            else
            {
                return;
            }

            if (result.Succeeded) Close();
            else Refresh();
        }

        private void Refresh()
        {
            var localization = compositionRoot.LocalizationService;
            var snapshot = journeyUseCase.GetSnapshot();
            titleText.text = localization.Translate(new LocalizationKey("ui.cart_dispatch.title"));
            closeButtonText.text = localization.Translate(new LocalizationKey("ui.common.close"));
            var stationName = localization.Translate(new LocalizationKey(
                currentStation == CartStationKind.Warehouse ? "location.warehouse" : "location.material_store"));
            stationText.text = localization.Translate(
                new LocalizationKey("ui.cart_dispatch.current_station"),
                new Dictionary<string, object> { ["station"] = stationName });
            var stateName = localization.Translate(CartJourneyLocalization.GetStateKey(snapshot.State));
            statusText.text = localization.Translate(
                new LocalizationKey("ui.cart.status"),
                new Dictionary<string, object> { ["state"] = stateName });

            var canDispatch = currentStation == CartStationKind.Warehouse
                && snapshot.State == CartJourneyState.AtWarehouse;
            var canReturn = currentStation == CartStationKind.MaterialStore
                && snapshot.State == CartJourneyState.AtDestination;
            actionButton.gameObject.SetActive(canDispatch || canReturn);
            instructionText.text = localization.Translate(new LocalizationKey(
                GetInstructionKey(snapshot.State, canDispatch, canReturn)));
            if (canDispatch)
            {
                actionButtonText.text = localization.Translate(new LocalizationKey("ui.cart_dispatch.send_to_store"));
            }
            else if (canReturn)
            {
                actionButtonText.text = localization.Translate(new LocalizationKey("ui.cart_dispatch.return_to_warehouse"));
            }
        }

        private static string GetInstructionKey(
            CartJourneyState state,
            bool canDispatch,
            bool canReturn)
        {
            if (canDispatch) return "ui.cart_dispatch.instruction.ready_warehouse";
            if (canReturn) return "ui.cart_dispatch.instruction.ready_store";
            if (state == CartJourneyState.TravelingToDestination
                || state == CartJourneyState.ReturningToWarehouse)
            {
                return "ui.cart_dispatch.instruction.in_transit";
            }

            return state == CartJourneyState.AtWarehouse
                ? "ui.cart_dispatch.instruction.go_warehouse"
                : "ui.cart_dispatch.instruction.go_store";
        }
    }
}
