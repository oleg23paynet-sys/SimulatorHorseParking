using System;
using UnityEditor;
using UnityEngine;

namespace HorseParking.Presentation.Editor
{
    public static class AssetAudit
    {
        private static readonly string[] Paths =
        {
            "Assets/_Project/Content/Models/Environment/ParkingGround/CobblestoneLowpoly/cobblestone.fbx",
            "Assets/_Project/Content/Models/Characters/Horse/LowPolyHorse/uploads_files_2798555_Horse.fbx",
            "Assets/_Project/Content/Models/Characters/Rider/X Bot.fbx",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_market_blue.fbx",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_lumbermill_blue.fbx",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/neutral/fence_wood_straight_gate.fbx",
            "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/decoration/props/sack.fbx"
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
