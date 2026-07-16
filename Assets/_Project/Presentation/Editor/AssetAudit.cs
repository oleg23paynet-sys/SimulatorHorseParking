using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HorseParking.Presentation.Editor
{
    public static class AssetAudit
    {
        public static void AuditSeatedIdle()
        {
            const string animationPath = "Assets/_Project/Content/Animations/Characters/Rider/MedievalRider/SeatedIdle_Source.fbx";
            var importer = AssetImporter.GetAtPath(animationPath) as ModelImporter;
            var clips = importer.defaultClipAnimations;
            Debug.Log("SEATED_AUDIT|defaultClipCount=" + clips.Length + "|allAssetCount=" + AssetDatabase.LoadAllAssetsAtPath(animationPath).Length);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(animationPath))
            {
                Debug.Log("SEATED_AUDIT|asset=" + asset.GetType().Name + "|" + asset.name);
            }
        }

        public static void AuditMountedClientMaterials()
        {
            const string modelPath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/SM_RedHorseRider.fbx";
            foreach (var material in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
            {
                var textureName = material.mainTexture == null ? "none" : material.mainTexture.name;
                Debug.Log("MOUNTED_MATERIAL|" + material.name + "|shader=" + material.shader.name + "|texture=" + textureName);
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                Debug.Log("MOUNTED_RENDERER|" + renderer.name + "|materials=" + string.Join(",", renderer.sharedMaterials.Select(material => material.name)));
            }
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static readonly string[] Paths =
        {
            "Assets/_Project/ThirdParty/HorseAnimsetPro/Horse Realistic.FBX",
            "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Rider.FBX",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_market_blue.fbx",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_lumbermill_blue.fbx",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/neutral/fence_wood_straight_gate.fbx",
            "Assets/_Project/Content/Models/Props/PaymentPouch/SM_PaymentPouch.fbx"
        };

        public static void Run()
        {
            foreach (var path in Paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    Debug.LogError("AUDIT|MISSING|" + path);
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogError("AUDIT|NO_RENDERER|" + path);
                    UnityEngine.Object.DestroyImmediate(instance);
                    continue;
                }

                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }

                var materialCount = 0;
                foreach (var renderer in renderers)
                {
                    materialCount += renderer.sharedMaterials.Length;
                }

                Debug.Log(string.Format(
                    "AUDIT|{0}|size={1:F3},{2:F3},{3:F3}|center={4:F3},{5:F3},{6:F3}|renderers={7}|materials={8}",
                    path,
                    bounds.size.x, bounds.size.y, bounds.size.z,
                    bounds.center.x, bounds.center.y, bounds.center.z,
                    renderers.Length,
                    materialCount));
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
