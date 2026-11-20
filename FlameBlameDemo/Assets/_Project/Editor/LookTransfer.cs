using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FlameBlame.Feedback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class LookTransfer
    {
        const string Settings = "Assets/_Project/Settings/";
        const string Backup = Settings + "LookBackup/";
        const string Main = "Assets/_Project/Scenes/Main.unity";
        [Serializable] public class AssetPair { public string source, backup, originalName; }
        [Serializable] public class Manifest { public int antiAliasing; public List<AssetPair> assets = new List<AssetPair>(); }

        static LookTransfer() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists("Temp/FlameBlame-RestoreLook"))
            {
                File.Delete("Temp/FlameBlame-RestoreLook");
                try { Restore(); CapturePair("Before"); File.WriteAllText("Logs/look-restore.txt", "PASS: user baseline restored and recaptured with full camera stack."); }
                catch (Exception e) { File.WriteAllText("Logs/look-restore.txt", e.ToString()); Debug.LogException(e); }
                return;
            }
            if (File.Exists("Temp/FlameBlame-CaptureLook"))
            {
                File.Delete("Temp/FlameBlame-CaptureLook"); CaptureComparison(); return;
            }
            const string request = "Temp/FlameBlame-ApplyLook";
            if (!File.Exists(request)) return;
            File.Delete(request);
            try { Apply(); File.WriteAllText("Logs/look-result.txt", "PASS: look applied and captured."); }
            catch (Exception e) { File.WriteAllText("Logs/look-result.txt", e.ToString()); Debug.LogException(e); }
        }

        [Shortcut("FlameBlame/Toggle Look", KeyCode.F2)]
        static void ToggleShortcut() { if (!EditorApplication.isPlaying) Toggle(); }

        [MenuItem("Tools/FlameBlame/Toggle Look")]
        public static void Toggle()
        {
            var controller = Object.FindFirstObjectByType<LookComparison>();
            if (controller == null) return;
            controller.SetLook(!controller.UsingLook);
            if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        static Light Key()
        {
            var warm = GameObject.Find("WarmKey");
            return warm != null ? warm.GetComponent<Light>() : RenderSettings.sun;
        }

        static Volume MainVolume()
        {
            Volume best = null;
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v.isGlobal && v.enabled && v.sharedProfile != null && (best == null || v.priority > best.priority)) best = v;
            return best;
        }

        static void RequireMain()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != Main)
                throw new InvalidOperationException("Open Main and stop Play first.");
        }

        [MenuItem("Tools/FlameBlame/Apply Look")]
        public static void Apply()
        {
            RequireMain();
            var key = Key(); var volume = MainVolume();
            if (key == null || volume == null) throw new InvalidOperationException("Main needs its key light and global volume.");
            var rotation = key.transform.rotation;
            var tuning = AssetDatabase.LoadAssetAtPath<VisualTuning>(Settings + "VisualTuning.asset");
            if (tuning == null) throw new InvalidOperationException("VisualTuning is missing.");
            var controller = Object.FindFirstObjectByType<LookComparison>();
            bool first = !File.Exists(Backup + "manifest.json");
            if (first) MakeBackup(key, volume);
            if (controller == null)
            {
                controller = new GameObject("Look Comparison (F2)").AddComponent<LookComparison>();
                controller.key = key; controller.volume = volume; controller.tuning = tuning;
                controller.originalVolumeActive = volume.gameObject.activeSelf;
                controller.originalVolumeEnabled = volume.enabled;
                controller.originalLighting.Capture(key);
                controller.backupProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Settings + "MainProfile_Backup_UserTuned.asset");
                controller.lookProfile = NewProfile();
            }
            ApplyMatte();
            ConfigurePipelines();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light != key) light.shadows = LightShadows.None;
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var baseCamera = Camera.main;
            var cameraStack = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            var finalCamera = cameraStack != null && cameraStack.Count > 0 ? cameraStack[cameraStack.Count - 1] : baseCamera;
            finalCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            controller.SetLook(true);
            if (Quaternion.Angle(rotation, key.transform.rotation) > .001f) throw new InvalidOperationException("Key rotation changed.");
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(controller.lookProfile);
            EditorUtility.SetDirty(tuning);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            CapturePair("After");
            Combine();
            File.AppendAllText("LOOK_NOTES.md", $"\n## Applied look\nFinal iteration 2: Neutral; contrast +{tuning.lookContrast}; saturation +{tuning.lookSaturation}; exposure {tuning.lookExposure}; split balance {tuning.lookSplitBalance}; bloom {tuning.lookBloom}/threshold 1; vignette {tuning.lookVignette}/smoothness {tuning.lookVignetteSmoothness}. Key cream (0.99,0.90,0.71), intensity {tuning.lookKeyIntensity}, soft shadow strength {tuning.lookShadowStrength}, bias 0.02/0.1; rotation unchanged. Gradient ambient sky (0.62,0.59,0.51), equator (0.36,0.34,0.32), ground (0.20,0.22,0.28), intensity 1.\nURP: HDR on, MSAA 2x, scale 1, main shadow map 2048, soft shadows on, distance 20m, LDR LUT32. Additional-light shadow texture support stays enabled for WebGL, while only the key casts shadows.\nF2 compares original profile/key/ambient with Look; matte materials and pipeline are shared. Before captures predate all changes. Restore User Tuned Look restores materials, pipeline/renderer assets and the full backed-up scene in one action. No reference assets imported.\nTune VisualTuning look fields and Apply Look (or toggle F2 twice). Archived Originals and transparent water/VFX are excluded. Toon shaders remain Toon; high color, MatCap and rim reflection effects are disabled instead of changing shaders.\n");
            Debug.Log("[FlameBlame] Look transfer PASS; key rotation unchanged, one shadow light.");
        }

        static VolumeProfile NewProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Settings + "MainProfile_Look.asset");
            if (existing != null) return existing;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Settings + "MainProfile_Look.asset");
            Add<Tonemapping>(profile); Add<ColorAdjustments>(profile); Add<SplitToning>(profile); Add<Bloom>(profile); Add<Vignette>(profile);
            return profile;
        }
        static void Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(component, profile);
        }

        static void MakeBackup(Light key, Volume volume)
        {
            Directory.CreateDirectory(Backup); Directory.CreateDirectory("Captures/Look"); AssetDatabase.Refresh();
            var manifest = new Manifest { antiAliasing = QualitySettings.antiAliasing };
            File.Copy("ProjectSettings/QualitySettings.asset", Backup + "QualitySettings.txt", false);
            var notes = new StringBuilder("# Look transfer baseline\n\nSource: https://github.com/KaganAyten/RestaurantGame3DUnity (MIT); settings only, no art imported.\n\n");
            notes.AppendLine("Key (called WarmKey in prompt): " + key.name + "; color=" + key.color + "; intensity=" + key.intensity
                + "; rotation=" + key.transform.eulerAngles + "; shadows=" + key.shadows + "; strength=" + key.shadowStrength
                + "; bias=" + key.shadowBias + "; normalBias=" + key.shadowNormalBias);
            notes.AppendLine("Ambient: " + RenderSettings.ambientMode + "; sky=" + RenderSettings.ambientSkyColor
                + "; equator=" + RenderSettings.ambientEquatorColor + "; ground=" + RenderSettings.ambientGroundColor + "; intensity=" + RenderSettings.ambientIntensity);
            notes.AppendLine("Volume source: " + AssetDatabase.GetAssetPath(volume.sharedProfile) + "\n```json\n" + EditorJsonUtility.ToJson(volume.sharedProfile, true) + "\n```");
            notes.AppendLine("Volume originally active: " + volume.gameObject.activeSelf + "; enabled=" + volume.enabled);
            foreach (var component in volume.sharedProfile.components) notes.AppendLine("```json\n" + EditorJsonUtility.ToJson(component, true) + "\n```");
            AssetDatabase.SaveAssetIfDirty(volume.sharedProfile);
            Copy(AssetDatabase.GetAssetPath(volume.sharedProfile), Settings + "MainProfile_Backup_UserTuned.asset");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Materials" }))
                BackupAsset(AssetDatabase.GUIDToAssetPath(guid), manifest);
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                BackupAsset(path, manifest);
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                notes.AppendLine("URP: " + path + "\n```json\n" + EditorJsonUtility.ToJson(pipeline, true) + "\n```");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/")) BackupAsset(path, manifest);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Backup + "Main_UserTuned.unity", true)) throw new IOException("Scene backup failed.");
            File.WriteAllText(Backup + "manifest.json", JsonUtility.ToJson(manifest, true));
            File.WriteAllText("LOOK_NOTES.md", notes.ToString());
            CapturePair("Before");
        }

        static void BackupAsset(string path, Manifest manifest)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path); AssetDatabase.SaveAssetIfDirty(asset);
            string copy = Backup + AssetDatabase.AssetPathToGUID(path) + Path.GetExtension(path);
            Copy(path, copy);
            var snapshot = AssetDatabase.LoadMainAssetAtPath(copy);
            snapshot.name = asset.name; EditorUtility.SetDirty(snapshot); AssetDatabase.SaveAssetIfDirty(snapshot);
            manifest.assets.Add(new AssetPair { source = path, backup = copy, originalName = asset.name });
        }
        static void Copy(string source, string destination)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destination) != null) throw new IOException("Refusing to overwrite backup: " + destination);
            if (!AssetDatabase.CopyAsset(source, destination)) throw new IOException("Backup failed: " + source);
        }

        [MenuItem("Tools/FlameBlame/Restore User Tuned Look")]
        public static void Restore()
        {
            RequireMain();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Backup + "manifest.json"));
            foreach (var pair in manifest.assets)
            {
                // CopyAsset renames the copied root; retain the source's original display name.
                var snapshot = AssetDatabase.LoadMainAssetAtPath(pair.backup);
                snapshot.name = string.IsNullOrEmpty(pair.originalName) ? Path.GetFileNameWithoutExtension(pair.source) : pair.originalName;
                EditorUtility.SetDirty(snapshot); AssetDatabase.SaveAssetIfDirty(snapshot);
                // Restore the whole serialized file so renderer feature subassets keep local references.
                File.Copy(pair.backup, pair.source, true);
                AssetDatabase.ImportAsset(pair.source, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.SaveAssets();
            QualitySettings.antiAliasing = manifest.antiAliasing;
            File.Copy(Backup + "QualitySettings.txt", "ProjectSettings/QualitySettings.asset", true);
            var scene = EditorSceneManager.OpenScene(Backup + "Main_UserTuned.unity");
            EditorSceneManager.SaveScene(scene, Main);
            // Retain backups; a fresh apply may recreate the comparison from the restored scene.
            Debug.Log("[FlameBlame] Restored user-tuned materials, URP and scene. Backups retained.");
        }

        [MenuItem("Tools/FlameBlame/Apply Matte Materials")]
        public static void ApplyMatte()
        {
            if (!File.Exists(Backup + "manifest.json")) { Apply(); return; }
            Directory.CreateDirectory("Captures/Look");
            var report = new StringBuilder(); int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Materials" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (path.Contains("/Originals/") || path.Contains("/VFX/") || path.Contains("/Water/")
                    || material.shader.name.Contains("Particles") || (material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f))
                { report.AppendLine("SKIP " + path); continue; }
                Color before = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                Set(material, "_Metallic", 0); Set(material, "_Smoothness", 0); Set(material, "_EnvironmentReflections", 0);
                Set(material, "_SpecularHighlights", 1); Set(material, "_ReceiveShadows", 1);
                if (material.shader.name == "Universal Render Pipeline/Lit")
                { material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF"); material.DisableKeyword("_RECEIVE_SHADOWS_OFF"); }
                if (material.shader.name == "Toon/Toon")
                {
                    material.SetColor("_HighColor", Color.black); Set(material, "_MatCap", 0); Set(material, "_RimLight", 0);
                    Set(material, "_Is_SpecularToHighColor", 0);
                }
                if (material.HasProperty("_BaseColor") && material.GetColor("_BaseColor") != before) throw new InvalidOperationException("Base color changed: " + path);
                EditorUtility.SetDirty(material); count++; report.AppendLine("MATTE " + path);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Captures/Look/Matte-report.txt", "Updated " + count + " materials.\n" + report);
        }
        static void Set(Material material, string property, float value) { if (material.HasProperty(property)) material.SetFloat(property, value); }

        static void ConfigurePipelines()
        {
            QualitySettings.antiAliasing = 2;
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/") || path.StartsWith(Backup)) continue;
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                var data = new SerializedObject(pipeline);
                data.FindProperty("m_SupportsHDR").boolValue = true;
                data.FindProperty("m_MSAA").intValue = 2; data.FindProperty("m_RenderScale").floatValue = 1;
                data.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
                data.FindProperty("m_SoftShadowsSupported").boolValue = true;
                data.FindProperty("m_ShadowDistance").floatValue = 20;
                data.FindProperty("m_ColorGradingMode").intValue = 0; data.FindProperty("m_ColorGradingLutSize").intValue = 32;
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/") || path.StartsWith(Backup)) continue;
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                foreach (var feature in renderer.rendererFeatures)
                    if (feature != null && feature.name.Contains("AmbientOcclusion")) { feature.SetActive(false); EditorUtility.SetDirty(feature); }
            }
        }

        static void CapturePair(string label)
        {
            var main = Camera.main;
            if (main == null) throw new InvalidOperationException("Main camera missing.");
            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            Vector3 position = main.transform.position; Quaternion rotation = main.transform.rotation;
            try
            {
            for (int angle = 0; angle < 2; angle++)
            {
                main.transform.SetPositionAndRotation(position + (angle == 0 ? Vector3.zero : Vector3.up * .4f),
                    rotation * Quaternion.Euler(0, angle * 40, 0));
                var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGBHalf);
                var previous = RenderTexture.active; var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                try
                {
                    // Warm up changed pipeline targets and shader variants before recording.
                    RenderPipeline.SubmitRenderRequest(main, new RenderPipeline.StandardRequest { destination = target });
                    RenderPipeline.SubmitRenderRequest(main, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                    File.WriteAllBytes($"Captures/Look/{label}-{angle + 1}.png", image.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(image); Object.DestroyImmediate(target); }
            }
            }
            finally { ShaderUtil.allowAsyncCompilation = asyncShaders; main.transform.SetPositionAndRotation(position, rotation); }
        }

        [MenuItem("Tools/FlameBlame/Capture Look Comparison")]
        public static void CaptureComparison()
        {
            var controller = Object.FindFirstObjectByType<LookComparison>();
            if (controller == null) throw new InvalidOperationException("Apply Look first.");
            bool selected = controller.UsingLook;
            try
            {
                controller.SetLook(false); CapturePair("Backup");
                controller.SetLook(true); CapturePair("Look");
                Combine("Backup", "Look");
            }
            finally { controller.SetLook(selected); }
        }

        static void Combine(string beforeLabel = "Before", string afterLabel = "After")
        {
            for (int angle = 1; angle <= 2; angle++)
            {
                var before = new Texture2D(2, 2); before.LoadImage(File.ReadAllBytes($"Captures/Look/{beforeLabel}-{angle}.png"));
                var after = new Texture2D(2, 2); after.LoadImage(File.ReadAllBytes($"Captures/Look/{afterLabel}-{angle}.png"));
                var pair = new Texture2D(2560, 720, TextureFormat.RGB24, false);
                pair.SetPixels(0, 0, 1280, 720, before.GetPixels()); pair.SetPixels(1280, 0, 1280, 720, after.GetPixels()); pair.Apply();
                File.WriteAllBytes($"Captures/Look/{beforeLabel}-{afterLabel}-{angle}.png", pair.EncodeToPNG());
                Object.DestroyImmediate(before); Object.DestroyImmediate(after); Object.DestroyImmediate(pair);
            }
        }
    }
}
