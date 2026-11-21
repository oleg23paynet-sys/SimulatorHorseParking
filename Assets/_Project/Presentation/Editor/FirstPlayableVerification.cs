using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HorseParking.Core.Construction;
using HorseParking.Core.Logistics;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Parking;
using HorseParking.Presentation.Construction;
using HorseParking.Presentation.Onboarding;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class FirstPlayableVerification
 {
  static double start,finishedAt;static int stage,errors;static bool full,two;static int admittedAtPayment;
  public static void Run(){EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");SessionState.SetBool("FirstPlayableVerify",true);EditorApplication.isPlaying=true;}
  public static void Preview(){SessionState.SetBool("FirstPlayablePreview",true);Run();}
  [InitializeOnLoadMethod] static void Resume()
  {
   if(!SessionState.GetBool("FirstPlayableVerify",false))return;
   start=EditorApplication.timeSinceStartup;UnityEngine.Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.timeScale=3;
   UnityEngine.Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};EditorApplication.update+=Tick;
  }
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static void Tick()
  {
   if(!EditorApplication.isPlaying)return;
   try
   {
    double elapsed=EditorApplication.timeSinceStartup-start;if(elapsed<3)return;
    if(SessionState.GetBool("FirstPlayablePreview",false)){Capture();Finish();return;}
    if(stage==7){if(EditorApplication.timeSinceStartup>finishedAt+2)Finish();return;}
    var root=Object.FindAnyObjectByType<GameCompositionRoot>();var queue=Object.FindAnyObjectByType<ParkingArrivalQueue>();var bays=Object.FindAnyObjectByType<ParkingExpansionPresenter>();var goals=Object.FindAnyObjectByType<FirstPlayableObjectives>();
    var clients=Object.FindObjectsByType<ParkingMvpRuntimeController>();var inventory=root.LogisticsInventoryUseCase;var build=root.ConstructionRequirementsUseCase;
    Check(bays.Occupied<=bays.Capacity,"Never overbook parking");Check(queue.Waiting<=2,"Bounded entrance queue");
    if(stage==0)
    {
     Check(inventory.Gold==10&&!root.HasGameProgress,"Clean start with 10 gold and no persistence");
     Check(!build.TryStartConstruction().Succeeded,"Cannot build without materials");stage=1;
    }
    if(stage==1 && queue.TurnedAway>0)
    {
     Check(queue.Waiting==2&&bays.Occupied==1,"Queue fills while first bay occupied");Check(inventory.Gold==10,"No early recurring expenses");
     var c=clients[0];Check(c.TryCollectPayment(),"First payment available");Check(!c.TryCollectPayment(),"No duplicate payment");
     Check(bays.Occupied==1,"Paid horse still reserves bay");admittedAtPayment=queue.Admitted;full=true;stage=2;
    }
    if(stage>=2)
    {
     foreach(var client in clients){if(client.CanCollectPayment)client.TryCollectPayment();if(client.CanOpenExit)client.TryOpenExit();}
    }
    if(stage==2 && inventory.Gold>=22 && clients.Sum(c=>c.CompletedVisits)>=1)
    {
     Check(queue.Admitted>admittedAtPayment,"Waiting visitor admitted after departure");
     Check(root.CartJourneyUseCase.TryDispatch("material-store").Succeeded,"Send cart to store");stage=3;
    }
    var journey=root.CartJourneyUseCase.GetSnapshot().State;
    if(stage==3&&journey==CartJourneyState.AtDestination)
    {
     foreach(var requirement in build.GetSnapshot().Requirements)Check(inventory.TryPurchaseForCart(requirement.ResourceId,requirement.RequiredQuantity).Succeeded,"Buy exact recipe using earned gold");
     Check(!build.GetSnapshot().CanStart,"Cargo is not warehouse stock");Check(root.CartJourneyUseCase.TryBeginReturn().Succeeded,"Return delivery");stage=4;
    }
    if(stage==4&&journey==CartJourneyState.AtWarehouse)
    {
     foreach(var requirement in build.GetSnapshot().Requirements)Check(inventory.TryUnloadCart(requirement.ResourceId,requirement.RequiredQuantity).Succeeded,"Unload delivered resources");
     var panel=Object.FindAnyObjectByType<ConstructionRequirementsPanel>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
     typeof(ConstructionRequirementsPanel).GetMethod("Open",flags).Invoke(panel,null);
     var button=(Button)typeof(ConstructionRequirementsPanel).GetField("startButton",flags).GetValue(panel);
     Check(button.interactable,"Construction button ready without selection trap");button.onClick.Invoke();
     Check(build.GetSnapshot().State==ConstructionState.InProgress,"UI starts construction");Check(!build.TryStartConstruction().Succeeded,"Duplicate start rejected");stage=5;
    }
    if(stage==5&&bays.Capacity==2){stage=6;Debug.Log("FIRST_PLAYABLE: second bay built through earned money and real cart travel.");}
    if(stage==6)
    {
     two|=clients.Count(c=>c.OccupiesSpace)==2;
     if(goals.SessionCompleted)
     {
      Check(two&&full,"Concurrent parking and full queue exercised");Capture();Debug.Log("FIRST_PLAYABLE_VERIFY_OK fullQueue="+full+" twoBays="+two+" served="+clients.Sum(c=>c.CompletedVisits)+" errors="+errors);stage=7;finishedAt=EditorApplication.timeSinceStartup;
     }
    }
    if(elapsed>200)throw new Exception("Timed out: stage="+stage+" goal="+goals.CurrentGoal+" journey="+journey);
   }catch(Exception e){errors++;Debug.LogException(e);Finish();}
  }
  static void Capture()
  {
   Directory.CreateDirectory("Docs/FirstPlayable");
   var camera=Object.FindObjectsByType<Camera>().First(c=>c.enabled&&c.cameraType==CameraType.Game);var rt=new RenderTexture(1440,900,24);var previous=camera.targetTexture;
   var overlays=Object.FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   var states=overlays.Select(c=>(c.worldCamera,c.planeDistance)).ToArray();
   for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceCamera;overlays[i].worldCamera=camera;overlays[i].planeDistance=.5f+i*.001f;}
   camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
   var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Docs/FirstPlayable/review.png",image.EncodeToPNG());
   RenderTexture.active=null;camera.targetTexture=previous;for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=states[i].worldCamera;overlays[i].planeDistance=states[i].planeDistance;}
   Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
  }
  static void Finish(){EditorApplication.update-=Tick;SessionState.SetBool("FirstPlayableVerify",false);SessionState.SetBool("FirstPlayablePreview",false);Time.timeScale=1;EditorApplication.Exit(errors==0?0:1);}
 }
}
