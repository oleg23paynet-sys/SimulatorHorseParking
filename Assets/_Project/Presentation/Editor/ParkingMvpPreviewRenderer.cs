using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorseParking.Presentation.Editor
{
    public static class ParkingMvpPreviewRenderer
    {
        private const string ScenePath = "Assets/_Project/Scenes/ParkingMvp.unity";
        private const string PreviewPath = "Docs/ParkingMvp_Preview.png";
        private const string RiderAnimationPath = "Assets/_Project/Content/Animations/Characters/Rider/MedievalRider/SeatedIdle_Source.fbx";

        public static void RenderPreview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            AnimationMode.StartAnimationMode();
            var rider = GameObject.Find("ClientRider_01");
            if (rider != null)
            {
                var seatedIdle = AssetDatabase.LoadAllAssetsAtPath(RiderAnimationPath)
                    .OfType<AnimationClip>()
                    .First(clip => !clip.name.StartsWith("__preview__"));
                AnimationMode.SampleAnimationClip(rider, seatedIdle, 0f);
            }
            var previewCamera = new GameObject("__PreviewCamera").AddComponent<Camera>();
            previewCamera.transform.position = new Vector3(5f, 3.5f, -6f);
            previewCamera.transform.LookAt(new Vector3(0f, 1.1f, -1f));
            previewCamera.fieldOfView = 52f;
            previewCamera.clearFlags = CameraClearFlags.Skybox;

            var texture = new RenderTexture(1280, 720, 24);
            previewCamera.targetTexture = texture;
            previewCamera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(PreviewPath, image.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(previewCamera.gameObject);
            AnimationMode.StopAnimationMode();
            Debug.Log("Parking MVP preview saved to " + PreviewPath);
        }
    }
}
