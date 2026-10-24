using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class PlayableSettlementInstaller
 {
  const string Source="Assets/Assetstore/Advance Studios/Medieval Castle/Prefabs/";
  public static void Install()
  {
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");
   var logic=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&c.GetType().Namespace?.StartsWith("HorseParking")==true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
   Replace("MaterialStore_01","House A.prefab");
   Replace("Warehouse_01","House B.prefab");
   var previousCastle=GameObject.Find("Castle_Landmark"); if(previousCastle!=null)Object.DestroyImmediate(previousCastle);
   var castle=Spawn("Castle.prefab"); castle.name="Castle_Landmark";
   var bounds=BoundsOf(castle);castle.transform.localScale*=24f/Mathf.Max(bounds.size.x,bounds.size.z);
   bounds=BoundsOf(castle);
   var terrain=Object.FindAnyObjectByType<Terrain>();
   var center=new Vector3(34,0,26);float ground=terrain.SampleHeight(center)+terrain.transform.position.y;
   castle.transform.position+=new Vector3(center.x-bounds.center.x,ground-bounds.min.y,center.z-bounds.center.z);
   var pad=BoundsOf(castle);pad.Expand(new Vector3(14,0,14));
   ClearPad(terrain,pad,ground);
   foreach(var filter in castle.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
   foreach(var pair in logic)if(pair.Key==null||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Gameplay configuration changed: "+pair.Key);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
   Debug.Log("SETTLEMENT_OK gameplay="+logic.Count+" castleBounds="+BoundsOf(castle));
   ParkingForestEnvironmentInstaller.RenderReview();
   Capture("castle",center+new Vector3(0,15,-38),center+Vector3.up*5);
  }
  static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new Exception("No visible meshes: "+go.name);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
  static GameObject Spawn(string asset)
  {
   var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source+asset));
   foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
   foreach(var r in go.GetComponentsInChildren<Renderer>(true))foreach(var mat in r.sharedMaterials)if(mat==null||mat.shader==null||!mat.shader.isSupported)throw new Exception("Unsupported material in "+asset);
   return go;
  }
  static void Replace(string name,string asset)
  {
   var target=GameObject.Find(name);if(target==null)throw new Exception("Missing "+name);
   if(target.transform.Find("ImportedBuildingVisual")!=null)return;
   var b=BoundsOf(target);
   var model=Spawn(asset);var m=BoundsOf(model);
   float scale=Mathf.Min(b.size.x/m.size.x,b.size.z/m.size.z,5f/m.size.y);
   model.transform.localScale*=scale;m=BoundsOf(model);
   model.transform.position+=new Vector3(b.center.x-m.center.x,b.min.y-m.min.y,b.center.z-m.center.z);
   foreach(var r in target.GetComponentsInChildren<Renderer>(true))r.enabled=false;
   model.name="ImportedBuildingVisual";model.transform.SetParent(target.transform,true);
   Debug.Log("REPLACED "+name+" with "+asset+" bounds="+BoundsOf(model));
  }
  static void ClearPad(Terrain terrain,Bounds pad,float ground)
  {
   var data=terrain.terrainData;var origin=terrain.transform.position;var size=data.size;
   var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
   for(int z=0;z<data.heightmapResolution;z++)for(int x=0;x<data.heightmapResolution;x++)
   {
    float wx=origin.x+x*size.x/(data.heightmapResolution-1),wz=origin.z+z*size.z/(data.heightmapResolution-1);
    float outside=Mathf.Max(pad.min.x-wx,wx-pad.max.x,pad.min.z-wz,wz-pad.max.z);
    if(outside<3)heights[z,x]=Mathf.Lerp(heights[z,x],(ground-origin.y)/size.y,1-Mathf.SmoothStep(0,1,Mathf.Max(0,outside)/3));
   }
   data.SetHeights(0,0,heights);
   bool Inside(Vector3 p)=>p.x>pad.min.x-2&&p.x<pad.max.x+2&&p.z>pad.min.z-2&&p.z<pad.max.z+2;
   data.treeInstances=data.treeInstances.Where(t=>!Inside(origin+Vector3.Scale(t.position,size))).ToArray();
   for(int layer=0;layer<data.detailPrototypes.Length;layer++)
   {var density=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer);for(int z=0;z<data.detailHeight;z++)for(int x=0;x<data.detailWidth;x++)if(Inside(origin+new Vector3(x*size.x/data.detailWidth,0,z*size.z/data.detailHeight)))density[z,x]=0;data.SetDetailLayer(0,0,layer,density);}
   var forest=GameObject.Find("ForestDemo_AuthoredEnvironment");
   if(forest!=null)foreach(var r in forest.GetComponentsInChildren<Renderer>())if(Inside(r.bounds.center)){var lod=r.GetComponentInParent<LODGroup>();if(lod!=null)lod.gameObject.SetActive(false);else r.enabled=false;foreach(var c in r.GetComponents<Collider>())c.enabled=false;}
   EditorUtility.SetDirty(data);
  }
  static void Capture(string name,Vector3 position,Vector3 target)
  {
   var cam=new GameObject("SettlementReview").AddComponent<Camera>();cam.transform.position=position;cam.transform.LookAt(target);cam.farClipPlane=300;
   var rt=new RenderTexture(1440,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();Directory.CreateDirectory("Docs/SettlementReview");File.WriteAllBytes("Docs/SettlementReview/"+name+".png",image.EncodeToPNG());RenderTexture.active=null;Object.DestroyImmediate(cam.gameObject);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
  }
 }
}
