using System;
using System.IO;
using FlameBlame.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    public static class SubmissionBuild
    {
        const string Scene = "Assets/_Project/Scenes/Main.unity";
        [MenuItem("Tools/FlameBlame/Configure WebGL")]
        public static void ConfigureWebGL()
        {
            Common();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.template = "PROJECT:FlameBlame";
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
            // Bind URP's empty comparison-shadow texture even when no additional light casts shadows.
            var mobilePipeline = AssetDatabase.LoadAssetAtPath<Object>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobilePipeline != null)
            {
                var pipelineData = new SerializedObject(mobilePipeline);
                pipelineData.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
                pipelineData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mobilePipeline);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] WebGL configured: Brotli/fallback, cache off, DXT, low stripping, 256–1024 MB, background enabled.");
        }
        static void Common()
        {
            PlayerSettings.productName = "Flame & Blame Demo";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.stripEngineCode = true;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        }
        [MenuItem("Tools/FlameBlame/Validate Submission")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new OperationCanceledException("Scene save cancelled.");
            EditorSceneManager.OpenScene(Scene);
            var root = Object.FindFirstObjectByType<GameBootstrap>();
            if (root == null) throw new InvalidOperationException("Main requires GameBootstrap.");
            var data = new SerializedObject(root);
            foreach (string field in new[] { "input", "hose", "hoseRenderer", "jet", "playerLook", "playerMotor", "cameraFeedback", "gameAudio", "hands", "chaosSettings", "hud", "effectsRoot", "flameTemplate", "steamTemplate", "splashTemplate", "sparkTemplate", "puddleTemplate" })
                if (data.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("Missing Main reference: " + field);
            int capacity = 240 + Count(data, "flameTemplate") * 16 + Count(data, "steamTemplate") * 6 + Count(data, "splashTemplate") * 8 + Count(data, "sparkTemplate") * 4;
            if (capacity > 1000) throw new InvalidOperationException("Authored particle capacity exceeds 1000: " + capacity);
            int shadows = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.enabled && light.shadows != LightShadows.None) shadows++;
            if (shadows > 1) throw new InvalidOperationException("More than one shadow light.");
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) throw new InvalidOperationException("Missing script: " + transform.name);
            Debug.Log("[FlameBlame] Submission validation PASS: Main references, scripts, " + capacity + " particle capacity, " + shadows + " shadow light.");
        }
        static int Count(SerializedObject root, string field)
        {
            var template = (ParticleSystem)root.FindProperty(field).objectReferenceValue;
            int total = 0; foreach (var system in template.GetComponentsInChildren<ParticleSystem>(true)) total += system.main.maxParticles;
            return total;
        }
        [MenuItem("Tools/FlameBlame/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureWebGL(); Validate();
            Build(BuildTarget.WebGL, "Builds/WebGL");
        }
        [MenuItem("Tools/FlameBlame/Build Windows")]
        public static void BuildWindows()
        {
            Common(); Validate();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);
            AssetDatabase.SaveAssets();
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/FlameBlameDemo.exe");
        }
        static void Build(BuildTarget target, string path)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException("Install " + target + " Build Support for Unity " + Application.unityVersion + " via Unity Hub, then rerun this command.");
            Directory.CreateDirectory("Build");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = path, target = target, options = BuildOptions.None });
            var summary = report.summary;
            File.WriteAllText("Build/report-" + target + ".json", JsonUtility.ToJson(new Receipt { target = target.ToString(), result = summary.result.ToString(), bytes = summary.totalSize, seconds = summary.totalTime.TotalSeconds, errors = summary.totalErrors, warnings = summary.totalWarnings, unity = Application.unityVersion, output = Path.GetFullPath(path) }, true));
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + summary.result + "; errors=" + summary.totalErrors);
            Debug.Log("[FlameBlame] BUILD SUCCEEDED: " + path + " (" + summary.totalSize + " bytes)");
        }
        [Serializable] sealed class Receipt
        { public string target, result, unity, output; public ulong bytes; public double seconds; public int errors, warnings; }
    }
}
