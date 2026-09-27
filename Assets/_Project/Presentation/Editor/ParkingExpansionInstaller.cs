using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Localization;
using HorseParking.Presentation.Parking;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class ParkingExpansionInstaller
 {
  const string ScenePath="Assets/_Project/Scenes/ParkingMvp.unity";
  const string Materials="Assets/_Project/Content/Materials/Architecture/";
  public static void Install()
  {
   var scene=EditorSceneManager.OpenScene(ScenePath);
   if(GameObject.Find("ParkingExpansion")) throw new InvalidOperationException("Expansion already installed.");
   var first=Object.FindFirstObjectByType<ParkingMvpRuntimeController>();
   var root=Object.FindFirstObjectByType<GameCompositionRoot>();
   var sources=scene.GetRootGameObjects().Where(g=>g.name=="ParkingMvpRuntime" || g.name=="ClientMountedHorseRider_01" || g.name=="PaymentSack_01" || g.name=="ExitGate_01" || g.name.StartsWith("ParkingSlot_01_") || g.name.StartsWith("ClientEntryLanePoint_") || g.name.StartsWith("ClientParkingPoint_") || g.name.StartsWith("ClientPaymentPoint_") || g.name.StartsWith("ClientExitPoint_") || g.name.StartsWith("RiderMountPoint_") || g.name.StartsWith("RiderOpenLanePoint_") || g.name.StartsWith("RiderAwayPoint_")).ToArray();
   if(sources.Length<12) throw new InvalidOperationException("Incomplete bay source: "+sources.Length);
   var staging=new GameObject("CloneSource");
   foreach(var source in sources) source.transform.SetParent(staging.transform,true);
   var template=Object.Instantiate(staging);
   foreach(var source in sources) source.transform.SetParent(null,true);
   Object.DestroyImmediate(staging);
   template.name="ParkingSlot_02_Template";
   template.transform.position=new Vector3(-14,0,0);
   var second=template.GetComponentInChildren<ParkingMvpRuntimeController>(true);
   second.ConfigureAdditionalSlot("parking-slot-02");
   foreach(var target in template.GetComponentsInChildren<ParkingClientInteractionTarget>(true)) target.Configure(second);
   foreach(var renderer in template.GetComponentsInChildren<SkinnedMeshRenderer>(true))
   {
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++)
    {
     var source=materials[i];
     if(source==null || !(source.name.Contains("HorseBody") || source.name=="M_HAP_Rider")) continue;
     string path=Materials+source.name+"_SecondVisitor.mat";
     var material=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(material==null) {material=new Material(source); AssetDatabase.CreateAsset(material,path);}
     material.SetColor("_BaseColor",source.name.Contains("HorseBody") ? new Color(0.55f,0.48f,0.42f) : new Color(0.66f,0.78f,0.67f));
     materials[i]=material;
    }
    renderer.sharedMaterials=materials;
   }
   template.SetActive(false);
   var canvas=new GameObject("ParkingCapacityHUD",typeof(Canvas),typeof(CanvasScaler));
   canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   canvas.GetComponent<Canvas>().sortingOrder=5;
   var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);
   var panel=new GameObject("Capacity",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(canvas.transform,false);
   var rect=panel.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(0.5f,1); rect.pivot=new Vector2(0.5f,1); rect.anchoredPosition=new Vector2(0,-110); rect.sizeDelta=new Vector2(830,70);
   panel.GetComponent<Image>().color=new Color(0.07f,0.09f,0.07f,0.9f); panel.GetComponent<Image>().raycastTarget=false;
   var textObject=new GameObject("Status",typeof(RectTransform),typeof(Text)); textObject.transform.SetParent(panel.transform,false);
   var tr=textObject.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=new Vector2(14,5); tr.offsetMax=new Vector2(-14,-5);
   var text=textObject.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=23; text.alignment=TextAnchor.MiddleCenter; text.color=new Color(1,0.87f,0.61f); text.raycastTarget=false;
   var expansion=new GameObject("ParkingExpansion").AddComponent<ParkingExpansionPresenter>(); expansion.Configure(root,first,template,text);
   var localization=AssetDatabase.LoadAssetAtPath<GameLocalizationSettings>("Assets/_Project/Settings/GameLocalizationSettings.asset");
   localization.EnsureTranslation("ru","parking.capacity.build","МЕСТ НЕТ · {used}/{capacity}\nПостройте новое стойло: подойдите к строительной табличке и нажмите E.");
   localization.EnsureTranslation("ru","parking.capacity.full","МЕСТ НЕТ · {used}/{capacity}\nЗаберите оплату и откройте шлагбаум, чтобы освободить стойло.");
   localization.EnsureTranslation("ru","parking.capacity.available","СТОЙЛА · занято {used}/{capacity} — принимаем наездников");
   localization.EnsureTranslation("en","parking.capacity.build","NO VACANCIES · {used}/{capacity}\nBuild another stall: approach the construction sign and press E.");
   localization.EnsureTranslation("en","parking.capacity.full","NO VACANCIES · {used}/{capacity}\nCollect payment and open the gate to free a stall.");
   localization.EnsureTranslation("en","parking.capacity.available","STALLS · occupied {used}/{capacity} — riders welcome");
   EditorUtility.SetDirty(localization);
   PrepareSecondBayGround();
   RestyleArchitecture(scene.GetRootGameObjects());
   RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=0.006f; RenderSettings.fogColor=new Color(0.53f,0.60f,0.55f);
   foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if(light.type==LightType.Directional) {light.shadows=LightShadows.Soft; light.shadowStrength=0.8f; light.color=new Color(1,0.94f,0.83f);}
   AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
   Debug.Log("EXPANSION_INSTALL_OK: independent second bay; construction unlock; capacity HUD; textured architecture.");
   ParkingForestEnvironmentInstaller.RenderReview();
  }
  static void PrepareSecondBayGround()
  {
   var terrain=Object.FindFirstObjectByType<Terrain>(); var data=terrain.terrainData; int n=data.heightmapResolution;
   var heights=data.GetHeights(0,0,n,n);
   for(int z=0;z<n;z++) for(int x=0;x<n;x++)
   {
    float wx=terrain.transform.position.x+x/(float)(n-1)*data.size.x, wz=terrain.transform.position.z+z/(float)(n-1)*data.size.z;
    float distance=Mathf.Max(Mathf.Abs(wx+14)-6,Mathf.Abs(wz+3)-18);
    float blend=1-Mathf.Clamp01(distance/3); if(blend>0) heights[z,x]=Mathf.Lerp(heights[z,x],-terrain.transform.position.y/data.size.y,blend);
   }
   data.SetHeights(0,0,heights);
   bool InBay(float x,float z)=>x>-23 && x<-7 && z>-25 && z<18;
   data.treeInstances=data.treeInstances.Where(t=>{var p=terrain.transform.position+Vector3.Scale(t.position,data.size);return !InBay(p.x,p.z);}).ToArray();
   for(int layer=0;layer<data.detailPrototypes.Length;layer++) {var values=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer); for(int z=0;z<data.detailHeight;z++) for(int x=0;x<data.detailWidth;x++) if(InBay(terrain.transform.position.x+x/(float)data.detailWidth*data.size.x,terrain.transform.position.z+z/(float)data.detailHeight*data.size.z)) values[z,x]=0; data.SetDetailLayer(0,0,layer,values);}
   var forest=GameObject.Find("ForestDemo_AuthoredEnvironment");
   if(forest!=null) foreach(Transform group in forest.transform) foreach(var child in group.Cast<Transform>().ToArray())
   {
    var renderers=child.GetComponentsInChildren<Renderer>(true); if(renderers.Length==0) continue;
    var b=renderers[0].bounds; foreach(var r in renderers) b.Encapsulate(r.bounds);
    if(b.max.x>-23 && b.min.x<-7 && b.max.z>-25 && b.min.z<18) Object.DestroyImmediate(child.gameObject);
   }
   EditorUtility.SetDirty(data); terrain.Flush();
  }
  static void RestyleArchitecture(GameObject[] roots)
  {
   var wood=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Content/Vehicles/DeliveryCart/Runtime/HandPushCart/textures/Wood035_2K-JPG_Color.jpg");
   var stone=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Content/AssetStore/NatureManufacture Assets/Forest Environment Dynamic Nature/Ground/Textures/T_ground_beech_forest_stones_01_BC_SM.tga");
   var shader=Shader.Find("HorseParking/WeatheredArchitecture"); if(shader==null || wood==null || stone==null) throw new InvalidOperationException("Architecture assets missing.");
   int count=0;
   foreach(var renderer in roots.SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)))
   {
    var filter=renderer.GetComponent<MeshFilter>(); if(filter==null || !AssetDatabase.GetAssetPath(filter.sharedMesh).Contains("KayKitMedievalHexagon")) continue;
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++)
    {
     var source=materials[i]; if(source==null || source.name.Contains("Ghost")) continue;
     string path=Materials+source.name+"_Weathered.mat";
     var material=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(material==null) {material=new Material(shader); material.SetTexture("_BaseMap",source.mainTexture); material.SetTexture("_Wood",wood); material.SetTexture("_Stone",stone); AssetDatabase.CreateAsset(material,path);}
     materials[i]=material; count++;
    }
    renderer.sharedMaterials=materials;
   }
   Debug.Log("ARCHITECTURE_TEXTURED render slots="+count);
  }
 }
}
