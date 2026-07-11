using System.Linq;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorseParking.Presentation.Editor
{
    public static class ParkingMvpSceneBuilder
    {
        private static float groundSurfaceY;
        private const string ScenePath = "Assets/_Project/Scenes/ParkingMvp.unity";
        private const string GroundPath = "Assets/_Project/Content/Models/Environment/ParkingGround/CobblestoneLowpoly/cobblestone.fbx";
        private const string MountedClientPath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/SM_RedHorseRider.fbx";
        private const string AdultHorsePath = "Assets/_Project/Content/Models/Characters/Horse/AdultTexturedHorse/uploads_files_2751214_horse.fbx";
        private const string RiderPath = "Assets/_Project/Content/Models/Characters/Rider/MedievalCharacter/SM_MedievalRider.fbx";
        private const string RiderAnimationPath = "Assets/_Project/Content/Animations/Characters/Rider/MedievalRider/SeatedIdle_Source.fbx";
        private const string RiderControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_MedievalRiderSeatedIdle.controller";
        private const string StorePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_market_blue.fbx";
        private const string WarehousePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_lumbermill_blue.fbx";
        private const string GatePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/neutral/fence_wood_straight_gate.fbx";
        private const string SackPath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/decoration/props/sack.fbx";
        private const string HorseMaterialPath = "Assets/_Project/Content/Materials/Characters/Horse/M_HorseChestnut.mat";
        private const string MountedHorseMaterialPath = "Assets/_Project/Content/Materials/Characters/MountedClients/M_RedHorseRiderHorse.mat";
        private const string MountedHorseTexturePath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/textures/HorseWagonTexture2.png";

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
            player.transform.position = new Vector3(0f, 1.1f, -13f);
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
            ScaleToHorizontalLength(ground, 30f);
            PlaceBaseAtHeight(ground, 0f);
            groundSurfaceY = GetBounds(ground).max.y;
            AddGroundCollider(ground);
        }

        private static void CreateParkingZone()
        {
            var zone = new GameObject("ParkingSlot_01");
            zone.transform.position = Vector3.zero;

            CreateParkingFence("ParkingSlot_01_LeftFence", new Vector3(-2f, 0f, -1f), 90f);
            CreateParkingFence("ParkingSlot_01_RightFence", new Vector3(2f, 0f, -1f), 90f);
            CreateParkingFence("ParkingSlot_01_BackFence", new Vector3(0f, 0f, 1.1f), 0f);
        }

        private static void CreateClient()
        {
            var horse = InstantiateAsset(AdultHorsePath, "ClientHorse_01");
            ScaleToHeight(horse, 1.65f);
            horse.transform.position = new Vector3(0f, 0f, -1f);
            PlaceBaseOnGround(horse);
            AddMeshColliders(horse);
        }

        private static void ApplyMountedHorseMaterial(GameObject mountedClient)
        {
            var horseRenderer = mountedClient.GetComponentsInChildren<Renderer>()
                .FirstOrDefault(renderer => renderer.name == "Horse");
            var horseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MountedHorseTexturePath);
            if (horseRenderer == null || horseTexture == null)
            {
                throw new System.InvalidOperationException("Mounted horse material or texture is missing.");
            }

            EnsureFolder("Assets/_Project/Content/Materials");
            EnsureFolder("Assets/_Project/Content/Materials/Characters");
            EnsureFolder("Assets/_Project/Content/Materials/Characters/MountedClients");
            AssetDatabase.DeleteAsset(MountedHorseMaterialPath);
            var material = new Material(horseRenderer.sharedMaterial)
            {
                name = "M_RedHorseRiderHorse",
                mainTexture = horseTexture
            };
            AssetDatabase.CreateAsset(material, MountedHorseMaterialPath);

            foreach (var renderer in mountedClient.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name == "Horse" || renderer.name.StartsWith("tireshape"))
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        private static void CreateRider(GameObject horse)
        {
            ConfigureRiderAnimationImport();
            var rider = InstantiateAsset(RiderPath, "ClientRider_01");
            rider.transform.SetParent(horse.transform, false);
            ScaleToHeight(rider, 1.7f);
            rider.transform.localPosition = new Vector3(0f, 1.05f, -0.18f);
            rider.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var animator = rider.GetComponent<Animator>();
            if (animator == null)
            {
                animator = rider.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = GetOrCreateRiderController();
            animator.applyRootMotion = false;
        }

        private static void CreateExitAndPayment()
        {
            var gate = InstantiateAsset(GatePath, "ExitGate_01");
            ScaleToHeight(gate, 2.2f);
            gate.transform.position = new Vector3(0f, 0f, 10f);
            gate.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            PlaceBaseOnGround(gate);
            AddMeshColliders(gate);

            var sack = InstantiateAsset(SackPath, "PaymentSack_01");
            ScaleToHeight(sack, 0.35f);
            sack.transform.position = new Vector3(0.8f, 0.2f, 8.5f);
            PlaceBaseOnGround(sack);
        }

        private static void CreateParkingFence(string name, Vector3 position, float yRotation)
        {
            var fence = InstantiateAsset(GatePath, name);
            ScaleToHeight(fence, 1.25f);
            fence.transform.position = position;
            fence.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            PlaceBaseOnGround(fence);
            AddMeshColliders(fence);
        }

        private static void CreateOperationsBuildings()
        {
            var store = InstantiateAsset(StorePath, "MaterialStore_01");
            ScaleToWidth(store, 5f);
            store.transform.position = new Vector3(-7f, 0f, 2f);
            store.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            PlaceBaseOnGround(store);

            var warehouse = InstantiateAsset(WarehousePath, "Warehouse_01");
            ScaleToWidth(warehouse, 5f);
            warehouse.transform.position = new Vector3(7f, 0f, 2f);
            warehouse.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            PlaceBaseOnGround(warehouse);
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

        private static void AddGroundCollider(GameObject root)
        {
            // The scanned paving mesh has millions of triangles; a MeshCollider here is
            // needlessly expensive. The invisible box matches the playable area instead.
            var bounds = GetBounds(root);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = root.transform.InverseTransformVector(bounds.size);
        }

        private static void ApplyHorseMaterial(GameObject horse)
        {
            var sourceMaterial = horse.GetComponentInChildren<Renderer>().sharedMaterial;
            EnsureFolder("Assets/_Project/Content/Materials");
            EnsureFolder("Assets/_Project/Content/Materials/Characters");
            EnsureFolder("Assets/_Project/Content/Materials/Characters/Horse");
            AssetDatabase.DeleteAsset(HorseMaterialPath);

            // Keep the shader supplied by the FBX because it is already compatible with
            // this project. Only the color changes; the model and its animations remain assets.
            var material = new Material(sourceMaterial)
            {
                name = "M_HorseChestnut"
            };
            var chestnut = new Color(0.28f, 0.09f, 0.025f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", chestnut);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", chestnut);
            }

            AssetDatabase.CreateAsset(material, HorseMaterialPath);

            foreach (var renderer in horse.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void ConfigureRiderAnimationImport()
        {
            var importer = AssetImporter.GetAtPath(RiderAnimationPath) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("Seated Idle animation is missing.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        private static void ConfigureRiderModelImport()
        {
            var importer = AssetImporter.GetAtPath(RiderPath) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("X Bot model is missing.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        private static AnimatorController GetOrCreateRiderController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(RiderControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(RiderControllerPath);
            var animationClips = AssetDatabase.LoadAllAssetsAtPath(RiderAnimationPath)
                .OfType<AnimationClip>()
                .ToArray();
            var seatedIdle = animationClips.FirstOrDefault(clip => clip.name == "mixamo.com")
                ?? animationClips.FirstOrDefault();
            if (seatedIdle == null)
            {
                throw new System.InvalidOperationException(
                    "Seated Idle imported without a usable clip. Found: " + string.Join(", ", animationClips.Select(clip => clip.name)));
            }
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("SeatedIdle");
            state.motion = seatedIdle;
            stateMachine.defaultState = state;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parentPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parentPath!, folderName);
        }

        private static void ScaleToHorizontalLength(GameObject root, float desiredLength)
        {
            var bounds = GetBounds(root);
            var currentLength = Mathf.Max(bounds.size.x, bounds.size.z);
            root.transform.localScale *= desiredLength / currentLength;
        }

        private static void ScaleToHeight(GameObject root, float desiredHeight)
        {
            var bounds = GetBounds(root);
            root.transform.localScale *= desiredHeight / bounds.size.y;
        }

        private static void ScaleToWidth(GameObject root, float desiredWidth)
        {
            var bounds = GetBounds(root);
            root.transform.localScale *= desiredWidth / Mathf.Max(bounds.size.x, bounds.size.z);
        }

        private static void PlaceBaseOnGround(GameObject root)
        {
            PlaceBaseAtHeight(root, groundSurfaceY);
        }

        private static void PlaceBaseAtHeight(GameObject root, float surfaceY)
        {
            var bounds = GetBounds(root);
            root.transform.position += new Vector3(0f, surfaceY - bounds.min.y, 0f);
        }

        private static Bounds GetBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }
    }
}
