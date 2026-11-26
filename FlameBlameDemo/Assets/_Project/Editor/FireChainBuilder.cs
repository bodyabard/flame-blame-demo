using System.Collections.Generic;
using System.IO;
using System.Text;
using FlameBlame.Core;
using FlameBlame.Fire;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Build Fire Chain: makes fire hop curtain -> sofa/armchair -> papers -> rug -> newspapers -> table -> magazines -> shelves,
    /// and turns the shelf books into their own burnable nodes. Idempotent; positions are derived from the existing furniture.
    /// Batch: Unity -batchmode -executeMethod FlameBlame.EditorTools.FireChainBuilder.RunBatch -quit
    public static class FireChainBuilder
    {
        const string Root = "Assets/_Project";
        const string MainPath = Root + "/Scenes/Main.unity";
        const string GroupName = "FireChain";

        [MenuItem("Tools/FlameBlame/Build Fire Chain")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainPath);
            var p = CalibrationSceneBuilder.LoadProfile();
            Radii(p);

            var crate = GameObject.Find("Crate").transform;
            var zoneRoot = crate.parent;                       // same hierarchy as the existing Floor2 furniture
            GameObject stale; while ((stale = GameObject.Find(GroupName)) != null) Object.DestroyImmediate(stale);
            var zone = Object.FindFirstObjectByType<LevelScenario>().floor2;   // the scenario zone whose members decide "floor cleared"
            members.Clear();
            var group = new GameObject(GroupName).transform; group.SetParent(zoneRoot, false);
            group.position = Vector3.zero; group.rotation = Quaternion.identity; group.localScale = Vector3.one;

            var paperMat = GameObject.Find("PaperStack_1").GetComponentInChildren<Renderer>().sharedMaterial;
            float floor = Bounds(GameObject.Find("PaperStack_1")).min.y;
            var rugMat = RugMaterial();
            var fabric = Profile("Fabric"); var paper = Profile("Paper");

            // Bridges: each one sits within spread range of the previous link (see fire chain note in SCALE_NOTES.md).
            Prop(group, "Rug", new Vector3(.15f, floor + .015f, 5.8f), new Vector3(1.5f, .03f, 1.1f), 0, rugMat, fabric, "Килимок", 260);
            Prop(group, "Magazines_2", new Vector3(1.7f, floor + .04f, 6.6f), new Vector3(.4f, .08f, .3f), 25, paperMat, paper, "Журнали", 70);
            Prop(group, "Magazines_1", new Vector3(1.7f, floor + .04f, 4.9f), new Vector3(.4f, .08f, .3f), -15, paperMat, paper, "Журнали", 70);

            foreach (string shelf in new[] { "Shelf_1", "Shelf_2" }) Books(group, GameObject.Find(shelf).transform, paper, shelf == "Shelf_1" ? "Books_1" : "Books_2");

            RegisterMembers(zone);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] Fire chain built.");
        }

        static readonly List<Flammable> members = new List<Flammable>();
        // FireZone.Cleared (the win condition) only looks at its members array, so the new nodes must be listed there.
        static void RegisterMembers(FireZone zone)
        {
            if (zone == null) { Debug.LogWarning("[FlameBlame] LevelScenario.floor2 missing; new props not registered."); return; }
            var so = new SerializedObject(zone); var arr = so.FindProperty("members");
            foreach (var f in members)
            {
                bool have = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == f) have = true;
                if (have) continue;
                arr.arraySize++; arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = f;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(zone);
        }

        static void Radii(ScaleProfile p)
        {
            Set("Fabric", p.spreadRadiusFabric); Set("Paper", p.spreadRadiusPaper); Set("Wood", p.spreadRadiusWood);
        }
        static FireMaterialProfile Profile(string n) => AssetDatabase.LoadAssetAtPath<FireMaterialProfile>(Root + "/Settings/Fire/Scenario_FireProfile_" + n + ".asset");
        static void Set(string n, float r) { var pr = Profile(n); pr.spreadRadius = r; EditorUtility.SetDirty(pr); }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        static Material RugMaterial()
        {
            string path = Root + "/Materials/Toon/Env_Rug_Toon.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var src = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Toon/Visual_Coral_Toon.mat");
            if (m == null) { m = new Material(src) { name = "Env_Rug_Toon" }; AssetDatabase.CreateAsset(m, path); }
            var pal = AssetDatabase.LoadAssetAtPath<EnvironmentPalette>(Root + "/Settings/EnvironmentPalette.asset");
            Color c = pal != null ? pal.flamCherry : new Color(.4f, .1f, .15f);
            float s1 = src.GetColor("_1st_ShadeColor").maxColorComponent / Mathf.Max(.0001f, src.GetColor("_BaseColor").maxColorComponent);
            float s2 = src.GetColor("_2nd_ShadeColor").maxColorComponent / Mathf.Max(.0001f, src.GetColor("_BaseColor").maxColorComponent);
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c); m.SetColor("_1st_ShadeColor", c * s1); m.SetColor("_2nd_ShadeColor", c * s2);
            EditorUtility.SetDirty(m); return m;
        }

        static Flammable Burnable(GameObject go, FireMaterialProfile profile, string label, int value)
        {
            SetLayer(go.transform, LayerMask.NameToLayer("Interactable"));
            var fz = go.AddComponent<FireZone>(); fz.id = FireZoneId.Floor2;   // per-object zone tag, same as the existing furniture
            var f = go.AddComponent<Flammable>();
            var so = new SerializedObject(f); so.FindProperty("profile").objectReferenceValue = profile; so.FindProperty("igniteOnStart").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
            var d = go.AddComponent<PropertyDamage>(); var ds = new SerializedObject(d);
            ds.FindProperty("displayName").stringValue = label; ds.FindProperty("propertyValue").intValue = value; ds.ApplyModifiedPropertiesWithoutUndo();
            return f;
        }
        static void SetLayer(Transform t, int layer) { t.gameObject.layer = layer; foreach (Transform c in t) SetLayer(c, layer); }

        static void Prop(Transform parent, string name, Vector3 pos, Vector3 size, float yaw, Material mat, FireMaterialProfile profile, string label, int value)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            members.Add(Burnable(go, profile, label, value));
        }

        /// Each shelf's decorative "Book" children (part of a prefab instance, so they can't be moved) are hidden and re-created
        /// as one burnable group sitting on the shelf, so the shelf and its books are separate fire nodes.
        static void Books(Transform parent, Transform shelf, FireMaterialProfile paper, string name)
        {
            var sources = new List<Transform>();
            foreach (var t in shelf.GetComponentsInChildren<Transform>(true)) if (t.name == "Book") sources.Add(t);
            // Flammable measures its node from bounds that start at the root pivot, so the pivot must sit inside the books.
            Bounds bounds = new Bounds(sources[0].position, Vector3.zero);
            foreach (var src in sources) bounds.Encapsulate(new Bounds(src.position, src.lossyScale));
            var group = new GameObject(name).transform; group.SetParent(parent, false);
            group.position = bounds.center; group.rotation = Quaternion.identity; group.localScale = Vector3.one;
            foreach (var src in sources)
            {
                var r = src.GetComponent<Renderer>();
                var book = GameObject.CreatePrimitive(PrimitiveType.Cube); book.name = "Book";
                Object.DestroyImmediate(book.GetComponent<Collider>());
                book.transform.SetParent(group, false);
                book.transform.SetPositionAndRotation(src.position, src.rotation); book.transform.localScale = src.lossyScale;
                book.GetComponent<Renderer>().sharedMaterial = r.sharedMaterial;
                src.gameObject.SetActive(false);
            }
            var box = group.gameObject.AddComponent<BoxCollider>(); box.center = Vector3.zero; box.size = bounds.size;
            members.Add(Burnable(group.gameObject, paper, "Книжки", 120));
        }

        public static void Shot()
        {
            EditorSceneManager.OpenScene(MainPath);
            var go = new GameObject("ShotCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 75; cam.aspect = 16f / 9f;
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            go.transform.position = new Vector3(-1.2f, 4.7f, 4.6f); go.transform.LookAt(new Vector3(1.4f, 3.5f, 6.0f));
            var tex = CalibrationSheets.Grab(cam, 1600, 900); File.WriteAllBytes("Captures/Calibration/fire_chain_view.png", tex.EncodeToPNG());
            EditorApplication.Exit(0);
        }

        // ---------------------------------------------------------------- batch + offline spread simulation

        public static void RunBatch()
        {
            try { Simulate("before"); Build(); Simulate("after"); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); File.WriteAllText("Captures/Calibration/error.txt", e.ToString()); EditorApplication.Exit(1); }
        }

        /// Ticks the real FireSystem on nodes built from the scene's Flammables (curtain ignited) and logs when each one catches fire.
        public static void Simulate(string tag)
        {
            EditorSceneManager.OpenScene(MainPath);
            var system = new FireSystem(null, 64);
            var nodes = new List<(string name, FireNode node)>(); FireNode curtain = null;
            foreach (var f in Object.FindObjectsByType<Flammable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var zone = f.GetComponentInParent<FireZone>(); if (zone == null || zone.id != FireZoneId.Floor2) continue;
                var prof = (FireMaterialProfile)new SerializedObject(f).FindProperty("profile").objectReferenceValue;
                var n = new FireNode(prof, Bounds(f.gameObject).center, f) { Zone = zone.id };
                system.Register(n); nodes.Add((f.name, n)); if (f.name == "Curtain") curtain = n;
            }
            system.Ignite(curtain);
            var sb = new StringBuilder(tag + ": fire spread simulation (Floor2, curtain ignited at t=0, no water, cap 64):\n");
            var caught = new Dictionary<string, float> { { "Curtain", 0f } };
            for (float t = 0; t < 180f; t += .1f)
            {
                system.Tick(.1f);
                foreach (var (name, node) in nodes) if (node.State != FireState.Idle && !caught.ContainsKey(name)) caught[name] = t;
            }
            foreach (var (name, node) in nodes)
                sb.AppendLine($"{name,-16} {(caught.TryGetValue(name, out float ct) ? "ignites at " + ct.ToString("0.0") + " s" : (node.Profile.canBurn ? "NEVER within 180 s" : "non-flammable (metal)"))}");
            File.WriteAllText("Captures/Calibration/fire_spread_sim_" + tag + ".txt", sb.ToString());
            Debug.Log("[FlameBlame] " + sb);
        }
    }
}
