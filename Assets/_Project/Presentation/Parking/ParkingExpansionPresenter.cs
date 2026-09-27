using HorseParking.Core.Construction;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;
namespace HorseParking.Presentation.Parking
{
    public sealed class ParkingExpansionPresenter : MonoBehaviour
    {
        [SerializeField] private GameCompositionRoot compositionRoot;
        [SerializeField] private ParkingMvpRuntimeController firstSlot;
        [SerializeField] private GameObject secondSlotTemplate;
        [SerializeField] private Text capacityText;
        private GameObject secondSlot;
        private ParkingMvpRuntimeController secondRuntime;
        public int Capacity => secondSlot != null ? 2 : 1;
        public int Occupied => (firstSlot.OccupiesSpace ? 1 : 0) + (secondRuntime != null && secondRuntime.OccupiesSpace ? 1 : 0);
        public void Configure(GameCompositionRoot root, ParkingMvpRuntimeController first, GameObject template, Text text)
        { compositionRoot=root; firstSlot=first; secondSlotTemplate=template; capacityText=text; }
        private void Update()
        {
            if (!compositionRoot.HasConstructionRequirements) return;
            bool built=compositionRoot.ConstructionRequirementsUseCase.GetSnapshot().State==ConstructionState.Completed;
            if (built && secondSlot==null)
            {
                secondSlot=Instantiate(secondSlotTemplate);
                secondSlot.name="ParkingSlot_02_Active";
                secondRuntime=secondSlot.GetComponentInChildren<ParkingMvpRuntimeController>(true);
                var dialogue=FindAnyObjectByType<ParkingClientDialoguePresenter>();
                if(dialogue!=null) secondRuntime.ClientDialogueRequested+=dialogue.ShowDialogue;
                secondSlot.SetActive(true);
            }
            else if (!built && secondSlot!=null)
            {
                // Loading an earlier save must revoke both the bay and its active visitor.
                secondRuntime.ReleaseDetachedRider();
                secondSlot.SetActive(false); Destroy(secondSlot); secondSlot=null; secondRuntime=null;
            }
            var key=Occupied>=Capacity ? (built ? "parking.capacity.full" : "parking.capacity.build") : "parking.capacity.available";
            capacityText.text=compositionRoot.LocalizationService.Translate(new LocalizationKey(key))
                .Replace("{used}",Occupied.ToString()).Replace("{capacity}",Capacity.ToString());
        }
    }
}
