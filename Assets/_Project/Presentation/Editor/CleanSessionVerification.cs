using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HorseParking.Core.Construction;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Parking;
using HorseParking.Presentation.Progress;
using HorseParking.Presentation.Construction;
using UnityEngine.UI;
using System.Reflection;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class CleanSessionVerification
 {
  static double start;static int stage,errors;static string saveBefore;
  static string SavePath=>Path.Combine(UnityEngine.Application.persistentDataPath,"Saves","parking-slot-01.json");
  static string Fingerprint()=>File.Exists(SavePath)?Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(SavePath))):"missing";
  [MenuItem("Horse Parking/Verify Construction Panel %#F8")]
  public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before verification.");EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");SessionState.SetBool("VerifyCleanSession",true);SessionState.SetString("SaveFingerprint",Fingerprint());EditorApplication.isPlaying=true;}
  [InitializeOnLoadMethod] static void Resume(){if(!SessionState.GetBool("VerifyCleanSession",false))return;saveBefore=SessionState.GetString("SaveFingerprint","");start=EditorApplication.timeSinceStartup;UnityEngine.Application.targetFrameRate=60;QualitySettings.vSyncCount=0;UnityEngine.Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};EditorApplication.update+=Tick;}
  static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
  static void Tick()
  {
   if(!EditorApplication.isPlaying)return;
   try
   {
    var elapsed=EditorApplication.timeSinceStartup-start;if(elapsed<3)return;
    var root=Object.FindAnyObjectByType<GameCompositionRoot>();var expansion=Object.FindAnyObjectByType<ParkingExpansionPresenter>();
    if(stage==0 && root.LogisticsInventoryUseCase.Gold<22)
    {
     foreach(var client in Object.FindObjectsByType<ParkingMvpRuntimeController>())
     {if(client.CanCollectPayment)client.TryCollectPayment();if(client.CanOpenExit)client.TryOpenExit();}
     if(elapsed>180)throw new Exception("Unable to earn construction budget");
     return;
    }
    if(stage==0)
    {
     Require(!root.HasGameProgress,"No persistence repository in prototype mode");
     Require(Object.FindObjectsByType<GameSavePresenter>(FindObjectsInactive.Include).All(s=>!s.enabled),"Save/load UI disabled");
     Require(root.ConstructionRequirementsUseCase.GetSnapshot().State==ConstructionState.Planned&&expansion.Capacity==1,"Clean initial construction and parking");
     Require(GameObject.Find("Castle_Landmark")!=null,"Castle present");
     foreach(var name in new[]{"MaterialStore_01","Warehouse_01"})Require(GameObject.Find(name).transform.Find("ImportedBuildingVisual")!=null,"Imported building "+name);
     var build=root.ConstructionRequirementsUseCase;var inventory=root.LogisticsInventoryUseCase;
     var panel=Object.FindAnyObjectByType<ConstructionRequirementsPanel>();
     var flags=BindingFlags.NonPublic|BindingFlags.Instance;
     void PanelAction(string method)=>typeof(ConstructionRequirementsPanel).GetMethod(method,flags).Invoke(panel,null);
     var startButton=(Button)typeof(ConstructionRequirementsPanel).GetField("startButton",flags).GetValue(panel);
     PanelAction("Open");
     Require(!startButton.interactable,"Start disabled before delivery");
     Require(!build.TryStartConstruction().Succeeded,"Cannot build without delivered resources");
     foreach(var requirement in build.GetSnapshot().Requirements)
     {Require(inventory.TryPurchaseForCart(requirement.ResourceId,requirement.RequiredQuantity).Succeeded,"Purchase construction materials");Require(inventory.TryUnloadCart(requirement.ResourceId,requirement.RequiredQuantity).Succeeded,"Unload resources");}
     Require(startButton.interactable,"Delivery enables start without extra project selection");
     PanelAction("Close");PanelAction("Open");
     Require(startButton.interactable,"Reopening keeps sole project ready to build");
     startButton.onClick.Invoke();
     FirstPlayableVerification.ConfirmPlacementForTest();
     Require(build.GetSnapshot().State==ConstructionState.InProgress,"Panel start button starts construction");
     Require(!build.TryStartConstruction().Succeeded,"Duplicate start rejected");stage=1;
    }
    if(stage==1&&root.ConstructionRequirementsUseCase.GetSnapshot().State==ConstructionState.Completed&&expansion.Capacity==2)
    {
     Require(Fingerprint()==saveBefore,"Old save untouched");Debug.Log("CLEAN_SESSION_VERIFY_OK: fresh start, persistence disabled, selected buildings, resource purchase/unload, natural worker construction, second parking slot, old save unchanged.");Finish();
    }
    if(elapsed>240)throw new Exception("Construction timeout: "+root.ConstructionRequirementsUseCase.GetSnapshot().State);
   }catch(Exception e){errors++;Debug.LogException(e);Finish();}
  }
  static void Finish(){EditorApplication.update-=Tick;SessionState.SetBool("VerifyCleanSession",false);Debug.Log("CONSTRUCTION_PANEL_VERIFY errors="+errors);if(UnityEngine.Application.isBatchMode)EditorApplication.Exit(errors==0?0:1);else EditorApplication.isPlaying=false;}
 }
}
