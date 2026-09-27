using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HorseParking.Presentation.Editor
{
    public static class GoblinEmergenceInstaller
    {
        const string PrefabPath = "Assets/_Project/Content/VFX/GoblinSpawn/Prefabs/VFX_GoblinSpawnVortex.prefab";
        public static void Install()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var swirl = root.GetComponent<ParticleSystem>();
                swirl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = swirl.main;
                main.loop = true; main.prewarm = false; main.playOnAwake = false;
                main.startLifetime = .35f; main.startSpeed = 0f; main.startSize3D = false; main.startSize = 1.8f;
                main.startRotation3D = false; main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.gravityModifier = 0f; main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.maxParticles = 12;
                var velocity = swirl.velocityOverLifetime; velocity.enabled = false;
                var force = swirl.forceOverLifetime; force.enabled = false;
                var noise = swirl.noise; noise.enabled = false;
                var shape = swirl.shape; shape.enabled = false;
                var emission = swirl.emission; emission.enabled = true; emission.rateOverTime = 14f; emission.SetBursts(new ParticleSystem.Burst[0]);
                var rotation = swirl.rotationOverLifetime; rotation.enabled = true; rotation.z = 4f;
                var size = swirl.sizeOverLifetime; size.enabled = false;
                var renderer = swirl.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.Local;
                // The existing prefab is oriented -90 degrees around X: its XY plane lies on the ground.
                renderer.maxParticleSize = 1f;
                var old = root.transform.Find("GroundDebris");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var debris = new GameObject("GroundDebris", typeof(ParticleSystem));
                debris.transform.SetParent(root.transform, false);
                var rocks = debris.GetComponent<ParticleSystem>();
                rocks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var rockMain = rocks.main;
                rockMain.loop = true; rockMain.playOnAwake = false; rockMain.prewarm = false;
                rockMain.simulationSpace = ParticleSystemSimulationSpace.World;
                rockMain.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .8f);
                rockMain.startSpeed = new ParticleSystem.MinMaxCurve(2.3f, 3.8f);
                rockMain.startSize = new ParticleSystem.MinMaxCurve(.07f, .16f);
                rockMain.gravityModifier = 1.3f; rockMain.maxParticles = 64;
                rockMain.startRotation3D = true;
                rockMain.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6f);
                rockMain.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6f);
                var rockEmission = rocks.emission; rockEmission.rateOverTime = 36f;
                var rockShape = rocks.shape; rockShape.enabled = true; rockShape.shapeType = ParticleSystemShapeType.Cone;
                rockShape.angle = 58f; rockShape.radius = .32f; rockShape.radiusThickness = .8f;
                var spin = rocks.rotationOverLifetime; spin.enabled = true; spin.separateAxes = true; spin.x = 5f; spin.y = 7f; spin.z = 3f;
                var collision = rocks.collision; collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D; collision.collidesWith = 1;
                collision.bounce = .2f; collision.dampen = .5f; collision.lifetimeLoss = .25f;
                var rockModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/Assets/fbx(unity)/decoration/nature/rock_single_A.fbx");
                var mesh = rockModel.GetComponentInChildren<MeshFilter>().sharedMesh;
                var rockRenderer = rocks.GetComponent<ParticleSystemRenderer>();
                rockRenderer.renderMode = ParticleSystemRenderMode.Mesh; rockRenderer.mesh = mesh;
                rockMain.startSize = new ParticleSystem.MinMaxCurve(.07f / mesh.bounds.size.magnitude, .20f / mesh.bounds.size.magnitude);
                const string matPath = "Assets/_Project/Content/VFX/GoblinSpawn/Materials/GroundDebris.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, matPath); }
                material.SetColor("_BaseColor", new Color(.23f, .20f, .15f)); material.SetFloat("_Smoothness", .05f);
                EditorUtility.SetDirty(material); rockRenderer.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("GOBLIN_EMERGENCE_INSTALLED: ground-aligned swirl and continuous ballistic stone emission.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
