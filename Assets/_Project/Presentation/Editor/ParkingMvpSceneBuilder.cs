using System.Linq;
using System.Collections.Generic;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using HorseParking.Presentation.Parking;
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
            CreatePlayer(compositionRoot);
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
