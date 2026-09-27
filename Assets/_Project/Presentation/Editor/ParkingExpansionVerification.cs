using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HorseParking.Core.Construction;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Parking;
using HorseParking.Presentation.Progress;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class ParkingExpansionVerification
 {
  static double start; static int stage,errors; static HashSet<ParkingMvpRuntimeController> paid=new(),exited=new();
  static bool parkedTogether; static bool rollback; static bool restored;
  public static void Run()
  {
   EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");
   foreach(var s in Object.FindObjectsByType<GameSavePresenter>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Object.DestroyImmediate(s);
   SessionState.SetBool("VerifyParkingExpansion",true); EditorApplication.isPlaying=true;
  }
  [InitializeOnLoadMethod] static void Resume()
  {
   if(!SessionState.GetBool("VerifyParkingExpansion",false))return;
   start=EditorApplication.timeSinceStartup; UnityEngine.Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
   UnityEngine.Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error || t==LogType.Exception)errors++;};
   EditorApplication.update+=Tick;
  }
  static void Tick()
  {
   if(!EditorApplication.isPlaying)return;
   try
   {
    var elapsed=EditorApplication.timeSinceStartup-start;
    var root=Object.FindFirstObjectByType<GameCompositionRoot>(); var manager=Object.FindFirstObjectByType<ParkingExpansionPresenter>();
    if(root==null || manager==null || elapsed<3)return;
    if(stage==0) {Require(manager.Capacity==1,"Initial capacity"); root.ConstructionRequirementsUseCase.TryRestoreProgress(ConstructionState.Completed,1);stage=1;}
    var clients=Object.FindObjectsByType<ParkingMvpRuntimeController>(FindObjectsSortMode.None);
    if(stage==1 && elapsed>6) {Require(manager.Capacity==2 && clients.Length==2,"Construction unlocks exactly two independent clients");stage=2;}
    if(stage==2)
    {
     if(clients.Count(c=>c.CanTalkToClient)==2) parkedTogether=true;
     foreach(var c in clients) {if(c.CanCollectPayment && c.TryCollectPayment())paid.Add(c); if(c.CanOpenExit)c.TryOpenExit(); if(!c.OccupiesSpace && paid.Contains(c))exited.Add(c);}
     if(elapsed>60 && paid.Count==2 && exited.Count==2)
     { root.ConstructionRequirementsUseCase.TryRestoreProgress(ConstructionState.Planned,0); stage=3; }
     if(elapsed>110) throw new Exception("Both visitors did not complete payment/exit. paid="+paid.Count+" exited="+exited.Count);
    }
    else if(stage==3 && manager.Capacity==1)
    {
     Require(clients.Length==1,"Rollback removes extra visitor"); rollback=true;
     root.ConstructionRequirementsUseCase.TryRestoreProgress(ConstructionState.Completed,1);stage=4;
    }
    else if(stage==4 && manager.Capacity==2 && clients.Length==2)
    { restored=true; Require(parkedTogether,"Concurrent parking"); Capture(); Finish(); }
   }
   catch(Exception e){errors++;Debug.LogException(e);Finish();}
  }
  static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
  static void Capture()
  {
   var camera=new GameObject("Review").AddComponent<Camera>();camera.transform.position=new Vector3(-24,12,-25);camera.transform.LookAt(new Vector3(-7,1,-1));camera.fieldOfView=65;
   var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
   var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();Directory.CreateDirectory("Docs/ParkingExpansion");File.WriteAllBytes("Docs/ParkingExpansion/TwoStalls.png",image.EncodeToPNG());RenderTexture.active=null;
  }
  static void Finish()
  {
   EditorApplication.update-=Tick;SessionState.SetBool("VerifyParkingExpansion",false);
   Debug.Log($"EXPANSION_VERIFY errors={errors}, simultaneous={parkedTogether}, paid={paid.Count}, exited={exited.Count}, rollback={rollback}, restore={restored}");EditorApplication.Exit(errors==0?0:1);
  }
 }
}

