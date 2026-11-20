using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlameBlame.EditorTools
{
    // Copies the artist's template; originals retain their GUIDs and shader settings.
    [InitializeOnLoad]
    public static class ToonMaterialMigration
    {
        const string Root = "Assets/_Project/Materials";
        const string Template = Root + "/Toon/Neighbor_B_Toon.mat";

        static ToonMaterialMigration() { EditorApplication.update += Poll; }

        static void Poll()
        {
            const string request = "Temp/FlameBlame-MigrateToon";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            File.Delete(request);
            try { Migrate(); }
            catch (Exception e) { File.WriteAllText("Logs/toon-migration.txt", e.ToString()); Debug.LogException(e); }
        }

        public static string OriginalPath(string name)
        {
            string path = Root + "/Originals/" + name + ".mat";
            return AssetDatabase.IsValidFolder(Root + "/Originals") ? path : Root + "/" + name + ".mat";
        }

        public static void ReapplyIfConfigured()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(Template) != null) Migrate();
        }

        [MenuItem("Tools/FlameBlame/Materials/Apply Environment Toon")]
        public static void Migrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before migrating materials.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Main.unity") throw new InvalidOperationException("Open Main before migrating materials.");
            var template = AssetDatabase.LoadAssetAtPath<Material>(Template)
                ?? AssetDatabase.LoadAssetAtPath<Material>(Root + "/Neighbor_B_Toon.mat");
            if (template == null || template.shader.name != "Toon/Toon")
                throw new InvalidOperationException("Neighbor_B_Toon must use Toon/Toon.");

            var sourcePaths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != template && !path.StartsWith(Root + "/Toon/", StringComparison.Ordinal)) sourcePaths.Add(path);
            }
            sourcePaths.Sort(StringComparer.Ordinal);
            var originals = new Dictionary<Material, string>();
            foreach (string path in sourcePaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                originals.Add(mat, EditorJsonUtility.ToJson(mat));
            }

            EnsureFolder("Originals"); EnsureFolder("Toon");
            AssetDatabase.SaveAssetIfDirty(template);
            Move(template, Template);
            Color reference = template.GetColor("_BaseColor");
            Color shade1 = template.GetColor("_1st_ShadeColor");
            Color shade2 = template.GetColor("_2nd_ShadeColor");
            var replacements = new Dictionary<Material, Material>();
            var report = new StringBuilder("Environment Toon migration\n");
            foreach (string path in sourcePaths)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(path);
                Move(source, Root + "/Originals/" + source.name + ".mat");
                // Preserve transparent water, particles and other specialized effects.
                bool opaque = !source.HasProperty("_Surface") || source.GetFloat("_Surface") < .5f;
                if (!opaque || !source.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)
                    || source.shader.name.Contains("Particles")) continue;
                string destination = Root + "/Toon/" + source.name + "_Toon.mat";
                var toon = source.name == "Neighbor_B" ? template : AssetDatabase.LoadAssetAtPath<Material>(destination);
                if (toon != template)
                {
                    bool create = toon == null;
                    if (create) toon = new Material(template) { name = source.name + "_Toon" };
                    else toon.CopyPropertiesFromMaterial(template);
                    Color color = source.GetColor("_BaseColor");
                    toon.SetColor("_BaseColor", color);
                    toon.SetColor("_Color", color);
                    toon.SetColor("_1st_ShadeColor", ScaleShade(color, reference, shade1));
                    toon.SetColor("_2nd_ShadeColor", ScaleShade(color, reference, shade2));
                    if (source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap") != null)
                    {
                        toon.SetTexture("_MainTex", source.GetTexture("_BaseMap"));
                        toon.SetTextureScale("_MainTex", source.GetTextureScale("_BaseMap"));
                        toon.SetTextureOffset("_MainTex", source.GetTextureOffset("_BaseMap"));
                    }
                    if (source.HasProperty("_EmissionColor")) toon.SetColor("_Emissive_Color", source.GetColor("_EmissionColor"));
                    if (create) AssetDatabase.CreateAsset(toon, destination);
                    else EditorUtility.SetDirty(toon);
                }
                replacements.Add(source, toon);
                report.AppendLine(source.name + " -> " + toon.name);
            }

            int prefabChanges = 0, sceneChanges = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = ReplaceReferences(root, replacements);
                    if (changed > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); prefabChanges += changed; }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var root in scene.GetRootGameObjects()) sceneChanges += ReplaceReferences(root, replacements);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Failed to save Main.");
            AssetDatabase.SaveAssets();
            foreach (var original in originals)
                if (EditorJsonUtility.ToJson(original.Key) != original.Value)
                    throw new InvalidOperationException("Original material changed: " + original.Key.name);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null && replacements.ContainsKey(material))
                            throw new InvalidOperationException("Unconverted renderer: " + renderer.name);
            report.AppendLine($"Verified {replacements.Count} Toon materials; original settings unchanged.");
            report.AppendLine($"Replaced references: prefabs={prefabChanges}, scene={sceneChanges}.");
            File.WriteAllText("Logs/toon-migration.txt", report.ToString());
            Debug.Log("[FlameBlame] " + report);
        }

        static Color ScaleShade(Color color, Color reference, Color shade)
        {
            float ratio = shade.maxColorComponent / Mathf.Max(reference.maxColorComponent, .0001f);
            Color result = color * ratio; result.a = color.a; return result;
        }

        static void EnsureFolder(string name)
        {
            if (!AssetDatabase.IsValidFolder(Root + "/" + name)) AssetDatabase.CreateFolder(Root, name);
        }

        static void Move(Material material, string destination)
        {
            string source = AssetDatabase.GetAssetPath(material);
            if (source == destination) return;
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }

        static int ReplaceReferences(GameObject root, Dictionary<Material, Material> replacements)
        {
            int count = 0;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                bool changed = false;
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference
                        || !(property.objectReferenceValue is Material material)
                        || !replacements.TryGetValue(material, out Material toon)) continue;
                    property.objectReferenceValue = toon; changed = true; count++;
                }
                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    EditorUtility.SetDirty(component);
                }
            }
            return count;
        }
    }
}
