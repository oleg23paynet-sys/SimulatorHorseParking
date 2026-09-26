using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HorseParking.Presentation.Editor
{
    /// <summary>Transfers authored content from the supplied demo; never scatters new vegetation.</summary>
    public static class ParkingForestEnvironmentInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/ParkingMvp.unity";
        private const string SourceFolder = "Assets/_Project/Content/AssetStore/NatureManufacture Assets/Forest Environment Dynamic Nature/Demo Scenes/";
        private const string Output = "Assets/_Project/Content/Environment/AuthoredForest";
        private const string RootName = "ForestDemo_AuthoredEnvironment";
        private static readonly Vector2 ClearingCenter = new(145, 130);
        private const float SelectionRadius = 96;
        private static readonly Dictionary<Mesh, Mesh> EmbeddedMeshes = new();

        [MenuItem("Horse Parking/Apply Supplied Forest Demo Environment")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var gameScene = EditorSceneManager.OpenScene(ScenePath);
            var gameplay = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x != null && x.GetType().Namespace?.StartsWith("HorseParking") == true)
                .ToDictionary(x => x, x => EditorJsonUtility.ToJson(x));
            var transforms = gameScene.GetRootGameObjects().Where(x => x.name != RootName)
                .SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .Where(x => x.GetComponent<Terrain>() == null)
                .ToDictionary(x => x, x => (x.localPosition, x.localRotation, x.localScale));
            var terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null) throw new InvalidOperationException("Game terrain missing.");
            Directory.CreateDirectory(Output + "/Meshes");
            AssetDatabase.Refresh();
            EmbeddedMeshes.Clear();
            var previous = GameObject.Find(RootName);
            if (previous != null) Object.DestroyImmediate(previous);
            var sourceScene = EditorSceneManager.OpenScene(SourceFolder + "Forest Demo Scene.unity", OpenSceneMode.Additive);
            var sourceRoots = sourceScene.GetRootGameObjects();
            var sourceTerrain = sourceRoots.SelectMany(x => x.GetComponentsInChildren<Terrain>()).Single();
            float sourceHeight = sourceTerrain.SampleHeight(new Vector3(ClearingCenter.x, 0, ClearingCenter.y));
            var offset = new Vector3(-ClearingCenter.x, -sourceHeight, -ClearingCenter.y);
            SceneManager.SetActiveScene(gameScene);
            var root = new GameObject(RootName);
            root.transform.position = offset;
            CopyTerrain(sourceTerrain, terrain, sourceHeight, offset);
            int selected = 0;
            var groups = new HashSet<string> { "Details", "Scarps and slopes", "Stones and Rocks", "Roots and logs", "Manual Trees and Grass", "Small Architecture" };
            foreach (var sourceRoot in sourceRoots)
            {
                if (groups.Contains(sourceRoot.name))
                {
                    var group = new GameObject(sourceRoot.name);
                    group.transform.SetParent(root.transform, false);
                    group.transform.localPosition = sourceRoot.transform.position;
                    group.transform.localRotation = sourceRoot.transform.rotation;
                    group.transform.localScale = sourceRoot.transform.lossyScale;
                    foreach (Transform child in sourceRoot.transform)
                    {
                        if (!ShouldCopy(child.gameObject)) continue;
                        CopyObject(child.gameObject, group.transform);
                        selected++;
                    }
                }
                else if ((sourceRoot.name.StartsWith("RamSpline") || sourceRoot.name.StartsWith("Lake Polygon")) && ShouldCopy(sourceRoot))
                {
                    CopyObject(sourceRoot, root.transform);
                    selected++;
                }
                else if (sourceRoot.name == "Prefab_Wind") CopyObject(sourceRoot, root.transform);
            }
            EditorSceneManager.CloseScene(sourceScene, true);
            foreach (var item in gameplay)
                if (item.Key == null || EditorJsonUtility.ToJson(item.Key) != item.Value)
                    throw new InvalidOperationException("Gameplay component changed: " + item.Key);
            foreach (var item in transforms)
                if (item.Key == null || (item.Key.localPosition, item.Key.localRotation, item.Key.localScale) != item.Value)
                    throw new InvalidOperationException("Gameplay transform changed: " + item.Key);
            // Existing gameplay lighting remains configured; all authored materials are retained.
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            Debug.Log($"AUTHORED_FOREST_INSTALLED: {selected} source objects; {terrain.terrainData.treeInstanceCount} source tree instances; {terrain.terrainData.detailPrototypes.Length} source detail types; {gameplay.Count} gameplay components and {transforms.Count} transforms unchanged. Source clearing {ClearingCenter}, elevation {sourceHeight}.");
            RenderReview();
        }

        private static float ClearingDistance(float x, float z)
        {
            // Covers the existing buildings, construction site and horse/cart routes.
            return Mathf.Max(Mathf.Abs(x) - 12.5f, Mathf.Abs(z + 2.5f) - 17.5f);
        }

        private static bool ShouldCopy(GameObject source)
        {
            var renderers = source.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var center = new Vector3(ClearingCenter.x, bounds.center.y, ClearingCenter.y);
            if (Vector3.Distance(bounds.ClosestPoint(center), center) > SelectionRadius) return false;
            // Skip authored decorations intersecting the playable pad; keep every other authored transform.
            return bounds.max.x < ClearingCenter.x - 13.5f || bounds.min.x > ClearingCenter.x + 13.5f
                || bounds.max.z < ClearingCenter.y - 21 || bounds.min.z > ClearingCenter.y + 16;
        }

        private static void CopyTerrain(Terrain source, Terrain destination, float height, Vector3 offset)
        {
            string path = Output + "/ForestDemo_GameplayTerrain.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null)
            {
                data = Object.Instantiate(source.terrainData);
                AssetDatabase.CreateAsset(data, path);
            }
            else EditorUtility.CopySerialized(source.terrainData, data);
            data.name = "ForestDemo_GameplayTerrain";
            // The supplied AO/height/smoothness maps are not URP metallic/AO/height/smoothness masks.
            // In URP their bright AO channel makes the whole ground metallic and grey.
            // Keep the author's albedo, normals, tiling and painting; adapt only mask interpretation.
            data.terrainLayers = source.terrainData.terrainLayers.Select(CopyTerrainLayerForUrp).ToArray();
            int resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float wx = x / (float)(resolution - 1) * data.size.x - ClearingCenter.x;
                float wz = z / (float)(resolution - 1) * data.size.z - ClearingCenter.y;
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(ClearingDistance(wx, wz) / 4));
                if (blend > 0) heights[z, x] = Mathf.Lerp(heights[z, x], height / data.size.y, blend);
            }
            data.SetHeights(0, 0, heights);
            // Coordinates, scale, rotation and colors of retained instances come directly from the source.
            data.treeInstances = data.treeInstances.Where(tree =>
            {
                var world = Vector3.Scale(tree.position, data.size);
                float x = world.x - ClearingCenter.x, z = world.z - ClearingCenter.y;
                return ClearingDistance(x, z) > 4 && x * x + z * z < SelectionRadius * SelectionRadius;
            }).ToArray();
            for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
            {
                var density = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, layer);
                for (int z = 0; z < data.detailHeight; z++)
                for (int x = 0; x < data.detailWidth; x++)
                {
                    float wx = (x + 0.5f) / data.detailWidth * data.size.x - ClearingCenter.x;
                    float wz = (z + 0.5f) / data.detailHeight * data.size.z - ClearingCenter.y;
                    if (ClearingDistance(wx, wz) < 0.5f || wx * wx + wz * wz > SelectionRadius * SelectionRadius) density[z, x] = 0;
                }
                data.SetDetailLayer(0, 0, layer, density);
            }
            // Keep all source terrain layers/paint. Only the occupied pad needs a walkable surface.
            destination.terrainData = data;
            destination.GetComponent<TerrainCollider>().terrainData = data;
            destination.transform.position = offset;
            destination.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(SourceFolder + "URP Terrain.mat");
            destination.treeDistance = 150;
            destination.treeBillboardDistance = 110;
            destination.detailObjectDistance = 60;
            destination.detailObjectDensity = source.detailObjectDensity;
            destination.heightmapPixelError = source.heightmapPixelError;
            destination.Flush();
            EditorUtility.SetDirty(data);
        }

        private static void CopyObject(GameObject source, Transform parent)
        {
            var copy = Object.Instantiate(source, parent, false);
            copy.name = source.name;
            // Source demo-generated roads/water can contain meshes embedded in the source scene.
            foreach (var filter in copy.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)))
                    filter.sharedMesh = SaveEmbeddedMesh(filter.sharedMesh);
            foreach (var collider in copy.GetComponentsInChildren<MeshCollider>(true))
                if (collider.sharedMesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(collider.sharedMesh)))
                    collider.sharedMesh = SaveEmbeddedMesh(collider.sharedMesh);
            foreach (var transform in copy.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
                renderer.lightmapIndex = -1;
        }

        private static TerrainLayer CopyTerrainLayerForUrp(TerrainLayer source)
        {
            string path = Output + "/" + source.name + ".terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null) { layer = Object.Instantiate(source); AssetDatabase.CreateAsset(layer, path); }
            else EditorUtility.CopySerialized(source, layer);
            layer.maskMapTexture = null;
            layer.metallic = 0;
            layer.smoothness = 0.15f;
            var serialized = new SerializedObject(layer);
            var smoothnessSource = serialized.FindProperty("m_SmoothnessSource");
            if (smoothnessSource != null) { smoothnessSource.intValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Mesh SaveEmbeddedMesh(Mesh source)
        {
            if (EmbeddedMeshes.TryGetValue(source, out var existing)) return existing;
            string path = Output + "/Meshes/SourceMesh_" + EmbeddedMeshes.Count.ToString("D3") + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = Object.Instantiate(source); AssetDatabase.CreateAsset(mesh, path); }
            else EditorUtility.CopySerialized(source, mesh);
            EmbeddedMeshes.Add(source, mesh);
            return mesh;
        }

        public static void RenderReview()
        {
            Directory.CreateDirectory("Docs/ForestReview");
            Capture("authored-player", new Vector3(-3.5f, 1.8f, -12), new Vector3(0, 1.4f, 1));
            Capture("authored-overview", new Vector3(24, 24, -35), new Vector3(0, 1, 0));
            Capture("authored-forest", new Vector3(-10, 2, -12), new Vector3(-35, 3, -20));
        }

        private static void Capture(string name, Vector3 position, Vector3 target)
        {
            var camera = new GameObject("__ForestReview").AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 65;
            camera.farClipPlane = 250;
            camera.clearFlags = CameraClearFlags.Skybox;
            var render = new RenderTexture(1440, 900, 24);
            camera.targetTexture = render;
            camera.Render();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = render;
            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes("Docs/ForestReview/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = previous;
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(render);
            Object.DestroyImmediate(image);
        }
    }
}
