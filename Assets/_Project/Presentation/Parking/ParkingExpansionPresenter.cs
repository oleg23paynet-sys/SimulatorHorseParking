using System.Collections.Generic;
using System.Linq;
using HorseParking.Core.Construction;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Construction;
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
        private readonly List<ParkingMvpRuntimeController> addedSlots=new();
        private ParkingPlacementController placement;
        private ConstructionProcessPresenter construction;
        private bool completedConsumed;
        private bool hasPendingSite;
        private Vector3 pendingPosition;
        private Quaternion pendingRotation;
        public int Capacity=>1+addedSlots.Count;
        public int Occupied=>(firstSlot.OccupiesSpace?1:0)+addedSlots.Count(s=>s!=null&&s.OccupiesSpace);
        public IEnumerable<ParkingMvpRuntimeController> Slots=>new[]{firstSlot}.Concat(addedSlots);
        public bool IsPlacing=>placement!=null&&placement.IsPlacing;
        public void Configure(GameCompositionRoot root,ParkingMvpRuntimeController first,GameObject template,Text text)
        {compositionRoot=root;firstSlot=first;secondSlotTemplate=template;capacityText=text;}
        private void Start()
        {
            construction=FindAnyObjectByType<ConstructionProcessPresenter>();
            if(construction!=null)construction.EnableParkingSites();
            placement=gameObject.AddComponent<ParkingPlacementController>();
            placement.Configure(this,compositionRoot,secondSlotTemplate,construction);
        }
        public void BeginPlacement()
        {
            if(placement!=null)placement.BeginPlacement();
        }
        public bool PrepareProject()
        {
            if(hasPendingSite||construction==null||!construction.ReadyForNextSite)return false;
            var useCase=compositionRoot.ConstructionRequirementsUseCase;
            if(useCase.GetSnapshot().State==ConstructionState.Completed)
            {
                if(!completedConsumed)return false;
                useCase.TryRestoreProgress(ConstructionState.Planned,0);
                completedConsumed=false;
            }
            return useCase.GetSnapshot().State==ConstructionState.Planned;
        }
        public bool ConfirmSite(Vector3 position,Quaternion rotation,Vector3 siteCenter)
        {
            var useCase=compositionRoot.ConstructionRequirementsUseCase;
            if(hasPendingSite||!useCase.GetSnapshot().CanStart||!construction.ReadyForNextSite)return false;
            construction.MoveParkingSite(siteCenter,rotation);
            pendingPosition=position;pendingRotation=rotation;hasPendingSite=true;
            var result=useCase.TryStartConstruction();
            if(!result.Succeeded)hasPendingSite=false;
            return result.Succeeded;
        }
        private void Update()
        {
            if(!compositionRoot.HasConstructionRequirements)return;
            if(Input.GetKeyDown(KeyCode.B)&&!Cursor.visible)BeginPlacement();
            var state=compositionRoot.ConstructionRequirementsUseCase.GetSnapshot().State;
            if(state==ConstructionState.Completed&&!completedConsumed && (construction==null||construction.ReadyForNextSite))
            {
                var bay=Instantiate(secondSlotTemplate);
                bay.name="ParkingSlot_"+(Capacity+1)+"_Active";
                if(hasPendingSite)bay.transform.SetPositionAndRotation(pendingPosition,pendingRotation);
                var runtime=bay.GetComponentInChildren<ParkingMvpRuntimeController>(true);
                runtime.ConfigureAdditionalSlot("parking-slot-"+(Capacity+1));
                var dialogue=FindAnyObjectByType<ParkingClientDialoguePresenter>();
                if(dialogue!=null)runtime.ClientDialogueRequested+=dialogue.ShowDialogue;
                addedSlots.Add(runtime);bay.SetActive(true);
                completedConsumed=true;hasPendingSite=false;placement?.CompleteSite();
            }
            string action=Occupied>=Capacity?"Нет мест: заберите оплату и откройте выход, либо B — построить стойло.":"Свободное место ждёт всадника из очереди.";
            if(state==ConstructionState.InProgress)action="Стройка идёт: обслуживайте готовые стойла.";
            else if(hasPendingSite)action="Стойло готово: ждём, пока рабочие освободят площадку.";
            var queue=GetComponent<ParkingArrivalQueue>();
            capacityText.text=$"ПАРКОВКА {Occupied}/{Capacity}\n{action}";
            if(queue!=null)capacityText.text+=$"\nОчередь {queue.Waiting}/2 • Прибытие через {Mathf.CeilToInt(queue.NextArrivalSeconds)} с • Уехали: {queue.TurnedAway}";
        }
    }
}
