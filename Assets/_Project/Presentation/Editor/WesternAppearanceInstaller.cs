using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class WesternAppearanceInstaller
 {
  const string Folder="Assets/_Project/Content/Materials/Architecture/";
  public static void Install()
  {
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");
   var before=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MonoBehaviour>(true)).Where(x=>x!=null && x.GetType().Namespace?.StartsWith("HorseParking")==true).ToDictionary(x=>x,x=>EditorJsonUtility.ToJson(x));
   var original=AssetDatabase.LoadAssetAtPath<Material>(Folder+"hexagons_medieval_Weathered.mat");
   var store=Style(original,"Western_Store",new Color(.83f,.77f,.61f),new Color(.50f,.43f,.30f),new Color(.24f,.38f,.37f));
   var warehouse=Style(original,"Western_Lumber",new Color(.53f,.65f,.58f),new Color(.25f,.38f,.31f),new Color(.32f,.36f,.39f));
   var stable=Style(original,"Western_Stable",new Color(.68f,.34f,.26f),new Color(.43f,.17f,.12f),new Color(.36f,.39f,.39f));
   var fence=Style(original,"Western_Fence",new Color(.40f,.39f,.33f),new Color(.69f,.65f,.54f),new Color(.36f,.39f,.39f));
   fence.SetTexture("_BaseMap",Texture2D.whiteTexture);
   var porch=Style(original,"Western_PorchWood",new Color(.26f,.22f,.16f),new Color(.26f,.22f,.16f),new Color(.32f,.36f,.39f));
   porch.SetTexture("_BaseMap",Texture2D.whiteTexture);
   original.SetColor("_PaintColor",new Color(.79f,.73f,.6f)); original.SetColor("_TimberColor",new Color(.69f,.6f,.44f)); original.SetColor("_RoofColor",new Color(.31f,.39f,.4f)); EditorUtility.SetDirty(original);
   var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   foreach(var t in all)
   {
    if(t==null)continue;
    if(t.name=="MaterialStore_01") {Assign(t.gameObject,store); Facade(t.gameObject,"GENERAL STORE",porch,new Color(.16f,.27f,.25f),2.55f);}
    if(t.name=="Warehouse_01") {Assign(t.gameObject,warehouse); Facade(t.gameObject,"LUMBER & SUPPLY",porch,new Color(.31f,.15f,.12f),2.65f);}
    if(t.name.StartsWith("ParkingSlot_01_") && t.name.EndsWith("Fence")) Fence(t.gameObject,fence);
   }
   foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
   {
    var filter=renderer.GetComponent<MeshFilter>();
    if(filter!=null && AssetDatabase.GetAssetPath(filter.sharedMesh).Contains("building_archeryrange")) Assign(renderer.gameObject,stable);
   }
   foreach(var pair in before) if(pair.Key==null || EditorJsonUtility.ToJson(pair.Key)!=pair.Value) throw new Exception("Gameplay changed: "+pair.Key);
   AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
   Debug.Log("WESTERN_APPEARANCE_OK: gameplay components preserved="+before.Count);
   ParkingForestEnvironmentInstaller.RenderReview();
  }
  static Material Style(Material source,string name,Color paint,Color timber,Color roof)
  {
   string path=Folder+name+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(material==null) {material=new Material(source);AssetDatabase.CreateAsset(material,path);}
   material.SetColor("_PaintColor",paint); material.SetColor("_TimberColor",timber);material.SetColor("_RoofColor",roof);material.SetColor("_StoneColor",new Color(.59f,.61f,.59f));EditorUtility.SetDirty(material);return material;
  }
  static void Assign(GameObject go,Material material)
  {
   foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true))
   {
    var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]!=null && mats[i].shader.name=="HorseParking/WeatheredArchitecture")mats[i]=material;r.sharedMaterials=mats;
   }
  }
  static GameObject Piece(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
  {
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.position=position;go.transform.localScale=scale;go.transform.SetParent(parent,true);go.GetComponent<Renderer>().sharedMaterial=material;return go;
  }
  static Material Flat(string name,Color color)
  {
   string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(material);return material;
  }
  static void Facade(GameObject building,string label,Material wood,Color boardColor,float height)
  {
   var old=building.transform.Find("WesternFacade");if(old!=null)Object.DestroyImmediate(old.gameObject);
   var renderers=building.GetComponentsInChildren<Renderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   var decor=new GameObject("WesternFacade");decor.transform.SetParent(building.transform,false);
   float z=b.min.z-.12f, y=b.min.y+height, x=b.center.x;
   var board=Flat("Western_Sign_"+building.name,boardColor);
   Piece(decor.transform,"Painted sign",new Vector3(x,y,z),new Vector3(3.15f,.64f,.14f),board);
   Piece(decor.transform,"Upper trim",new Vector3(x,y+.37f,z),new Vector3(3.4f,.12f,.23f),wood);
   Piece(decor.transform,"Lower trim",new Vector3(x,y-.37f,z),new Vector3(3.4f,.1f,.23f),wood);
   for(int side=-1;side<=1;side+=2)Piece(decor.transform,"Porch post",new Vector3(x+side*1.58f,b.min.y+height*.5f,z+.05f),new Vector3(.14f,height,.14f),wood);
   for(int i=0;i<14;i++)Piece(decor.transform,"Porch plank",new Vector3(x-1.6f+i*.245f,b.min.y+.045f,z-.4f),new Vector3(.23f,.09f,.95f),wood);
   var sign=new GameObject("Sign lettering",typeof(TextMesh));sign.transform.position=new Vector3(x,y-.015f,z-.081f);sign.transform.SetParent(decor.transform,true);sign.transform.rotation=Quaternion.identity;
   var text=sign.GetComponent<TextMesh>();text.text=label;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=.085f;text.color=new Color(.96f,.9f,.7f);
   var lettering=sign.GetComponent<Renderer>().bounds.size;
   float fit=Mathf.Min(2.85f/Mathf.Max(lettering.x,.001f),.4f/Mathf.Max(lettering.y,.001f));
   sign.transform.localScale*=fit;
   // Keep all new pieces visual-only: the existing interaction and movement colliders remain authoritative.
  }
  static void Fence(GameObject go,Material material)
  {
   var old=go.transform.Find("WesternRails");if(old!=null)Object.DestroyImmediate(old.gameObject);
   var renderers=go.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0)return;
   var bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.enabled=false;}
   var root=new GameObject("WesternRails");root.transform.SetParent(go.transform,false);
   bool alongX=bounds.size.x>bounds.size.z;float length=alongX?bounds.size.x:bounds.size.z;
   float height=Mathf.Min(bounds.size.y,1.45f);var start=bounds.center;start.y=bounds.min.y;
   int posts=Mathf.Max(2,Mathf.CeilToInt(length/1.4f)+1);
   for(int i=0;i<posts;i++){var p=start+(alongX?Vector3.right:Vector3.forward)*(-length*.5f+i*length/(posts-1));p.y+=height*.5f;Piece(root.transform,"Square post",p,new Vector3(.17f,height,.17f),material);}
   foreach(float y in new[]{.35f,.75f,1.12f}){var p=start+Vector3.up*Mathf.Min(y,height*.82f);Piece(root.transform,"Split rail",p,alongX?new Vector3(length,.13f,.09f):new Vector3(.09f,.13f,length),material);}
  }
 }
}
