using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class WindowWallBuilder
    {
        static WindowWallBuilder() { EditorApplication.update += Poll; }

        static void Poll()
        {
            const string request = "Temp/FlameBlame-WindowWall";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            File.Delete(request);
            try { Apply(); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Logs/window-wall.txt", e.ToString()); }
        }

        [MenuItem("Tools/FlameBlame/Replace West Wall With Window")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Main.unity") throw new InvalidOperationException("Open Main first.");
            var wall = GameObject.Find("Room/Wall_W");
            if (wall == null) throw new InvalidOperationException("Room/Wall_W is missing.");
            Convert(wall);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Main.");
            // A ray normal to the wall must pass through the opening, but hit the sill.
            Physics.SyncTransforms();
            Vector3 normal = wall.transform.right;
            Vector3 center = wall.transform.TransformPoint(new Vector3(0, .05f, 0));
            Vector3 sill = wall.transform.TransformPoint(new Vector3(0, -.4f, 0));
            foreach (var collider in wall.GetComponentsInChildren<BoxCollider>())
            {
                if (collider.Raycast(new Ray(center - normal * 2, normal), out _, 4))
                    throw new InvalidOperationException("A wall collider blocks the window.");
            }
            bool sillHit = false;
            foreach (var collider in wall.GetComponentsInChildren<BoxCollider>())
                sillHit |= collider.Raycast(new Ray(sill - normal * 2, normal), out _, 4);
            if (!sillHit) throw new InvalidOperationException("Sill collision is missing.");
            File.WriteAllText("Logs/window-wall.txt", "Wall_W: four cube sections; window ray clear; sill collision verified.");
            Capture(wall.transform);
        }

        static void Capture(Transform wall)
        {
            var go = new GameObject("Window Preview");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = wall.TransformPoint(new Vector3(16, .05f, 0));
            camera.transform.LookAt(wall.TransformPoint(new Vector3(0, .05f, 0)));
            camera.nearClipPlane = .03f;
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                Directory.CreateDirectory("Captures");
                File.WriteAllBytes("Captures/West-wall-window.png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void Convert(GameObject wall)
        {
            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer == null) return; // Already converted; retain manual section adjustments.
            // Unit cube coordinates preserve the existing wall's position, rotation and size.
            // Opening: 45% of wall width, from 30% to 80% of wall height.
            var material = renderer.sharedMaterial;
            Section(wall, "Wall_W_Left", new Vector3(0, 0, -.3625f), new Vector3(1, 1, .275f), renderer, material);
            Section(wall, "Wall_W_Right", new Vector3(0, 0, .3625f), new Vector3(1, 1, .275f), renderer, material);
            Section(wall, "Wall_W_Below", new Vector3(0, -.35f, 0), new Vector3(1, .3f, .45f), renderer, material);
            Section(wall, "Wall_W_Above", new Vector3(0, .4f, 0), new Vector3(1, .2f, .45f), renderer, material);
            foreach (var collider in wall.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            UnityEngine.Object.DestroyImmediate(wall.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(renderer);
        }

        static void Section(GameObject wall, string name, Vector3 position, Vector3 scale, MeshRenderer source, Material material)
        {
            var section = GameObject.CreatePrimitive(PrimitiveType.Cube);
            section.name = name;
            section.transform.SetParent(wall.transform, false);
            section.transform.localPosition = position;
            section.transform.localScale = scale;
            section.layer = wall.layer;
            GameObjectUtility.SetStaticEditorFlags(section, GameObjectUtility.GetStaticEditorFlags(wall));
            var renderer = section.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            renderer.renderingLayerMask = source.renderingLayerMask;
        }
    }
}
