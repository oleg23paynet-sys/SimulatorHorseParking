using System.Linq;
using HorseParking.Core.Construction;
using HorseParking.Core.Logistics;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Parking;
using UnityEngine;
using UnityEngine.UI;
namespace HorseParking.Presentation.Onboarding
{
 public sealed class FirstPlayableObjectives : MonoBehaviour
 {
  [SerializeField] GameCompositionRoot root;
  [SerializeField] GameObject panel;
  [SerializeField] Text title, instruction, footer;
  [SerializeField] Image progress;
  float nextRefresh; int completedAtExpansion=-1;
  public bool SessionCompleted { get; private set; }
  public string CurrentGoal { get; private set; }
  public void Configure(GameCompositionRoot composition,GameObject view,Text heading,Text body,Text hint,Image fill)
  {root=composition;panel=view;title=heading;instruction=body;footer=hint;progress=fill;}
  string T(string key)=>root.LocalizationService.Translate(new LocalizationKey(key));
  void Update()
  {
   if(Input.GetKeyDown(KeyCode.F1))panel.SetActive(!panel.activeSelf);
   if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.25f;
   var clients=FindObjectsByType<ParkingMvpRuntimeController>(FindObjectsSortMode.None);
   int served=clients.Sum(c=>c.CompletedVisits);
   var build=root.ConstructionRequirementsUseCase.GetSnapshot();
   var inventory=root.LogisticsInventoryUseCase;var cargo=inventory.GetCartSnapshot();
   var journey=root.CartJourneyUseCase.GetSnapshot();
   int cost=0;string recipe="";
   foreach(var r in build.Requirements)
   {
    int carried=cargo.Items.Where(i=>i.ResourceId.Equals(r.ResourceId)).Sum(i=>i.Quantity);
    int missing=Mathf.Max(0,r.MissingQuantity-carried);
    int price=inventory.GetStoreOffers().First(o=>o.ResourceId.Equals(r.ResourceId)).PriceGold;
    cost+=missing*price;
    if(missing>0)recipe+=T("resource."+r.ResourceId.Value)+" ×"+missing+"  ";
   }
   int stage=0;string goal;
   if(build.State==ConstructionState.Completed)
   {
    if(completedAtExpansion<0)completedAtExpansion=served;
    SessionCompleted|=served-completedAtExpansion>=2;
    stage=SessionCompleted?6:5;goal=SessionCompleted?"complete":"two";
   }
   else if(build.State==ConstructionState.InProgress){goal="building";stage=4;}
   else if(build.CanStart){goal="build";stage=3;}
   else if(cargo.UsedCapacityUnits>0 && (journey.State==CartJourneyState.ReturningToWarehouse||journey.State==CartJourneyState.AtWarehouse))
   {goal=journey.State==CartJourneyState.AtWarehouse?"unload":"returning";stage=2;}
   else if(served==0){goal=clients.Any(c=>c.PaymentsCollected>0)?"exit":"first";}
   else if(inventory.Gold<cost){goal="earn";stage=1;}
   else if(journey.State==CartJourneyState.AtWarehouse){goal="dispatch";stage=2;}
   else if(journey.State==CartJourneyState.AtDestination){goal=cost==0?"return":"buy";stage=2;}
   else{goal="travel";stage=2;}
   CurrentGoal=goal;
   title.text=T("firstplay.title");
   instruction.text=T("firstplay."+goal).Replace("{cost}",cost.ToString()).Replace("{gold}",inventory.Gold.ToString()).Replace("{recipe}",recipe).Replace("{served}",Mathf.Max(0,served-completedAtExpansion).ToString());
   var parking=FindAnyObjectByType<ParkingExpansionPresenter>();
   if(goal=="build")instruction.text="Материалы доставлены. B — прозрачный макет стойла; WASD — камера, R — поворот, ЛКМ — подтвердить, Esc — отмена.";
   if(parking!=null&&parking.IsPlacing)instruction.text="Выберите ровное свободное место. Зелёный макет — можно строить; красный — место или подъезд заняты. Материалы списываются только после подтверждения.";
   footer.text=T("firstplay.footer");progress.fillAmount=stage/6f;
   if(parking==null||parking.Capacity==1)footer.text+=" • "+T("firstplay.grace");
  }
 }
}
