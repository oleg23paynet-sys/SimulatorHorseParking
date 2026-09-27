using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HorseParking.Core.Construction;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Construction;
using HorseParking.Presentation.Progress;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class GoblinEmergenceVerification
 {
  static double start; static bool triggered,underground,mid,late,finished; static int errors;
  static ConstructionWorkerMotionPresenter worker; static Camera camera;
  static object Read(string name) => typeof(ConstructionWorkerMotionPresenter).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(worker);
  public static void Run()
  {
   EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");
   foreach(var s in Object.FindObjectsByType<GameSavePresenter>(FindObjectsInactive.Include))Object.DestroyImmediate(s);
   SessionState.SetBool("VerifyGoblinEmergence",true); EditorApplication.isPlaying=true;
  }
  [InitializeOnLoadMethod] static void Resume()
  {
   if(!SessionState.GetBool("VerifyGoblinEmergence",false))return;
   start=EditorApplication.timeSinceStartup; UnityEngine.Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
   UnityEngine.Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};
   EditorApplication.update+=Tick;
  }
  static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
  static void Tick()
  {
   if(!EditorApplication.isPlaying||finished)return;
   try
   {
    var elapsed=EditorApplication.timeSinceStartup-start;
    if(elapsed<3)return;
    if(!triggered)
    {
     var root=Object.FindAnyObjectByType<GameCompositionRoot>();
     Require(root.ConstructionRequirementsUseCase.TryRestoreProgress(ConstructionState.InProgress,.05),"Start construction");
     worker=Object.FindObjectsByType<ConstructionWorkerMotionPresenter>(FindObjectsInactive.Include).First();
     triggered=true;
    }
    var phase=Read("phase").ToString();
    var visual=(Transform)Read("workerVisualRoot");
    var effect=(GameObject)Read("groundVortex");
    if(phase=="VortexLeadIn"&&!underground)
    {
     Require(visual.position.y<worker.transform.position.y-1f,"Worker must start below soil");
     underground=true; Capture("01-underground");
    }
    if(phase=="Emerging")
    {
     float progress=Mathf.InverseLerp((float)Read("phaseStartedAt"),(float)Read("phaseEndsAt"),Time.time);
     if(progress>.5f && !mid)
     {
      Require(visual.position.y<worker.transform.position.y-.2f,"Halfway model still intersects soil");
      Require(Quaternion.Angle(visual.localRotation,(Quaternion)Read("visualRestLocalRotation"))>5f,"Goblin spins while rising");
      var swirl=effect.GetComponentInChildren<ParticleSystem>();
      Require(swirl.GetComponent<ParticleSystemRenderer>().alignment==ParticleSystemRenderSpace.Local,"Swirl lies on soil instead of facing camera");
      var rocks=effect.GetComponentsInChildren<ParticleSystem>().Single(p=>p.name=="GroundDebris");
      Require(rocks.particleCount>0,"Rocks emitted throughout rise");
      Debug.Log("GOBLIN_MID particles="+rocks.particleCount+" visual="+visual.position+" surface="+effect.transform.position);
      mid=true; Capture("02-emerging");
     }
     if(progress>.85f&&!late){late=true;Capture("03-emerged");}
    }
    if(worker.HasReachedBuildPoint)
    {
     Require(underground&&mid&&late,"Full emergence observed");
     Require(effect.GetComponentsInChildren<ParticleSystem>().All(p=>!p.isEmitting),"Emission stops after emergence");
     Debug.Log("GOBLIN_VERIFY_OK: underground start, rising spin, ground swirl, flying rocks, navigation/build arrival."); Finish();
    }
    if(elapsed>45)throw new Exception("Worker did not reach building; phase="+phase);
   }
   catch(Exception e){errors++;Debug.LogException(e);Finish();}
  }
  static void Capture(string name)
  {
   if(camera==null){camera=new GameObject("Goblin review camera").AddComponent<Camera>();camera.transform.position=worker.transform.position+new Vector3(2.5f,1.7f,-3f);camera.transform.LookAt(worker.transform.position+Vector3.up*.35f);camera.fieldOfView=48;}
   var rt=new RenderTexture(1100,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
   var img=new Texture2D(1100,800,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1100,800),0,0);img.Apply();Directory.CreateDirectory("Docs/GoblinEmergence");File.WriteAllBytes("Docs/GoblinEmergence/"+name+".png",img.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(img);rt.Release();Object.DestroyImmediate(rt);
  }
  static void Finish(){finished=true;EditorApplication.update-=Tick;SessionState.SetBool("VerifyGoblinEmergence",false);EditorApplication.Exit(errors==0?0:1);}
 }
}
