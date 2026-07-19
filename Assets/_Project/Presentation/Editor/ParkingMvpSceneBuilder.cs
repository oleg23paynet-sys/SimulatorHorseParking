using System.Linq;
using System.Collections.Generic;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Logistics;
using HorseParking.Presentation.Localization;
using HorseParking.Presentation.Player;
using HorseParking.Presentation.Parking;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace HorseParking.Presentation.Editor
{
    public static class ParkingMvpSceneBuilder
    {
        private static float groundSurfaceY;
        private const string ScenePath = "Assets/_Project/Scenes/ParkingMvp.unity";
        private const string LogisticsInventorySettingsPath = "Assets/_Project/Settings/LogisticsInventorySettings.asset";
        private const string GameLocalizationSettingsPath = "Assets/_Project/Settings/GameLocalizationSettings.asset";
        private const string DeliveryCartRuntimeFolder = "Assets/_Project/Content/Vehicles/DeliveryCart/Runtime/HandPushCart";
        private const string DeliveryCartModelPath = DeliveryCartRuntimeFolder + "/SM_HandPushCart.fbx";
        private const string DeliveryCartTextureFolder = DeliveryCartRuntimeFolder + "/textures";
        private const string DeliveryCartWoodDiffusePath = DeliveryCartTextureFolder + "/Wood035_2K-JPG_Color.jpg";
        private const string DeliveryCartWoodNormalPath = DeliveryCartTextureFolder + "/Wood035_2K-JPG_NormalGL.jpg";
        private const string DeliveryCartFabricDiffusePath = DeliveryCartTextureFolder + "/YellowFabric.jpg";
        private const string DeliveryCartFabricNormalPath = DeliveryCartTextureFolder + "/Fabric032_2K_NormalGL.jpg";
        private const string DeliveryCartControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_DeliveryCartWheels.controller";
        private const string DeliveryCartDriverControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_DeliveryCartDriver.controller";
        private const string DeliveryCartMaterialFolder = "Assets/_Project/Content/Vehicles/DeliveryCart/Materials";
        private const string CartDriverModelPath = "Assets/_Project/Content/Models/Characters/CartDriver/MedievalCivilian3/Runtime/CH_CartDriver.fbx";
        private const string CartDriverRuntimeFolder = "Assets/_Project/Content/Models/Characters/CartDriver/MedievalCivilian3/Runtime";
        private const string CartDriverTextureFolder = CartDriverRuntimeFolder + "/Textures";
        private const string CartDriverMaterialFolder = CartDriverRuntimeFolder + "/Materials";
        private const string KayKitPropsFolder = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/decoration/props";
        private const string WoodCargoModelPath = KayKitPropsFolder + "/resource_lumber.fbx";
        private const string StoneCargoModelPath = KayKitPropsFolder + "/resource_stone.fbx";
        private const string IronCargoModelPath = KayKitPropsFolder + "/crate_A_small.fbx";
        private const string TerrainDataPath = "Assets/_Project/Content/Terrain/TerrainData_ParkingMvp.asset";
        private const string TerrainLayerPath = "Assets/_Project/Content/Terrain/TerrainLayer_ParkingMvpCobblestone.terrainlayer";
        private const string TerrainTexturePath = "Assets/_Project/Content/Models/Environment/ParkingGround/CobblestoneLowpoly/0.jpg";
        private const string MountedClientPath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/SM_RedHorseRider.fbx";
        private const string HorseAnimsetModelPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Horse Realistic.FBX";
        private const string HorseAnimsetAvatarPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Horse_A.fbx";
        private const string HorseAnimsetIdlePath = "Assets/_Project/ThirdParty/HorseAnimsetPro/H_Idle_01.FBX";
        private const string HorseAnimsetWalkPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/H_Walk.FBX";
        private const string HorseAnimsetAlbedoPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Horse4 Albedo Brown.png";
        private const string HorseAnimsetNormalPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Horse4 Normal.png";
        private const string HorseAnimsetMetallicPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Horse4 MetallicSmoothness.png";
        private const string HorseAnimsetControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_HorseAnimsetParkingClient.controller";
        private const string HorseAnimsetRiderAvatarPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Rider.FBX";
        private const string HorseAnimsetRiderIdlePath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Rider_Idle_01.FBX";
        private const string HorseAnimsetRiderWalkPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Rider_Walk.FBX";
        private const string HorseAnimsetRiderMountDismountPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Transitions/Rider_Mount_Dismount_Left.FBX";
        private const string HorseAnimsetRiderOnFootIdlePath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/OnFoot/S_Idle.fbx";
        private const string HorseAnimsetRiderOnFootWalkPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/OnFoot/S_Walk_F.fbx";
        private const string HorseAnimsetRiderAlbedoPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Textures/CowBoyDiffuse.png";
        private const string HorseAnimsetRiderNormalPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Textures/CowBoyNormal.Png";
        private const string HorseAnimsetRiderSpecularPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Textures/CowboySpecularSmoothness.png";
        private const string HorseAnimsetRiderControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_HorseAnimsetMountedRider.controller";
        private const string HorseAnimsetMaterialFolder = "Assets/_Project/Content/Materials/Characters/HorseAnimsetPro";
        private const string TemporaryMountedClientPath = "Assets/_Project/Content/Models/Characters/MountedClients/TemporaryManualV2/SK_ParkingClientMounted_TemporaryV2.fbx";
        private const string TemporaryHorseWalkClipPath = "Assets/_Project/Presentation/Animation/Clips/A_TemporaryHorseWalk_V2.anim";
        private const string VoxelHorsePath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/FBX/Animal/TVS_VoxelKnights_Horse.fbx";
        private const string VoxelKnightPath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/FBX/Characters/TVS_VoxelKnights_Knight.fbx";
        private const string VoxelHorseIdlePath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/Animations/Animals/Horse_Idle_Anim.fbx";
        private const string VoxelHorseWalkPath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/Animations/Animals/Horse_Walk_Anim.fbx";
        private const string VoxelRiderMountedPath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/Animations/Humans/Human_Mounted_Anim.fbx";
        private const string VoxelHorseTexturePath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/Textures/Animals/TVS_VoxelKnights_Horse_Texture.png";
        private const string VoxelKnightTexturePath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/VoxelKnights/Voxel Knights/Textures/Characters/TVS_VoxelKnights_Knight_Texture.png";
        private const string VoxelHorseControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_VoxelHorse.controller";
        private const string VoxelRiderControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_VoxelKnightMounted.controller";
        private const string VoxelHorseMaterialPath = "Assets/_Project/Content/Materials/Characters/VoxelKnights/M_VoxelHorse.mat";
        private const string VoxelKnightMaterialPath = "Assets/_Project/Content/Materials/Characters/VoxelKnights/M_VoxelKnight.mat";
        private const string AdultHorsePath = "Assets/_Project/Content/Models/Characters/Horse/AdultTexturedHorse/uploads_files_2751214_horse.fbx";
        private const string RiderPath = "Assets/_Project/Content/Models/Characters/Rider/MedievalCharacter/SM_MedievalRider.fbx";
        private const string RiderAnimationPath = "Assets/_Project/Content/Animations/Characters/Rider/MedievalRider/SeatedIdle_Source.fbx";
        private const string RiderControllerPath = "Assets/_Project/Presentation/Animation/Controllers/AC_MedievalRiderSeatedIdle.controller";
        private const string StorePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_market_blue.fbx";
        private const string WarehousePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/blue/building_lumbermill_blue.fbx";
        private const string GatePath = "Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/buildings/neutral/fence_wood_straight_gate.fbx";
        private const string PaymentPouchPath = "Assets/_Project/Content/Models/Props/PaymentPouch/SM_PaymentPouch.fbx";
        private const float PaymentPouchCordLength = 0.14f;
        private const string HorseMaterialPath = "Assets/_Project/Content/Materials/Characters/Horse/M_HorseChestnut.mat";
        private const string MountedHorseMaterialPath = "Assets/_Project/Content/Materials/Characters/MountedClients/M_RedHorseRiderHorse.mat";
        private const string MountedHorseTexturePath = "Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/textures/HorseWagonTexture2.png";

        [MenuItem("Horse Parking/Build Parking MVP Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting();
            var compositionRoot = new GameObject("GameCompositionRoot").AddComponent<GameCompositionRoot>();
            compositionRoot.ConfigureLogisticsInventory(LoadOrCreateLogisticsInventorySettings());
            compositionRoot.ConfigureLocalization(LoadOrCreateGameLocalizationSettings());
            var playerController = CreatePlayer(compositionRoot);
            CreateLogisticsInventoryHud(compositionRoot);
            CreateGround();
            var parkingRouteObstacles = CreateParkingZone();
            var client = CreateClient(
                out var animationAdapter,
                out var paymentBagAnchor,
                out var riderRoot,
                out var riderAnimator,
                out var saddleParent);
            var exitAndPayment = CreateExitAndPayment();
            CreateParkingRuntime(
                compositionRoot,
                client,
                animationAdapter,
                paymentBagAnchor,
                riderRoot,
                riderAnimator,
                saddleParent,
                exitAndPayment.gate,
                exitAndPayment.sack,
                parkingRouteObstacles);
            var operationsBuildings = CreateOperationsBuildings();
            CreateStage32Logistics(
                compositionRoot,
                playerController,
                operationsBuildings.store,
                operationsBuildings.warehouse,
                LoadOrCreateLogisticsInventorySettings(),
                LoadOrCreateGameLocalizationSettings());

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Parking MVP scene created at " + ScenePath);
        }

        [MenuItem("Horse Parking/Install Stage 3.1 Inventory")]
        public static void InstallStage31Inventory()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var compositionRoot = Object.FindAnyObjectByType<GameCompositionRoot>();
            if (compositionRoot == null)
            {
                throw new System.InvalidOperationException("ParkingMvp is missing GameCompositionRoot.");
            }

            compositionRoot.ConfigureLogisticsInventory(LoadOrCreateLogisticsInventorySettings());
            compositionRoot.ConfigureLocalization(LoadOrCreateGameLocalizationSettings());
            CreateLogisticsInventoryHud(compositionRoot);
            EditorUtility.SetDirty(compositionRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 3.1 logistics inventory installed in " + ScenePath);
        }

        [MenuItem("Horse Parking/Install Stage 3.2 Cart Journey")]
        public static void InstallStage32CartJourney()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var compositionRoot = Object.FindAnyObjectByType<GameCompositionRoot>()
                ?? throw new System.InvalidOperationException("ParkingMvp is missing GameCompositionRoot.");
            var playerController = Object.FindAnyObjectByType<FirstPersonPlayerController>()
                ?? throw new System.InvalidOperationException("ParkingMvp is missing the first-person player.");
            var store = GameObject.Find("MaterialStore_01")
                ?? throw new System.InvalidOperationException("ParkingMvp is missing MaterialStore_01.");
            var warehouse = GameObject.Find("Warehouse_01")
                ?? throw new System.InvalidOperationException("ParkingMvp is missing Warehouse_01.");
            var inventorySettings = LoadOrCreateLogisticsInventorySettings();
            var localizationSettings = LoadOrCreateGameLocalizationSettings();

            EnsureStage32Translations(localizationSettings);
            compositionRoot.ConfigureLogisticsInventory(inventorySettings);
            compositionRoot.ConfigureLocalization(localizationSettings);
            CreateLogisticsInventoryHud(compositionRoot);
            CreateStage32Logistics(
                compositionRoot,
                playerController,
                store,
                warehouse,
                inventorySettings,
                localizationSettings);

            EditorUtility.SetDirty(compositionRoot);
            EditorUtility.SetDirty(inventorySettings);
            EditorUtility.SetDirty(localizationSettings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 3.2 automatic delivery cart journey installed in " + ScenePath);
        }

        [MenuItem("Horse Parking/Repair Delivery Cart Cargo And Route")]
        public static void RepairDeliveryCartCargoAndRoute()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var adapter = Object.FindAnyObjectByType<DeliveryCartVisualAdapter>()
                ?? throw new System.InvalidOperationException("ParkingMvp is missing DeliveryCartVisualAdapter.");
            var vehicle = adapter.gameObject;
            var cart = vehicle.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == "DeliveryCart_Visual")?.gameObject
                ?? throw new System.InvalidOperationException("ParkingMvp is missing DeliveryCart_Visual.");

            foreach (var existing in vehicle.GetComponentsInChildren<Transform>(true)
                         .Where(candidate => candidate.name.StartsWith("DeliveryCartCargo_"))
                         .Select(candidate => candidate.gameObject)
                         .ToArray())
            {
                Object.DestroyImmediate(existing);
            }

            var woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WoodCargoModelPath)
                ?? throw new System.InvalidOperationException("Wood cargo FBX is missing.");
            var stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoneCargoModelPath)
                ?? throw new System.InvalidOperationException("Stone cargo FBX is missing.");
            var ironPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IronCargoModelPath)
                ?? throw new System.InvalidOperationException("Iron cargo FBX is missing.");
            adapter.ConfigureCargoPrefabs(woodPrefab, stonePrefab, ironPrefab);
            adapter.ConfigureCargoVisuals(
                CreateDeliveryCartCargoVisuals(
                    vehicle.transform, cart, woodPrefab, "DeliveryCartCargo_Wood",
                    new[] { new Vector2(-0.34f, 0.02f), new Vector2(0.34f, 0.02f) }, 0.62f),
                CreateDeliveryCartCargoVisuals(
                    vehicle.transform, cart, stonePrefab, "DeliveryCartCargo_Stone",
                    new[] { new Vector2(-0.34f, 0.38f), new Vector2(0.34f, 0.38f) }, 0.52f),
                CreateDeliveryCartCargoVisuals(
                    vehicle.transform, cart, ironPrefab, "DeliveryCartCargo_Iron",
                    new[] { new Vector2(-0.34f, -0.32f), new Vector2(0.34f, -0.32f) }, 0.54f));

            SetRoutePoint("CartRoute_WarehouseDelivery", new Vector3(7.2f, groundSurfaceY, -3.8f));
            SetRoutePoint("CartRoute_WarehouseApproach", new Vector3(7.2f, groundSurfaceY, -8f));
            SetRoutePoint("CartRoute_StoreApproach", new Vector3(-7.2f, groundSurfaceY, -8f));
            SetRoutePoint("CartRoute_MaterialStoreLoading", new Vector3(-7.2f, groundSurfaceY, -3.8f));

            EditorUtility.SetDirty(adapter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Delivery cart cargo and collision-safe route repaired in " + ScenePath);
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        private static FirstPersonPlayerController CreatePlayer(GameCompositionRoot compositionRoot)
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
            return playerController;
        }

        private static void CreateGround()
        {
            EnsureFolder("Assets/_Project/Content");
            EnsureFolder("Assets/_Project/Content/Terrain");
            AssetDatabase.DeleteAsset(TerrainDataPath);
            AssetDatabase.DeleteAsset(TerrainLayerPath);
            var terrainData = new TerrainData
            {
                heightmapResolution = 33,
                size = new Vector3(40f, 3f, 40f)
            };
            terrainData.SetHeights(0, 0, new float[33, 33]);
            terrainData.terrainLayers = new[] { CreateGroundTerrainLayer() };
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);

            var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "ParkingTerrain_40x40";
            terrainObject.transform.position = new Vector3(-20f, 0f, -20f);
            groundSurfaceY = terrainObject.transform.position.y + terrainData.GetInterpolatedHeight(0.5f, 0.5f);
        }

        private static Collider[] CreateParkingZone()
        {
            var zone = new GameObject("ParkingSlot_01");
            zone.transform.position = Vector3.zero;

            var left = CreateParkingFence("ParkingSlot_01_LeftFence", new Vector3(-2f, 0f, -1f), 90f);
            var right = CreateParkingFence("ParkingSlot_01_RightFence", new Vector3(2f, 0f, -1f), 90f);
            var back = CreateParkingFence("ParkingSlot_01_BackFence", new Vector3(0f, 0f, 1.1f), 0f);
            return left.GetComponentsInChildren<Collider>(true)
                .Concat(right.GetComponentsInChildren<Collider>(true))
                .Concat(back.GetComponentsInChildren<Collider>(true))
                .ToArray();
        }

        private static GameObject CreateClient(
            out MonoBehaviour animationAdapter,
            out Transform paymentBagAnchor,
            out Transform riderRoot,
            out Animator riderAnimator,
            out Transform saddleParent)
        {
            var mountedClient = new GameObject("ClientMountedHorseRider_01");
            ConfigureHorseAnimsetImports();
            var horseVisual = InstantiateAsset(HorseAnimsetModelPath, "HorseAnimsetPro_Visual");
            horseVisual.transform.SetParent(mountedClient.transform, false);
            ConfigureHorseAnimsetRenderers(horseVisual);
            ScaleToHeight(horseVisual, 2.25f);

            var animator = horseVisual.GetComponent<Animator>() ?? horseVisual.AddComponent<Animator>();
            animator.runtimeAnimatorController = GetOrCreateHorseAnimsetController();
            // Route navigation owns ordinary horse translation and rotation. H_Walk
            // is visual-only; paid Root Motion remains enabled only for rider
            // mount/dismount transitions in RiderParkingSequencePresenter.
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            mountedClient.transform.position = new Vector3(0f, 0f, -12f);
            PlaceHorseFeetOnTerrain(horseVisual);
            riderAnimator = CreateReadySeatedRider(mountedClient, horseVisual, out riderRoot, out saddleParent);
            var adapter = mountedClient.AddComponent<AnimatorMountedClientAnimationAdapter>();
            adapter.Configure(new[] { animator, riderAnimator });
            animationAdapter = adapter;
            paymentBagAnchor = CreatePaymentBagAnchor(horseVisual);
            return mountedClient;
        }

        private static void ConfigureHorseAnimsetImports()
        {
            var avatarImporter = AssetImporter.GetAtPath(HorseAnimsetAvatarPath) as ModelImporter;
            if (avatarImporter == null)
            {
                throw new System.InvalidOperationException("Horse Animset source avatar is missing: " + HorseAnimsetAvatarPath);
            }

            avatarImporter.animationType = ModelImporterAnimationType.Generic;
            avatarImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            avatarImporter.SaveAndReimport();
            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(HorseAnimsetAvatarPath).OfType<Avatar>().FirstOrDefault();
            if (sourceAvatar == null)
            {
                throw new System.InvalidOperationException("Horse Animset source avatar did not import.");
            }

            ConfigureHorseModelImporter(HorseAnimsetModelPath, ModelImporterAvatarSetup.CreateFromThisModel, null, false);
            ConfigureHorseModelImporter(HorseAnimsetIdlePath, ModelImporterAvatarSetup.CopyFromOther, sourceAvatar, true);
            ConfigureHorseModelImporter(HorseAnimsetWalkPath, ModelImporterAvatarSetup.CopyFromOther, sourceAvatar, true);
        }

        private static void ConfigureHorseModelImporter(string path, ModelImporterAvatarSetup avatarSetup, Avatar sourceAvatar, bool loop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("Horse Animset asset is missing: " + path);
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = avatarSetup;
            if (sourceAvatar != null)
            {
                importer.sourceAvatar = sourceAvatar;
            }

            if (loop)
            {
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                    clip.keepOriginalPositionXZ = true;
                    clip.keepOriginalPositionY = true;
                }

                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
        }

        private static AnimatorController GetOrCreateHorseAnimsetController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(HorseAnimsetControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(HorseAnimsetControllerPath);
            controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var stateMachine = controller.layers[0].stateMachine;
            var idle = stateMachine.AddState("Idle");
            idle.motion = LoadHorseAnimsetClip(HorseAnimsetIdlePath, "H_Idle_01");
            var walk = stateMachine.AddState("Walk");
            walk.motion = LoadHorseAnimsetClip(HorseAnimsetWalkPath, "H_Walk");
            stateMachine.defaultState = idle;

            var toWalk = idle.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walking");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            var toIdle = walk.AddTransition(idle);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walking");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadHorseAnimsetClip(string path, string preferredName)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();
            var clip = clips.FirstOrDefault(candidate => candidate.name == preferredName) ?? clips.FirstOrDefault();
            if (clip == null)
            {
                throw new System.InvalidOperationException("Horse Animset clip did not import from " + path);
            }

            return clip;
        }

        private static void ConfigureHorseAnimsetRenderers(GameObject horseVisual)
        {
            var enabledNames = new HashSet<string>
            {
                "Horse Body", "Horse Eyes", "Horse Mane1", "Horse Tail", "Saddle", "Reins"
            };
            foreach (var renderer in horseVisual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = enabledNames.Contains(renderer.name);
            }

            if (!horseVisual.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.enabled && renderer.name == "Horse Body"))
            {
                throw new System.InvalidOperationException("Horse Animset realistic body renderer was not found.");
            }

            ApplyHorseAnimsetMaterials(horseVisual);
        }

        private static void ApplyHorseAnimsetMaterials(GameObject horseVisual)
        {
            EnsureFolder("Assets/_Project/Content/Materials");
            EnsureFolder("Assets/_Project/Content/Materials/Characters");
            EnsureFolder(HorseAnimsetMaterialFolder);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(HorseAnimsetAlbedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(HorseAnimsetNormalPath);
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(HorseAnimsetMetallicPath);
            if (albedo == null || normal == null || metallic == null)
            {
                throw new System.InvalidOperationException("Horse Animset textures are missing.");
            }

            var body = CreateHorseAnimsetMaterial("M_HAP_HorseBody", new Color(0.42f, 0.19f, 0.08f), albedo, normal, metallic);
            var darkHair = CreateHorseAnimsetMaterial("M_HAP_HorseHair", new Color(0.035f, 0.025f, 0.02f), null, null, null);
            var leather = CreateHorseAnimsetMaterial("M_HAP_Leather", new Color(0.16f, 0.055f, 0.02f), null, null, null);
            var eyes = CreateHorseAnimsetMaterial("M_HAP_Eyes", new Color(0.01f, 0.008f, 0.006f), null, null, null);
            foreach (var renderer in horseVisual.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled))
            {
                if (renderer.name == "Horse Body") renderer.sharedMaterial = body;
                else if (renderer.name == "Horse Eyes") renderer.sharedMaterial = eyes;
                else if (renderer.name.Contains("Mane") || renderer.name.Contains("Tail")) renderer.sharedMaterial = darkHair;
                else renderer.sharedMaterial = leather;
            }
        }

        private static Material CreateHorseAnimsetMaterial(string name, Color color, Texture2D albedo, Texture2D normal, Texture2D metallic)
        {
            var path = HorseAnimsetMaterialFolder + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            // ParkingMvp currently uses the Built-in Render Pipeline. Selecting the
            // installed-but-inactive URP shader would render the horse magenta.
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new System.InvalidOperationException("Built-in Standard shader is unavailable.");
            }
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (albedo != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (metallic != null && material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", metallic);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.32f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Animator CreateReadySeatedRider(
            GameObject mountedClient,
            GameObject horseVisual,
            out Transform riderRoot,
            out Transform saddleParent)
        {
            // The old female mesh had no skeleton and could never follow a riding clip.
            // The paid HAP rider is a proper Humanoid and is isolated behind the same
            // animation adapter as the horse, so the visual package remains replaceable.
            ConfigureHorseAnimsetRiderImports();
            var rider = InstantiateAsset(HorseAnimsetRiderAvatarPath, "ClientRider_01");
            rider.transform.SetParent(mountedClient.transform, false);
            var riderMaterial = CreateHorseAnimsetMaterial(
                "M_HAP_Rider",
                Color.white,
                AssetDatabase.LoadAssetAtPath<Texture2D>(HorseAnimsetRiderAlbedoPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(HorseAnimsetRiderNormalPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(HorseAnimsetRiderSpecularPath));
            foreach (var renderer in rider.GetComponentsInChildren<Renderer>(true))
            {
                // Pistols do not belong in the medieval parking demo.
                renderer.enabled = !renderer.name.StartsWith("Pistol");
                if (renderer.enabled)
                {
                    renderer.sharedMaterial = riderMaterial;
                }
            }

            ScaleEnabledRenderersToHeight(rider, 1.72f);
            var horseBounds = GetEnabledRendererBounds(horseVisual);
            var riderBounds = GetEnabledRendererBounds(rider);
            // Keep the seated rider above the saddle instead of letting the legs
            // and pelvis intersect the horse's back.
            var desiredBottom = horseBounds.min.y + 0.82f;
            rider.transform.position += new Vector3(
                horseBounds.center.x - riderBounds.center.x,
                desiredBottom - riderBounds.min.y,
                horseBounds.center.z - riderBounds.center.z - 0.08f);

            var riderAnimator = rider.GetComponent<Animator>() ?? rider.AddComponent<Animator>();
            riderAnimator.runtimeAnimatorController = GetOrCreateHorseAnimsetRiderController();
            // Root Motion stays enabled on the paid rider Animator. The sequence
            // presenter consumes it only in MountLeft/DismountLeft and ignores it in
            // mounted/on-foot looping states.
            riderAnimator.applyRootMotion = true;
            riderAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var saddleBone = FindDescendant(horseVisual.transform, "Spine2") ?? FindDescendant(horseVisual.transform, "Back");
            saddleParent = saddleBone != null ? saddleBone : mountedClient.transform;
            rider.transform.SetParent(saddleParent, true);
            riderRoot = rider.transform;

            return riderAnimator;
        }

        private static void ConfigureHorseAnimsetRiderImports()
        {
            var sourceImporter = AssetImporter.GetAtPath(HorseAnimsetRiderAvatarPath) as ModelImporter;
            if (sourceImporter == null)
            {
                throw new System.InvalidOperationException("Horse Animset rider avatar is missing: " + HorseAnimsetRiderAvatarPath);
            }

            sourceImporter.animationType = ModelImporterAnimationType.Human;
            sourceImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            sourceImporter.SaveAndReimport();
            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(HorseAnimsetRiderAvatarPath).OfType<Avatar>().FirstOrDefault();
            if (sourceAvatar == null || !sourceAvatar.isValid || !sourceAvatar.isHuman)
            {
                throw new System.InvalidOperationException("Horse Animset rider humanoid avatar is invalid.");
            }

            ConfigureHorseAnimsetRiderClip(HorseAnimsetRiderIdlePath, sourceAvatar);
            ConfigureHorseAnimsetRiderClip(HorseAnimsetRiderWalkPath, sourceAvatar);
            ConfigureHorseAnimsetRiderClip(HorseAnimsetRiderMountDismountPath, sourceAvatar, false, true);
            ConfigureHorseAnimsetRiderClip(HorseAnimsetRiderOnFootIdlePath, sourceAvatar);
            ConfigureHorseAnimsetRiderClip(HorseAnimsetRiderOnFootWalkPath, sourceAvatar);
        }

        private static void ConfigureHorseAnimsetRiderClip(string path, Avatar sourceAvatar, bool loop = true, bool useRootMotion = false)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("Horse Animset rider clip is missing: " + path);
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }
            foreach (var clip in clips)
            {
                clip.loopTime = loop;
                clip.loopPose = loop;
                // Mount/dismount transitions contain authored root translation. Baking
                // it into the body pose makes the rider float above the horse.
                clip.keepOriginalPositionXZ = !useRootMotion;
                clip.keepOriginalPositionY = !useRootMotion;
                clip.heightFromFeet = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController GetOrCreateHorseAnimsetRiderController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(HorseAnimsetRiderControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(HorseAnimsetRiderControllerPath);
            controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("OnFootWalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dismount", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Mount", AnimatorControllerParameterType.Trigger);
            var stateMachine = controller.layers[0].stateMachine;
            var mountedIdle = stateMachine.AddState("MountedIdle");
            mountedIdle.motion = LoadHorseAnimsetClip(HorseAnimsetRiderIdlePath, "Rider_Idle_01");
            var mountedWalk = stateMachine.AddState("MountedWalk");
            mountedWalk.motion = LoadHorseAnimsetClip(HorseAnimsetRiderWalkPath, "Rider_Walk");
            var dismount = stateMachine.AddState("DismountLeft");
            dismount.motion = LoadHorseAnimsetClip(HorseAnimsetRiderMountDismountPath, "Rider_Dismount_Left");
            var onFootIdle = stateMachine.AddState("OnFootIdle");
            onFootIdle.motion = LoadHorseAnimsetClip(HorseAnimsetRiderOnFootIdlePath, "S_Idle");
            var onFootWalk = stateMachine.AddState("OnFootWalk");
            onFootWalk.motion = LoadHorseAnimsetClip(HorseAnimsetRiderOnFootWalkPath, "S_Walk_F");
            var mount = stateMachine.AddState("MountLeft");
            mount.motion = LoadHorseAnimsetClip(HorseAnimsetRiderMountDismountPath, "Rider_Mount_Left");
            stateMachine.defaultState = mountedIdle;

            var toWalk = mountedIdle.AddTransition(mountedWalk);
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walking");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            var toIdle = mountedWalk.AddTransition(mountedIdle);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walking");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;

            var toDismount = stateMachine.AddAnyStateTransition(dismount);
            toDismount.AddCondition(AnimatorConditionMode.If, 0f, "Dismount");
            toDismount.hasExitTime = false;
            toDismount.duration = 0.06f;
            toDismount.canTransitionToSelf = false;
            var dismountComplete = dismount.AddTransition(onFootIdle);
            dismountComplete.hasExitTime = true;
            dismountComplete.exitTime = 0.98f;
            dismountComplete.duration = 0.05f;

            var beginOnFootWalk = onFootIdle.AddTransition(onFootWalk);
            beginOnFootWalk.AddCondition(AnimatorConditionMode.If, 0f, "OnFootWalking");
            beginOnFootWalk.hasExitTime = false;
            beginOnFootWalk.duration = 0.10f;
            var stopOnFootWalk = onFootWalk.AddTransition(onFootIdle);
            stopOnFootWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "OnFootWalking");
            stopOnFootWalk.hasExitTime = false;
            stopOnFootWalk.duration = 0.10f;

            var beginMount = onFootIdle.AddTransition(mount);
            beginMount.AddCondition(AnimatorConditionMode.If, 0f, "Mount");
            beginMount.hasExitTime = false;
            beginMount.duration = 0.06f;
            var mountComplete = mount.AddTransition(mountedIdle);
            mountComplete.hasExitTime = true;
            mountComplete.exitTime = 0.98f;
            mountComplete.duration = 0.05f;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Transform CreatePaymentBagAnchor(GameObject horseVisual)
        {
            var head = FindDescendant(horseVisual.transform, "Head");
            var jaw = FindDescendant(horseVisual.transform, "Jaw") ?? head;
            var leftBridlePoint = FindDescendant(horseVisual.transform, "Reins_Bn_Head_L");
            var rightBridlePoint = FindDescendant(horseVisual.transform, "Reins_Bn_Head_R");
            if (head == null || jaw == null)
            {
                throw new System.InvalidOperationException("Horse Animset head/jaw bones were not found for the payment bag.");
            }

            var bounds = GetEnabledRendererBounds(horseVisual);
            var mouthDirection = head.position - bounds.center;
            mouthDirection.y = 0f;
            mouthDirection = mouthDirection.sqrMagnitude < 0.001f ? horseVisual.transform.forward : mouthDirection.normalized;
            var anchor = new GameObject("PaymentBagAnchor_BridleLowerPoint").transform;
            // Use the package's ready bridle/rein attachment bones instead of the
            // mouth mesh. Their midpoint is the bit; lowering it keeps the knot
            // outside the lips and lets the pouch hang freely below the chin.
            var bridleMidpoint = leftBridlePoint != null && rightBridlePoint != null
                ? Vector3.Lerp(leftBridlePoint.position, rightBridlePoint.position, 0.5f)
                : head.position + (mouthDirection * 0.27f) - (Vector3.up * 0.22f);
            anchor.position = bridleMidpoint - (Vector3.up * PaymentPouchCordLength);
            anchor.rotation = Quaternion.LookRotation(mouthDirection, Vector3.up);
            anchor.SetParent(jaw, true);
            return anchor;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
        }

        private static Bounds GetEnabledRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
            {
                throw new System.InvalidOperationException("No enabled renderers found on " + root.name);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void ScaleEnabledRenderersToHeight(GameObject root, float desiredHeight)
        {
            var bounds = GetEnabledRendererBounds(root);
            root.transform.localScale *= desiredHeight / bounds.size.y;
        }

        private static void ApplyMountedHorseMaterial(GameObject mountedClient)
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MountedClientPath);
            var horseRenderer = mountedClient.GetComponentsInChildren<Renderer>()
                .FirstOrDefault(renderer => renderer.name == "Horse");
            var horseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MountedHorseTexturePath);
            if (sourcePrefab == null || horseRenderer == null || horseTexture == null)
            {
                throw new System.InvalidOperationException("Mounted horse material or texture is missing.");
            }

            var sourceMaterials = sourcePrefab.GetComponentsInChildren<Renderer>()
                .GroupBy(renderer => renderer.name)
                .ToDictionary(group => group.Key, group => group.First().sharedMaterials);
            foreach (var renderer in mountedClient.GetComponentsInChildren<Renderer>())
            {
                if (sourceMaterials.TryGetValue(renderer.name, out var materials))
                {
                    renderer.sharedMaterials = materials;
                }
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

        private static (GameObject gate, GameObject sack) CreateExitAndPayment()
        {
            var gate = InstantiateAsset(GatePath, "ExitGate_01");
            ScaleToHeight(gate, 2.2f);
            gate.transform.position = new Vector3(0f, 0f, -5.5f);
            gate.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            PlaceBaseOnGround(gate);
            AddMeshColliders(gate);

            // Keep the interaction root at the knot. The FBX visual is rotated and
            // offset beneath it, so attaching the root to the mouth makes the pouch
            // hang naturally instead of fastening its whole body to the horse.
            var sack = new GameObject("PaymentSack_01");
            var cord = new GameObject("PaymentPouchCord");
            cord.transform.SetParent(sack.transform, false);
            var cordRenderer = cord.AddComponent<LineRenderer>();
            cordRenderer.useWorldSpace = false;
            cordRenderer.positionCount = 2;
            cordRenderer.SetPosition(0, Vector3.zero);
            cordRenderer.SetPosition(1, Vector3.up * PaymentPouchCordLength);
            cordRenderer.startWidth = 0.018f;
            cordRenderer.endWidth = 0.014f;
            cordRenderer.numCapVertices = 6;
            cordRenderer.numCornerVertices = 4;
            cordRenderer.generateLightingData = true;
            cordRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cordRenderer.receiveShadows = false;
            cordRenderer.sharedMaterial = CreateHorseAnimsetMaterial(
                "M_PaymentPouchCord",
                new Color(0.12f, 0.045f, 0.018f),
                null,
                null,
                null);
            var sackVisual = InstantiateAsset(PaymentPouchPath, "PaymentPouchVisual");
            sackVisual.transform.SetParent(sack.transform, false);
            sackVisual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ScaleToHeight(sackVisual, 0.18f);
            var sackBounds = GetEnabledRendererBounds(sackVisual);
            sackVisual.transform.position += new Vector3(
                -sackBounds.center.x,
                -sackBounds.max.y,
                -sackBounds.center.z);
            sack.transform.position = new Vector3(0.8f, 0.2f, -5f);
            AddMeshColliders(sackVisual);
            return (gate, sack);
        }

        private static void CreateParkingRuntime(
            GameCompositionRoot compositionRoot,
            GameObject client,
            MonoBehaviour animationAdapter,
            Transform paymentBagAnchor,
            Transform riderRoot,
            Animator riderAnimator,
            Transform saddleParent,
            GameObject gate,
            GameObject sack,
            Collider[] parkingRouteObstacles)
        {
            var runtimeObject = new GameObject("ParkingMvpRuntime");
            var runtime = runtimeObject.AddComponent<ParkingMvpRuntimeController>();
            var entryLanePoint = new GameObject("ClientEntryLanePoint_01");
            entryLanePoint.transform.position = new Vector3(0f, groundSurfaceY, -5.5f);
            var parkingPoint = new GameObject("ClientParkingPoint_01");
            parkingPoint.transform.position = new Vector3(0f, groundSurfaceY, -1f);
            var paymentPoint = new GameObject("ClientPaymentPoint_01");
            paymentPoint.transform.position = new Vector3(0f, groundSurfaceY, -4.25f);
            var exitPoint = new GameObject("ClientExitPoint_01");
            exitPoint.transform.position = new Vector3(0f, groundSurfaceY, -14f);
            var riderDismountPoint = new GameObject("RiderMountPoint_01");
            riderDismountPoint.transform.position = new Vector3(-1.15f, groundSurfaceY, -1f);
            riderDismountPoint.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var riderLanePoint = new GameObject("RiderOpenLanePoint_01");
            // Centre of the unobstructed entrance between the two side fences.
            riderLanePoint.transform.position = new Vector3(0f, groundSurfaceY, -3.35f);
            riderLanePoint.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var riderAwayPoint = new GameObject("RiderAwayPoint_01");
            riderAwayPoint.transform.position = new Vector3(-4.5f, groundSurfaceY, -4.5f);
            var route = client.AddComponent<MountedClientRoutePresenter>();
            route.Configure(client.transform, entryLanePoint.transform, parkingPoint.transform, paymentPoint.transform, exitPoint.transform, animationAdapter, runtime.NotifyClientParked, runtime.NotifyClientAtPaymentGate, runtime.NotifyClientExited);
            var riderSequence = riderRoot.gameObject.AddComponent<RiderParkingSequencePresenter>();
            var dismountClip = LoadHorseAnimsetClip(HorseAnimsetRiderMountDismountPath, "Rider_Dismount_Left");
            var mountClip = LoadHorseAnimsetClip(HorseAnimsetRiderMountDismountPath, "Rider_Mount_Left");
            var terrainCollider = Terrain.activeTerrain != null
                ? Terrain.activeTerrain.GetComponent<TerrainCollider>()
                : null;
            if (terrainCollider == null)
            {
                throw new System.InvalidOperationException("Parking terrain collider is required for rider grounding.");
            }
            riderSequence.Configure(
                riderRoot,
                riderAnimator,
                saddleParent,
                riderDismountPoint.transform,
                riderLanePoint.transform,
                riderAwayPoint.transform,
                terrainCollider,
                parkingRouteObstacles.Concat(gate.GetComponentsInChildren<Collider>(true)).ToArray(),
                dismountClip.length + 0.12f,
                mountClip.length + 0.12f);
            runtime.Configure(compositionRoot, client, sack, route, paymentBagAnchor, riderSequence);

            var sackTarget = sack.AddComponent<ParkingPaymentBagInteractionTarget>();
            sackTarget.Configure(runtime);
            var gateTarget = gate.AddComponent<ParkingExitGateInteractionTarget>();
            gateTarget.Configure(runtime);
        }

        private static AnimatorController GetOrCreateVoxelHorseController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(VoxelHorseControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(VoxelHorseControllerPath);
            controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var stateMachine = controller.layers[0].stateMachine;
            var idle = stateMachine.AddState("Idle");
            idle.motion = GetFirstAnimationClip(VoxelHorseIdlePath);
            var walk = stateMachine.AddState("Walk");
            walk.motion = GetFirstAnimationClip(VoxelHorseWalkPath);
            stateMachine.defaultState = idle;
            var toWalk = idle.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walking");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.1f;
            var toIdle = walk.AddTransition(idle);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walking");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController GetOrCreateVoxelRiderController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(VoxelRiderControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(VoxelRiderControllerPath);
            var state = controller.layers[0].stateMachine.AddState("Mounted");
            state.motion = GetFirstAnimationClip(VoxelRiderMountedPath);
            controller.layers[0].stateMachine.defaultState = state;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip GetFirstAnimationClip(string path)
        {
            ConfigureLoopingAnimation(path);
            var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__"));
            if (clip == null)
            {
                throw new System.InvalidOperationException("No animation clip found in " + path);
            }

            return clip;
        }

        private static void ConfigureLoopingAnimation(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("Animation importer is missing: " + path);
            }

            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ApplyVoxelMaterial(GameObject root, string texturePath, string materialPath, string materialName)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(MountedHorseMaterialPath);
            if (texture == null || sourceMaterial == null)
            {
                throw new System.InvalidOperationException("Voxel texture or compatible Standard material is missing for " + materialName);
            }

            EnsureFolder("Assets/_Project/Content/Materials");
            EnsureFolder("Assets/_Project/Content/Materials/Characters");
            EnsureFolder("Assets/_Project/Content/Materials/Characters/VoxelKnights");
            AssetDatabase.DeleteAsset(materialPath);
            // This project currently renders its existing FBX assets through the built-in
            // Standard shader. Reusing that known-good material avoids magenta URP errors.
            var material = new Material(sourceMaterial) { name = materialName };
            material.mainTexture = texture;
            AssetDatabase.CreateAsset(material, materialPath);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        private static GameObject CreateParkingFence(string name, Vector3 position, float yRotation)
        {
            var fence = InstantiateAsset(GatePath, name);
            ScaleToHeight(fence, 1.25f);
            fence.transform.position = position;
            fence.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            PlaceBaseOnGround(fence);
            AddMeshColliders(fence);
            return fence;
        }

        private static (GameObject store, GameObject warehouse) CreateOperationsBuildings()
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
            return (store, warehouse);
        }

        private static void CreateStage32Logistics(
            GameCompositionRoot compositionRoot,
            FirstPersonPlayerController playerController,
            GameObject store,
            GameObject warehouse,
            LogisticsInventorySettings inventorySettings,
            GameLocalizationSettings localizationSettings)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                groundSurfaceY = terrain.transform.position.y + terrain.SampleHeight(Vector3.zero);
            }

            EnsureStage32Translations(localizationSettings);
            var oldRuntime = GameObject.Find("Stage32_LogisticsJourney");
            if (oldRuntime != null) Object.DestroyImmediate(oldRuntime);
            var oldUi = GameObject.Find("CartDispatchUI");
            if (oldUi != null) Object.DestroyImmediate(oldUi);
            var oldPushUi = GameObject.Find("CartPushUI");
            if (oldPushUi != null) Object.DestroyImmediate(oldPushUi);

            AddMeshColliders(store);
            AddMeshColliders(warehouse);
            RemoveMissingScripts(store);
            RemoveMissingScripts(warehouse);

            var warehouseTarget = CreateCartDispatchTarget(warehouse, CartStationKind.Warehouse);
            var storeTarget = CreateCartDispatchTarget(store, CartStationKind.MaterialStore);

            var runtimeRoot = new GameObject("Stage32_LogisticsJourney");
            var route = CreateDeliveryCartRoute(runtimeRoot.transform);
            CreateDeliveryCartVehicle(
                runtimeRoot.transform,
                compositionRoot,
                route,
                inventorySettings.CartTravelSpeedMetersPerSecond);

            var playerCamera = playerController.GetComponentInChildren<Camera>(true)
                ?? throw new System.InvalidOperationException("First-person camera is missing.");
            CreateStage32Ui(
                compositionRoot,
                playerController,
                playerCamera,
                warehouseTarget,
                storeTarget,
                inventorySettings.MaterialStoreId);
        }

        private static CartDispatchInteractionTarget CreateCartDispatchTarget(
            GameObject building,
            CartStationKind stationKind)
        {
            var target = building.GetComponent<CartDispatchInteractionTarget>()
                ?? building.AddComponent<CartDispatchInteractionTarget>();
            target.Configure(stationKind);
            return target;
        }

        private static Transform[] CreateDeliveryCartRoute(Transform parent)
        {
            var positions = new[]
            {
                new Vector3(7.2f, groundSurfaceY, -3.8f),
                new Vector3(7.2f, groundSurfaceY, -8f),
                new Vector3(-7.2f, groundSurfaceY, -8f),
                new Vector3(-7.2f, groundSurfaceY, -3.8f)
            };
            var names = new[]
            {
                "CartRoute_WarehouseDelivery",
                "CartRoute_WarehouseApproach",
                "CartRoute_StoreApproach",
                "CartRoute_MaterialStoreLoading"
            };
            var route = new Transform[positions.Length];
            for (var index = 0; index < positions.Length; index++)
            {
                route[index] = new GameObject(names[index]).transform;
                route[index].SetParent(parent, false);
                route[index].position = positions[index];
                Vector3 direction;
                if (index == 0)
                {
                    // The warehouse endpoint faces the direction in which the cart
                    // arrives from route[1], so stopping never introduces a 180 turn.
                    direction = positions[index] - positions[index + 1];
                }
                else if (index == positions.Length - 1)
                {
                    // The store endpoint faces the outbound arrival direction.
                    direction = positions[index] - positions[index - 1];
                }
                else
                {
                    direction = positions[index + 1] - positions[index];
                }
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    route[index].rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
            return route;
        }

        private static GameObject CreateDeliveryCartVehicle(
            Transform parent,
            GameCompositionRoot compositionRoot,
            Transform[] route,
            float travelSpeed)
        {
            ConfigureDeliveryCartImport();
            var vehicle = new GameObject("DeliveryCartVehicle_01");
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.SetPositionAndRotation(route[0].position, route[0].rotation);

            var cart = InstantiateAsset(DeliveryCartModelPath, "DeliveryCart_Visual");
            cart.transform.SetParent(vehicle.transform, false);
            var importedCartRotation = cart.transform.localRotation;
            cart.transform.localPosition = Vector3.zero;
            // Preserve only the FBX axis conversion. This hand-cart asset already
            // points its physical handles toward the pusher; an extra Y=180 placed
            // the wooden handles on the side opposite the driver's IK targets.
            cart.transform.localRotation = importedCartRotation;
            foreach (var renderer in cart.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = renderer.name.StartsWith(
                    "HandCart_",
                    System.StringComparison.OrdinalIgnoreCase);
            }
            ScaleToHorizontalLength(cart, 3.4f);
            PlaceVisualBaseAtVehicleGround(cart, vehicle.transform.position.y + 0.03f);
            ApplyDeliveryCartMaterials(cart);
            var cartAnimator = cart.GetComponentInChildren<Animator>(true);
            if (cartAnimator == null)
            {
                cartAnimator = cart.AddComponent<Animator>();
            }
            cartAnimator.runtimeAnimatorController = GetOrCreateDeliveryCartController();
            cartAnimator.applyRootMotion = false;
            cartAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var leftHandle = CreateCartGripTarget(
                vehicle.transform,
                "DeliveryCartLeftHandleGrip",
                new Vector3(-0.42f, 1.03f, -1.58f),
                new Vector3(0f, 0f, 88f));
            var rightHandle = CreateCartGripTarget(
                vehicle.transform,
                "DeliveryCartRightHandleGrip",
                new Vector3(0.42f, 1.03f, -1.58f),
                new Vector3(0f, 0f, -88f));
            var leftElbow = CreateCartGripTarget(
                vehicle.transform,
                "DeliveryCartLeftElbowHint",
                new Vector3(-0.68f, 1.28f, -1.92f),
                Vector3.zero);
            var rightElbow = CreateCartGripTarget(
                vehicle.transform,
                "DeliveryCartRightElbowHint",
                new Vector3(0.68f, 1.28f, -1.92f),
                Vector3.zero);

            ConfigureDeliveryCartDriverImport();
            var pushPoint = new GameObject("DeliveryCartDriverPushPoint").transform;
            pushPoint.SetParent(vehicle.transform, false);
            pushPoint.localPosition = new Vector3(0f, 0f, -2.12f);
            pushPoint.localRotation = Quaternion.identity;

            var driver = InstantiateAsset(CartDriverModelPath, "DeliveryCartDriver_01");
            driver.transform.SetParent(pushPoint, false);
            RemoveMissingScripts(driver);
            foreach (var renderer in driver.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = renderer.name.IndexOf(
                    "civilian",
                    System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
            ApplyDeliveryCartDriverMaterials(driver);
            ScaleEnabledRenderersToHeight(driver, 1.72f);
            driver.transform.localPosition = Vector3.zero;
            driver.transform.localRotation = Quaternion.identity;
            PlaceVisualBaseAtVehicleGround(driver, vehicle.transform.position.y + 0.03f);
            var driverAnimator = driver.GetComponent<Animator>();
            if (driverAnimator == null)
            {
                driverAnimator = driver.AddComponent<Animator>();
            }
            driverAnimator.runtimeAnimatorController = GetOrCreateDeliveryCartDriverController();
            driverAnimator.applyRootMotion = false;
            driverAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var handleIk = driver.GetComponent<CartDriverHandleIk>()
                ?? driver.AddComponent<CartDriverHandleIk>();
            handleIk.Configure(leftHandle, rightHandle, leftElbow, rightElbow);

            var visualAdapter = vehicle.AddComponent<DeliveryCartVisualAdapter>();
            visualAdapter.Configure(cartAnimator, driverAnimator);
            var woodCargoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WoodCargoModelPath);
            var stoneCargoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoneCargoModelPath);
            var ironCargoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IronCargoModelPath);
            visualAdapter.ConfigureCargoPrefabs(woodCargoPrefab, stoneCargoPrefab, ironCargoPrefab);
            visualAdapter.ConfigureCargoVisuals(
                CreateDeliveryCartCargoVisuals(
                    vehicle.transform, cart, woodCargoPrefab, "DeliveryCartCargo_Wood",
                    new[] { new Vector2(-0.34f, 0.02f), new Vector2(0.34f, 0.02f) }, 0.62f),
                CreateDeliveryCartCargoVisuals(
                    vehicle.transform, cart, stoneCargoPrefab, "DeliveryCartCargo_Stone",
                    new[] { new Vector2(-0.34f, 0.38f), new Vector2(0.34f, 0.38f) }, 0.52f),
                CreateDeliveryCartCargoVisuals(
                    vehicle.transform, cart, ironCargoPrefab, "DeliveryCartCargo_Iron",
                    new[] { new Vector2(-0.34f, -0.32f), new Vector2(0.34f, -0.32f) }, 0.54f));
            var journeyPresenter = vehicle.AddComponent<DeliveryCartJourneyPresenter>();
            journeyPresenter.Configure(
                compositionRoot,
                vehicle.transform,
                route,
                visualAdapter,
                travelSpeed);
            return vehicle;
        }

        private static Transform CreateCartGripTarget(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            var target = new GameObject(name).transform;
            target.SetParent(parent, false);
            target.localPosition = localPosition;
            target.localRotation = Quaternion.Euler(localEulerAngles);
            return target;
        }

        private static GameObject[] CreateDeliveryCartCargoVisuals(
            Transform vehicle,
            GameObject cart,
            GameObject prefab,
            string namePrefix,
            Vector2[] localPositions,
            float targetHeight)
        {
            if (prefab == null)
            {
                throw new System.InvalidOperationException("Delivery cart cargo prefab is missing: " + namePrefix);
            }

            var results = new GameObject[localPositions.Length];
            var cartBounds = GetBounds(cart);
            for (var index = 0; index < localPositions.Length; index++)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.name = namePrefix + "_" + (index + 1);
                visual.transform.SetParent(vehicle, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(collider);
                }
                foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true))
                {
                    Object.DestroyImmediate(body);
                }
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                ScaleToHeight(visual, targetHeight);

                var desiredBottomCenter = vehicle.TransformPoint(new Vector3(
                    localPositions[index].x,
                    0f,
                    localPositions[index].y));
                desiredBottomCenter.y = cartBounds.max.y - 0.2f;
                var visualBounds = GetBounds(visual);
                visual.transform.position += new Vector3(
                    desiredBottomCenter.x - visualBounds.center.x,
                    desiredBottomCenter.y - visualBounds.min.y,
                    desiredBottomCenter.z - visualBounds.center.z);
                visual.SetActive(false);
                results[index] = visual;
            }
            return results;
        }

        private static void SetRoutePoint(string name, Vector3 position)
        {
            var routePoint = GameObject.Find(name)
                ?? throw new System.InvalidOperationException("ParkingMvp is missing route point " + name + ".");
            routePoint.transform.position = position;
            EditorUtility.SetDirty(routePoint.transform);
        }

        private static void ConfigureDeliveryCartDriverImport()
        {
            var importer = AssetImporter.GetAtPath(CartDriverModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException(
                    "Downloaded cart driver FBX is missing: " + CartDriverModelPath);
            }

            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            ConfigureHorseAnimsetRiderImports();
        }

        private static void ApplyDeliveryCartDriverMaterials(GameObject driver)
        {
            EnsureFolder(CartDriverMaterialFolder);
            var slots = new[] { "Body", "Bottom", "Moustache", "Shoes", "Top", "Beard", "Hair" };
            var materials = slots.ToDictionary(
                slot => slot,
                CreateDeliveryCartDriverMaterial,
                System.StringComparer.OrdinalIgnoreCase);

            foreach (var renderer in driver.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                var assigned = renderer.sharedMaterials;
                for (var index = 0; index < assigned.Length; index++)
                {
                    var importedName = assigned[index] != null ? assigned[index].name : string.Empty;
                    if (materials.TryGetValue(importedName, out var material))
                    {
                        assigned[index] = material;
                    }
                    else if (index < slots.Length)
                    {
                        assigned[index] = materials[slots[index]];
                    }
                }
                renderer.sharedMaterials = assigned;
            }
        }

        private static Material CreateDeliveryCartDriverMaterial(string slot)
        {
            var materialPath = CartDriverMaterialFolder + "/M_CartDriver_" + slot + ".mat";
            AssetDatabase.DeleteAsset(materialPath);
            var shader = Shader.Find("Standard")
                ?? throw new System.InvalidOperationException("Built-in Standard shader is unavailable.");
            var material = new Material(shader) { name = "M_CartDriver_" + slot };
            var texturePrefix = CartDriverTextureFolder + "/civilian 3_" + slot;
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePrefix + "_diffuse.png");
            var normalPath = texturePrefix + "_normal.png";
            ConfigureNormalTexture(normalPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (diffuse != null)
            {
                material.SetTexture("_MainTex", diffuse);
            }
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Glossiness", 0.2f);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static void ConfigureNormalTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController GetOrCreateDeliveryCartDriverController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(DeliveryCartDriverControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(DeliveryCartDriverControllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            var stateMachine = controller.layers[0].stateMachine;
            var layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;
            var idle = stateMachine.AddState("DriverPushIdle");
            idle.motion = LoadHorseAnimsetClip(HorseAnimsetRiderOnFootIdlePath, "S_Idle");
            var walk = stateMachine.AddState("DriverPushWalk");
            walk.motion = LoadHorseAnimsetClip(HorseAnimsetRiderOnFootWalkPath, "S_Walk_F");
            stateMachine.defaultState = idle;

            var beginWalk = idle.AddTransition(walk);
            beginWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            beginWalk.hasExitTime = false;
            beginWalk.duration = 0.10f;
            var stopWalk = walk.AddTransition(idle);
            stopWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            stopWalk.hasExitTime = false;
            stopWalk.duration = 0.10f;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }
        }

        private static void ConfigureDeliveryCartImport()
        {
            var importer = AssetImporter.GetAtPath(DeliveryCartModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("Converted delivery cart FBX is missing: " + DeliveryCartModelPath);
            }

            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                clip.keepOriginalPositionXZ = true;
                clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = true;
                clip.lockRootHeightY = true;
                clip.lockRootRotation = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController GetOrCreateDeliveryCartController()
        {
            EnsureFolder("Assets/_Project/Presentation/Animation");
            EnsureFolder("Assets/_Project/Presentation/Animation/Controllers");
            AssetDatabase.DeleteAsset(DeliveryCartControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(DeliveryCartControllerPath);
            var clip = AssetDatabase.LoadAllAssetsAtPath(DeliveryCartModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => candidate.name.Equals(
                    "Cart_Pull",
                    System.StringComparison.OrdinalIgnoreCase)
                    && candidate.length > 1f)
                ?? AssetDatabase.LoadAllAssetsAtPath(DeliveryCartModelPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(candidate => candidate.name.IndexOf(
                        "Cart_Pull",
                        System.StringComparison.OrdinalIgnoreCase) >= 0
                        && candidate.length > 0.1f);
            if (clip == null)
            {
                throw new System.InvalidOperationException("Hand push cart FBX has no Cart_Pull animation clip.");
            }

            var state = controller.layers[0].stateMachine.AddState("CartPull");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ApplyDeliveryCartMaterials(GameObject cart)
        {
            EnsureFolder(DeliveryCartMaterialFolder);
            ConfigureNormalTexture(DeliveryCartWoodNormalPath);
            ConfigureNormalTexture(DeliveryCartFabricNormalPath);
            var wood = CreateDeliveryCartMaterial(
                "M_DeliveryCartWood",
                DeliveryCartWoodDiffusePath,
                DeliveryCartWoodNormalPath);
            var fabric = CreateDeliveryCartMaterial(
                "M_DeliveryCartFabric",
                DeliveryCartFabricDiffusePath,
                DeliveryCartFabricNormalPath);
            foreach (var renderer in cart.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    materials[index] = materials[index] != null
                        && materials[index].name.IndexOf(
                            "Cloth",
                            System.StringComparison.OrdinalIgnoreCase) >= 0
                            ? fabric
                            : wood;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material CreateDeliveryCartMaterial(string name, string diffusePath, string normalPath)
        {
            var path = DeliveryCartMaterialFolder + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            var shader = Shader.Find("Standard")
                ?? throw new System.InvalidOperationException("Built-in Standard shader is unavailable.");
            var material = new Material(shader) { name = name };
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Glossiness", 0.18f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void PlaceVisualBaseAtVehicleGround(GameObject visual, float surfaceY)
        {
            var bounds = GetEnabledRendererBounds(visual);
            visual.transform.position += Vector3.up * (surfaceY - bounds.min.y);
        }

        private static void CreateStage32Ui(
            GameCompositionRoot compositionRoot,
            FirstPersonPlayerController playerController,
            Camera playerCamera,
            CartDispatchInteractionTarget warehouseTarget,
            CartDispatchInteractionTarget storeTarget,
            string materialStoreId)
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject(
                "CartDispatchUI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var objectivePanel = new GameObject(
                "CartObjectivePanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            objectivePanel.transform.SetParent(canvasObject.transform, false);
            var objectiveRect = objectivePanel.GetComponent<RectTransform>();
            objectiveRect.anchorMin = objectiveRect.anchorMax = objectiveRect.pivot = new Vector2(0.5f, 1f);
            objectiveRect.anchoredPosition = new Vector2(0f, -18f);
            objectiveRect.sizeDelta = new Vector2(1120f, 86f);
            objectivePanel.GetComponent<Image>().color = new Color(0.04f, 0.025f, 0.015f, 0.9f);
            var prompt = CreateCentredText(
                objectivePanel.transform,
                "InteractionPrompt",
                font,
                28,
                Vector2.zero,
                new Vector2(1080f, 74f));
            prompt.alignment = TextAnchor.MiddleCenter;
            var promptHud = canvasObject.AddComponent<InteractionPromptHud>();
            promptHud.Configure(playerCamera, compositionRoot, prompt, 3f);

            var panelRoot = new GameObject("CartDispatchPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 500f);
            panelRoot.GetComponent<Image>().color = new Color(0.035f, 0.02f, 0.012f, 0.98f);

            var title = CreateCentredText(panelRoot.transform, "Title", font, 38, new Vector2(0f, 195f), new Vector2(760f, 58f));
            var station = CreateCentredText(panelRoot.transform, "Station", font, 30, new Vector2(0f, 125f), new Vector2(740f, 52f));
            var status = CreateCentredText(panelRoot.transform, "Status", font, 28, new Vector2(0f, 72f), new Vector2(740f, 48f));
            var instruction = CreateCentredText(panelRoot.transform, "Instruction", font, 25, new Vector2(0f, 10f), new Vector2(720f, 78f));
            instruction.horizontalOverflow = HorizontalWrapMode.Wrap;
            instruction.verticalOverflow = VerticalWrapMode.Overflow;
            var action = CreateUiButton(panelRoot.transform, "ActionButton", font, new Vector2(0f, -92f), new Vector2(620f, 70f));
            var close = CreateUiButton(panelRoot.transform, "CloseButton", font, new Vector2(0f, -190f), new Vector2(280f, 58f));

            var dispatchPanel = canvasObject.AddComponent<CartDispatchPanel>();
            dispatchPanel.Configure(
                compositionRoot,
                playerController,
                warehouseTarget,
                storeTarget,
                panelRoot,
                title,
                station,
                status,
                instruction,
                action.button,
                action.label,
                close.button,
                close.label,
                materialStoreId);
        }

        private static (Button button, Text label) CreateUiButton(
            Transform parent,
            string name,
            Font font,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.55f, 0.29f, 0.08f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var label = CreateCentredText(buttonObject.transform, "Label", font, 26, Vector2.zero, size - new Vector2(20f, 10f));
            return (button, label);
        }

        private static Text CreateCentredText(
            Transform parent,
            string name,
            Font font,
            int fontSize,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = new Color(1f, 0.96f, 0.82f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static void EnsureStage32Translations(GameLocalizationSettings settings)
        {
            var manualKeys = new[]
            {
                "interaction.cart.push",
                "interaction.cart.release",
                "interaction.cart.target",
                "interaction.cart.grabbed",
                "interaction.cart.released",
                "interaction.cart.unavailable"
            };
            foreach (var locale in new[] { "ru", "en" })
            {
                foreach (var key in manualKeys)
                {
                    settings.RemoveTranslation(locale, key);
                }
            }

            var entries = new[]
            {
                ("ru", "ui.cart.status", "Телега: {state}"),
                ("ru", "cart.state.at_warehouse", "на складе"),
                ("ru", "cart.state.traveling_to_store", "едет в магазин"),
                ("ru", "cart.state.at_store", "у магазина"),
                ("ru", "cart.state.returning_to_warehouse", "возвращается на склад"),
                ("ru", "cart.state.unknown", "неизвестно"),
                ("ru", "ui.cart_dispatch.title", "Управление телегой"),
                ("ru", "ui.cart_dispatch.current_station", "Вы у: {station}"),
                ("ru", "ui.cart_dispatch.send_to_store", "Отправить в магазин материалов"),
                ("ru", "ui.cart_dispatch.return_to_warehouse", "Доставить товар на склад"),
                ("ru", "ui.cart_dispatch.instruction.ready_warehouse", "Телега стоит рядом со складом и пока пуста. Со склада сейчас ничего грузить не нужно: отправьте телегу в магазин. Покупка и загрузка будут на следующем этапе."),
                ("ru", "ui.cart_dispatch.instruction.ready_store", "Телега стоит у рынка в точке погрузки. На этапе 3.2 нажмите кнопку, чтобы проверить доставку к складу; покупка и видимый груз появятся на этапе 3.3."),
                ("ru", "ui.cart_dispatch.instruction.go_warehouse", "Телега находится НА СКЛАДЕ. Закройте окно и подойдите к высокой постройке с краном справа."),
                ("ru", "ui.cart_dispatch.instruction.go_store", "Телега находится У МАГАЗИНА. Закройте окно и подойдите к лавке с товарами слева."),
                ("ru", "ui.cart_dispatch.instruction.in_transit", "Телега уже в пути. Дождитесь её прибытия."),
                ("ru", "ui.common.close", "Закрыть"),
                ("ru", "location.warehouse", "Склад"),
                ("ru", "location.material_store", "Магазин материалов"),
                ("ru", "interaction.cart.manage", "Управлять телегой"),
                ("ru", "interaction.cart.opened", "Управление телегой открыто"),
                ("ru", "ui.interaction.prompt", "[E] {action}: {target}"),
                ("ru", "ui.cart.guide.warehouse", "ТОЧКА ДОСТАВКИ: СКЛАД — высокая постройка с краном справа."),
                ("ru", "ui.cart.guide.store", "ТЕЛЕГА У РЫНКА МАТЕРИАЛОВ — возница стоит за рукоятками. Подойдите к лавке и нажмите E, чтобы отправить её к складу."),
                ("ru", "ui.cart.world_label", "ТЕЛЕГА"),
                ("en", "ui.cart.status", "Cart: {state}"),
                ("en", "cart.state.at_warehouse", "at warehouse"),
                ("en", "cart.state.traveling_to_store", "traveling to material store"),
                ("en", "cart.state.at_store", "at material store"),
                ("en", "cart.state.returning_to_warehouse", "returning to warehouse"),
                ("en", "cart.state.unknown", "unknown"),
                ("en", "ui.cart_dispatch.title", "Cart management"),
                ("en", "ui.cart_dispatch.current_station", "You are at: {station}"),
                ("en", "ui.cart_dispatch.send_to_store", "Send to material store"),
                ("en", "ui.cart_dispatch.return_to_warehouse", "Deliver goods to warehouse"),
                ("en", "ui.cart_dispatch.instruction.ready_warehouse", "The empty cart is beside the warehouse. Nothing is loaded here yet: send it to the store. Purchasing and loading are handled in the next stage."),
                ("en", "ui.cart_dispatch.instruction.ready_store", "The cart is parked at the market loading point. In Stage 3.2, use the button to verify delivery to the warehouse; purchasing and visible cargo arrive in Stage 3.3."),
                ("en", "ui.cart_dispatch.instruction.go_warehouse", "The cart is AT THE WAREHOUSE. Close this window and approach the tall crane building on the right."),
                ("en", "ui.cart_dispatch.instruction.go_store", "The cart is AT THE STORE. Close this window and approach the market stall on the left."),
                ("en", "ui.cart_dispatch.instruction.in_transit", "The cart is already traveling. Wait for it to arrive."),
                ("en", "ui.common.close", "Close"),
                ("en", "location.warehouse", "Warehouse"),
                ("en", "location.material_store", "Material store"),
                ("en", "interaction.cart.manage", "Manage cart"),
                ("en", "interaction.cart.opened", "Cart management opened"),
                ("en", "ui.interaction.prompt", "[E] {action}: {target}"),
                ("en", "ui.cart.guide.warehouse", "DELIVERY POINT: WAREHOUSE — the tall crane building on the right."),
                ("en", "ui.cart.guide.store", "CART AT MATERIAL MARKET — the driver is behind its handles. Approach the stall and press E to send it to the warehouse."),
                ("en", "ui.cart.world_label", "CART")
            };
            foreach (var entry in entries)
            {
                settings.EnsureTranslation(entry.Item1, entry.Item2, entry.Item3);
            }
            EditorUtility.SetDirty(settings);
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

        private static LogisticsInventorySettings LoadOrCreateLogisticsInventorySettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LogisticsInventorySettings>(LogisticsInventorySettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder("Assets/_Project/Settings");
            settings = ScriptableObject.CreateInstance<LogisticsInventorySettings>();
            AssetDatabase.CreateAsset(settings, LogisticsInventorySettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static GameLocalizationSettings LoadOrCreateGameLocalizationSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameLocalizationSettings>(GameLocalizationSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder("Assets/_Project/Settings");
            settings = ScriptableObject.CreateInstance<GameLocalizationSettings>();
            AssetDatabase.CreateAsset(settings, GameLocalizationSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void CreateLogisticsInventoryHud(GameCompositionRoot compositionRoot)
        {
            var existing = GameObject.Find("LogisticsInventoryHUD");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var canvasObject = new GameObject("LogisticsInventoryHUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var panelObject = new GameObject("LogisticsInventoryPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(360f, 354f);
            panelObject.GetComponent<Image>().color = new Color(0.10f, 0.065f, 0.035f, 0.88f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var headerText = CreateHudText(panelObject.transform, "Title", font, 24, FontStyle.Bold, new Vector2(16f, -12f), new Vector2(328f, 34f));
            headerText.alignment = TextAnchor.MiddleLeft;

            var warehouseText = CreateHudText(panelObject.transform, "WarehouseInventory", font, 18, FontStyle.Normal, new Vector2(16f, -54f), new Vector2(328f, 112f));
            var cartText = CreateHudText(panelObject.transform, "CartInventory", font, 18, FontStyle.Normal, new Vector2(16f, -178f), new Vector2(328f, 112f));
            var journeyText = CreateHudText(panelObject.transform, "CartJourneyStatus", font, 18, FontStyle.Bold, new Vector2(16f, -302f), new Vector2(328f, 34f));

            var hud = canvasObject.AddComponent<LogisticsInventoryHud>();
            hud.Configure(compositionRoot, headerText, warehouseText, cartText, journeyText);
        }

        private static Text CreateHudText(
            Transform parent,
            string name,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = new Color(0.96f, 0.88f, 0.68f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;

            var shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return text;
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

        private static void PlaceHorseFeetOnTerrain(GameObject mountedClient)
        {
            var horseRenderers = mountedClient.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.enabled && (renderer.name == "Horse Body" || renderer.name.Contains("Horse")))
                .ToArray();
            if (horseRenderers.Length == 0)
            {
                throw new System.InvalidOperationException("Mounted client is missing the Horse renderer.");
            }

            var hoofBaselineY = horseRenderers.Min(renderer => renderer.bounds.min.y);
            mountedClient.transform.position += Vector3.up * (groundSurfaceY - hoofBaselineY + 0.02f);
        }

        private static TerrainLayer CreateGroundTerrainLayer()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTexturePath);
            if (texture == null)
            {
                throw new System.InvalidOperationException("Terrain texture is missing: " + TerrainTexturePath);
            }

            var terrainLayer = new TerrainLayer
            {
                diffuseTexture = texture,
                tileSize = new Vector2(4f, 4f)
            };
            AssetDatabase.CreateAsset(terrainLayer, TerrainLayerPath);
            return terrainLayer;
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
