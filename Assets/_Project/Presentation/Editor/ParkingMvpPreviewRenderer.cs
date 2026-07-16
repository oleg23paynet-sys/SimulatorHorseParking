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
        private const string HorseWalkPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/H_Walk.FBX";
        private const string RiderWalkPath = "Assets/_Project/ThirdParty/HorseAnimsetPro/Rider/Rider_Walk.FBX";

        public static void RenderPreview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            AnimationMode.StartAnimationMode();
            var horse = GameObject.Find("HorseAnimsetPro_Visual");
            if (horse != null)
            {
                var walk = AssetDatabase.LoadAllAssetsAtPath(HorseWalkPath)
                    .OfType<AnimationClip>()
                    .First(clip => !clip.name.StartsWith("__preview__"));
                AnimationMode.SampleAnimationClip(horse, walk, walk.length * 0.35f);
            }

            var rider = GameObject.Find("ClientRider_01");
            if (rider != null)
            {
                var walk = AssetDatabase.LoadAllAssetsAtPath(RiderWalkPath)
                    .OfType<AnimationClip>()
                    .First(clip => !clip.name.StartsWith("__preview__"));
                AnimationMode.SampleAnimationClip(rider, walk, walk.length * 0.35f);
            }

            var bag = GameObject.Find("PaymentSack_01");
            var bagAnchor = GameObject.Find("PaymentBagAnchor_Mouth");
            if (bag != null && bagAnchor != null)
            {
                bag.transform.SetParent(bagAnchor.transform, true);
                bag.transform.position = bagAnchor.transform.position;
                bag.transform.rotation = bagAnchor.transform.rotation;
                bag.SetActive(true);
            }

            var previewCamera = new GameObject("__PreviewCamera").AddComponent<Camera>();
            previewCamera.transform.position = new Vector3(4.1f, 1.9f, -11.6f);
            previewCamera.transform.LookAt(new Vector3(0f, 1.35f, -12f));
            previewCamera.fieldOfView = 45f;
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
