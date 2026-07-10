using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorseParking.Presentation.Editor
{
    public static class ParkingMvpSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/ParkingMvp.unity";
        private const string GroundPath = "Assets/_Project/Content/Models/Environment/ParkingGround/CobblestoneLowpoly/cobblestone.fbx";
        private const string HorsePath = "Assets/_Project/Content/Models/Characters/Horse/LowPolyHorse/uploads_files_2798555_Horse.fbx";
        private const string RiderPath = "Assets/_Project/Content/Models/Characters/Rider/X Bot.fbx";
        private const string StorePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_market_blue.fbx";
        private const string WarehousePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_lumbermill_blue.fbx";
        private const string GatePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/neutral/fence_wood_straight_gate.fbx";
        private const string SackPath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/decoration/props/sack.fbx";

        [MenuItem("Horse Parking/Build Parking MVP Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting();
            var compositionRoot = new GameObject("GameCompositionRoot").AddComponent<GameCompositionRoot>();
            CreatePlayer(compositionRoot);
            CreateGround();
            CreateParkingZone();
            CreateClient();
            CreateExitAndPayment();
            CreateOperationsBuildings();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Parking MVP scene created at " + ScenePath);
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        private static void CreatePlayer(GameCompositionRoot compositionRoot)
        {
            var player = new GameObject("Player_FirstPerson_NoHands");
            player.transform.position = new Vector3(0f, 1.1f, -10f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var playerController = player.AddComponent<FirstPersonPlayerController>();
            var cameraObject = new GameObject("FirstPersonCamera_NoHands");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            playerController.Configure(cameraComponent, compositionRoot);
        }

        private static void CreateGround()
        {
            var ground = InstantiateAsset(GroundPath, "ParkingGround_Cobblestone");
            ground.transform.position = new Vector3(0f, -0.08f, 0f);
            ground.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
            AddMeshColliders(ground);
        }

        private static void CreateParkingZone()
        {
            var zone = new GameObject("ParkingSlot_01");
            zone.transform.position = Vector3.zero;
        }

        private static void CreateClient()
        {
            var horse = InstantiateAsset(HorsePath, "ClientHorse_01");
            horse.transform.position = new Vector3(0f, 0f, -2f);
            horse.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            horse.transform.localScale = Vector3.one * 1.15f;
            AddMeshColliders(horse);

            var rider = InstantiateAsset(RiderPath, "ClientRider_01");
            rider.transform.SetParent(horse.transform, true);
            rider.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            rider.transform.localRotation = Quaternion.identity;
            rider.transform.localScale = Vector3.one * 0.55f;
        }

        private static void CreateExitAndPayment()
        {
            var gate = InstantiateAsset(GatePath, "ExitGate_01");
            gate.transform.position = new Vector3(0f, 0f, 7f);
            gate.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            AddMeshColliders(gate);

            var sack = InstantiateAsset(SackPath, "PaymentSack_01");
            sack.transform.position = new Vector3(0.8f, 0.7f, 5.8f);
            sack.transform.localScale = Vector3.one * 1.3f;
        }

        private static void CreateOperationsBuildings()
        {
            var store = InstantiateAsset(StorePath, "MaterialStore_01");
            store.transform.position = new Vector3(-8f, 0f, 1f);
            store.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            var warehouse = InstantiateAsset(WarehousePath, "Warehouse_01");
            warehouse.transform.position = new Vector3(8f, 0f, 1f);
            warehouse.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
        }

        private static GameObject InstantiateAsset(string assetPath, string instanceName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                throw new System.InvalidOperationException("Required asset is missing: " + assetPath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = instanceName;
            return instance;
        }

        private static void AddMeshColliders(GameObject root)
        {
            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                var collider = meshFilter.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                }

                collider.sharedMesh = meshFilter.sharedMesh;
            }
        }
    }
}
