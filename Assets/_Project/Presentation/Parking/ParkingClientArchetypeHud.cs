#nullable enable

using System.Collections.Generic;
using HorseParking.Core.Localization;
using HorseParking.Core.Parking;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Parking
{
    [DisallowMultipleComponent]
    public sealed class ParkingClientArchetypeHud : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private ParkingMvpRuntimeController runtimeController = null!;
        [SerializeField] private Text profileText = null!;

        public void Configure(
            GameCompositionRoot root,
            ParkingMvpRuntimeController runtime,
            Text text)
        {
            compositionRoot = root;
            runtimeController = runtime;
            profileText = text;
        }

        private void Start()
        {
            if (compositionRoot == null || runtimeController == null || profileText == null)
            {
                Debug.LogError("Client archetype HUD is not configured.", this);
                enabled = false;
                return;
            }

            runtimeController.ClientArchetypeChanged += Refresh;
            if (runtimeController.CurrentArchetype != null)
            {
                Refresh(runtimeController.CurrentArchetype);
            }
        }

        private void OnDestroy()
        {
            if (runtimeController != null)
            {
                runtimeController.ClientArchetypeChanged -= Refresh;
            }
        }

        private void Refresh(ParkingClientArchetype archetype)
        {
            var localization = compositionRoot.LocalizationService;
            profileText.text = localization.Translate(
                new LocalizationKey("ui.client_archetype.summary"),
                new Dictionary<string, object>
                {
                    ["name"] = localization.Translate(archetype.NameKey),
                    ["description"] = localization.Translate(archetype.DescriptionKey),
                    ["seconds"] = Mathf.RoundToInt((float)archetype.ParkingDurationSeconds),
                    ["gold"] = archetype.Tariff.GoldPerPeriod,
                    ["period"] = Mathf.RoundToInt((float)archetype.Tariff.BillingPeriodSeconds)
                });
        }
    }
}
