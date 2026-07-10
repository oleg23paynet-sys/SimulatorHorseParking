using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorseParking.Presentation.Editor
{
    public static class BootstrapSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [MenuItem("Horse Parking/Build Bootstrap Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            var compositionRoot = CreateCompositionRoot();
            CreatePlayer(compositionRoot);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Bootstrap scene created at " + ScenePath);
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static GameCompositionRoot CreateCompositionRoot()
        {
            return new GameObject("GameCompositionRoot").AddComponent<GameCompositionRoot>();
        }

        private static void CreatePlayer(GameCompositionRoot compositionRoot)
        {
            var player = new GameObject("Player_FirstPerson_NoHands");
            player.transform.position = new Vector3(0f, 1.1f, -3f);

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

    }
}
