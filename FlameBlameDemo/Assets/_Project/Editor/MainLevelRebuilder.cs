using System;
using System.Collections.Generic;
using System.IO;
using FlameBlame.Core;
using FlameBlame.Fire;
using FlameBlame.Feedback;
using FlameBlame.Hose;
using FlameBlame.Npc;
using FlameBlame.Electric;
using FlameBlame.Water;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Rebuild Main Level: applies ScaleProfile + EnvironmentPalette to the existing Main scene in place.
    /// Lights, Volume and gameplay logic are never touched. Safe to re-run (original values live in LevelBaseline).
    public static class MainLevelRebuilder
    {
        const string Root = "Assets/_Project";
        const string MainPath = Root + "/Scenes/Main.unity";
        const string PalettePath = Root + "/Settings/EnvironmentPalette.asset";
        const string BaselinePath = Root + "/Settings/LevelBaseline.asset";
        const string ToonDir = Root + "/Materials/Toon";
        const float OldW = 12f, OldL = 10f;
        static ScaleProfile p; static EnvironmentPalette pal; static LevelBaseline bl; static Material template;

        [MenuItem("Tools/FlameBlame/Rebuild Main Level")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before rebuilding.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainPath);
            p = CalibrationSceneBuilder.LoadProfile(); pal = LoadPalette(); bl = LoadBaseline();
            template = AssetDatabase.LoadAssetAtPath<Material>(ToonDir + "/Neighbor_B_Toon.mat");
            var byName = new Dictionary<string, Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!byName.ContainsKey(t.name)) byName[t.name] = t;

            ApplyPalette(byName);
            Room(byName);
            Layout(byName);
            Npcs(byName);
            CameraAndHands(byName);
            Mechanics(byName);
            EditorUtility.SetDirty(bl); AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FlameBlame] Main level rebuilt from ScaleProfile + EnvironmentPalette.");
        }

        // ---------------------------------------------------------------- assets

        static EnvironmentPalette LoadPalette()
        {
            var a = AssetDatabase.LoadAssetAtPath<EnvironmentPalette>(PalettePath);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<EnvironmentPalette>();
            // Seed room colours straight from the accepted calibration scene materials.
            const string cal = Root + "/Art/Calibration/Cal_";
            a.wall = CalColor(cal + "Peach.mat", a.wall); a.floor = CalColor(cal + "Floor.mat", a.floor);
            a.wainscot = CalColor(cal + "Wainscot.mat", a.wainscot); a.door = CalColor(cal + "Door.mat", a.door);
            AssetDatabase.CreateAsset(a, PalettePath); return a;
        }
        static Color CalColor(string path, Color fallback)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path); return m != null ? m.GetColor("_BaseColor") : fallback;
        }
        static LevelBaseline LoadBaseline()
        {
            var a = AssetDatabase.LoadAssetAtPath<LevelBaseline>(BaselinePath);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<LevelBaseline>(); AssetDatabase.CreateAsset(a, BaselinePath); return a;
        }

        // ---------------------------------------------------------------- materials

        static Color Shade(Color c, string prop)
        {
            float ratio = template.GetColor(prop).maxColorComponent / Mathf.Max(template.GetColor("_BaseColor").maxColorComponent, .0001f);
            var r = c * ratio; r.a = c.a; return r;
        }
        static void Tint(Material m, Color c)
        {
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
            m.SetColor("_1st_ShadeColor", Shade(c, "_1st_ShadeColor")); m.SetColor("_2nd_ShadeColor", Shade(c, "_2nd_ShadeColor"));
            EditorUtility.SetDirty(m);
        }
        /// Existing Toon material (plus its Lit original so a later re-migration keeps the palette) or a new copy of the template.
        static Material Toon(string name, Color c)
        {
            string path = ToonDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(template) { name = name }; AssetDatabase.CreateAsset(m, path); }
            Tint(m, c);
            string original = Root + "/Materials/Originals/" + name.Replace("_Toon", "") + ".mat";
            var o = AssetDatabase.LoadAssetAtPath<Material>(original);
            if (o != null) { o.SetColor("_BaseColor", c); EditorUtility.SetDirty(o); }
            return m;
        }
        static Material Env(string name, Color c) => Toon("Env_" + name + "_Toon", c);

        static void ApplyPalette(Dictionary<string, Transform> byName)
        {
            var map = new Dictionary<string, Color>
            {
                { "Visual_Sage_Toon", pal.wall }, { "Floor_Toon", pal.floor }, { "Visual_Teal_Toon", pal.sofa }, { "Fabric_Toon", pal.sofa },
                { "Visual_Coral_Toon", pal.accent }, { "Visual_Walnut_Toon", pal.wood }, { "Wood_Toon", pal.wood }, { "Metal_Toon", pal.metal },
                { "Paper_Toon", pal.paper }, { "Visual_Cream_Toon", pal.paper }, { "Visual_Petal_Toon", pal.petal }, { "Visual_Leaf_Toon", pal.leaf }
            };
            foreach (var kv in map)
                if (AssetDatabase.LoadAssetAtPath<Material>(ToonDir + "/" + kv.Key + ".mat") != null) Toon(kv.Key, kv.Value);
            // Watering can is the one big-ish prop that used the green accent: make it warm.
            if (byName.TryGetValue("WateringCan", out var can))
                foreach (var r in can.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterial != null && r.sharedMaterial.name == "Visual_Leaf_Toon") r.sharedMaterial = Toon("Visual_Coral_Toon", pal.accent);
        }

        // ---------------------------------------------------------------- room

        static void Room(Dictionary<string, Transform> byName)
        {
            float W = p.roomWidth, L = p.roomLength, H = p.roomHeight;
            var room = byName["Room"];
            Set(room, "Floor", new Vector3(0, -.1f, 0), new Vector3(W, .2f, L));
            Set(room, "Wall_N", new Vector3(0, H / 2, L / 2 + .1f), new Vector3(W + .4f, H, .2f));
            Set(room, "Wall_S", new Vector3(0, H / 2, -L / 2 - .1f), new Vector3(W + .4f, H, .2f));
            Set(room, "Wall_W", new Vector3(-W / 2 - .1f, H / 2, 0), new Vector3(.2f, H, L));
            Set(room, "Wall_E", new Vector3(W / 2 + .1f, H / 2, 0), new Vector3(.2f, H, L));
            Set(room, "Ceiling", new Vector3(0, H + .1f, 0), new Vector3(W + .4f, .2f, L + .4f));
            var ceiling = room.Find("Ceiling");
            ceiling.gameObject.SetActive(true);
            var cr = ceiling.GetComponent<Renderer>();
            cr.sharedMaterial = Env("Ceiling", pal.ceiling);
            // Step 1 of the ceiling plan: no shadow casting/receiving, so the user's lighting is unaffected.
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; cr.receiveShadows = false;

            var old = room.Find("Decor"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var decor = new GameObject("Decor").transform; decor.SetParent(room, false);
            var wain = Env("Wainscot", pal.wainscot);
            Box(decor, "WainscotN", new Vector3(0, .5f, L / 2 - .02f), new Vector3(W, 1f, .04f), wain);
            Box(decor, "WainscotS", new Vector3(0, .5f, -L / 2 + .02f), new Vector3(W, 1f, .04f), wain);
            Box(decor, "WainscotW", new Vector3(-W / 2 + .02f, .5f, 0), new Vector3(.04f, 1f, L), wain);
            Box(decor, "WainscotE", new Vector3(W / 2 - .02f, .5f, 0), new Vector3(.04f, 1f, L), wain);
            float dw = p.doorHeight * .43f;
            var door = new GameObject("Door").transform; door.SetParent(decor, false); door.localPosition = new Vector3(W * .12f, 0, L / 2 - .03f);
            Box(door, "DoorFrame", new Vector3(0, p.doorHeight / 2 + .03f, 0), new Vector3(dw + .12f, p.doorHeight + .06f, .05f), Env("DoorFrame", pal.wainscot * .55f));
            Box(door, "DoorLeaf", new Vector3(0, p.doorHeight / 2, -.02f), new Vector3(dw, p.doorHeight, .05f), Env("Door", pal.door));
            Box(door, "DoorHandle", new Vector3(dw * .38f, p.doorHeight * .5f, -.06f), new Vector3(.08f, .05f, .05f), Env("DoorMetal", pal.metal));
        }
        static void Set(Transform parent, string name, Vector3 pos, Vector3 scale) { var t = parent.Find(name); t.localPosition = pos; t.localScale = scale; }
        static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var t = CalibrationSceneBuilder.Part(parent, name, pos, scale, m); t.gameObject.layer = 0;
        }

        // ---------------------------------------------------------------- layout

        static void Layout(Dictionary<string, Transform> byName)
        {
            float W = p.roomWidth, L = p.roomLength, kx = W / OldW, kz = L / OldL, f = p.objectScaleMultiplier * p.furnitureFit;
            float mx = W / 2 - .2f, mz = L / 2 - .2f;
            foreach (string name in new[] { "Sofa", "Armchair", "Curtain", "PaperStack_1", "PaperStack_2", "PaperStack_3", "Shelf_1", "Shelf_2", "Table",
                                            "Crate", "MetalLocker", "MetalBarrel", "SparkCable", "WateringCan", "Book", "Flowerpot", "Hydrant", "Player" })
            {
                if (!byName.TryGetValue(name, out var t)) { Debug.LogWarning("[FlameBlame] Missing in Main: " + name); continue; }
                var b = bl.Transform("layout:" + name, t);
                bool player = name == "Player";
                float x = Mathf.Clamp(b.position.x * kx, -mx, mx), z = Mathf.Clamp(b.position.z * kz, -mz, mz);
                if (name.StartsWith("Shelf")) x = W / 2 - .25f;
                if (name == "Curtain") x = -W / 2 + .06f;
                t.position = new Vector3(x, player ? b.position.y : b.position.y * f, z);
                if (!player) t.localScale = b.scale * f;
                var rb = t.GetComponent<Rigidbody>();
                if (rb != null) rb.mass = Mathf.Max(.3f, bl.Value("mass:" + name, rb.mass) * f * f * f);
                if (name == "SparkCable")
                    Float(t.GetComponent<SparkCable>(), "contactRadius", bl.Value("cable:radius", GetFloat(t.GetComponent<SparkCable>(), "contactRadius")) * f);
            }
        }

        // ---------------------------------------------------------------- NPCs

        static void Npcs(Dictionary<string, Transform> byName)
        {
            var eye = AssetDatabase.LoadAssetAtPath<Material>(ToonDir + "/Feel_Eye_Toon.mat"); var pupil = AssetDatabase.LoadAssetAtPath<Material>(ToonDir + "/Feel_Pupil_Toon.mat");
            Func<string, Color, Material> mat = (n, c) =>
            {
                switch (n)
                {
                    case "Eye": return eye; case "Pupil": return pupil;
                    case "Skin": return Toon("Npc_Skin_Toon", pal.npcSkin); case "Yellow": return Toon("Npc_Stripe_Toon", pal.npcStripe);
                    case "Boot": return Toon("Npc_Boot_Toon", pal.npcBoot); case "NpcGlove": return Toon("Npc_Glove_Toon", pal.npcGlove);
                    default: return Toon("Npc_" + n + "_Toon", c);
                }
            };
            var player = byName["Player"].position;
            var homes = new Dictionary<string, Vector3> { { "Neighbor_A", new Vector3(-.95f, 0, 1.55f) }, { "Neighbor_B", new Vector3(-.4f, 0, .55f) } };
            var shapes = new List<ToyNpcShape>();
            foreach (var kv in homes)
            {
                var root = byName[kv.Key]; var actor = root.GetComponent<NpcActor>();
                var visual = root.Find("Visual");
                for (int i = visual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(visual.GetChild(i).gameObject);
                var old = visual.GetComponent<ToyNpcShape>(); if (old != null) Object.DestroyImmediate(old);
                Color jacket = kv.Key == "Neighbor_A" ? pal.npcJacketA : pal.npcJacketB;
                var jacketMat = Toon(kv.Key == "Neighbor_A" ? "Npc_JacketA_Toon" : "Npc_JacketB_Toon", jacket);
                var shape = CalibrationSceneBuilder.BuildNpcShape(visual, p.npcHeight, p.npcHeadRatio, mat, false, jacketMat, out Renderer[] tinted);
                float s = shape.Shape.localScale.x, h = p.npcHeight;
                shape.Shape.localPosition = new Vector3(0, -h / 2, 0);
                root.position = new Vector3(kv.Value.x, h / 2, kv.Value.z);
                var cap = root.GetComponent<CapsuleCollider>(); cap.radius = .23f * s; cap.height = h; cap.center = Vector3.zero;
                Vector3 to = player - root.position; visual.localRotation = Quaternion.Euler(0, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 0);
                var torso = shape.Shape.Find("Torso").GetComponent<Renderer>();
                Ref(actor, "bodyRenderer", torso); Ref(actor, "visual", visual); Refs(actor, "tintedParts", tinted);
                Float(actor, "feetOffset", h / 2);
                Float(actor, "panicRadius", bl.Value("npc:panic", GetFloat(actor, "panicRadius")) * p.distanceScale);
                Float(actor, "charRadius", bl.Value("npc:char", GetFloat(actor, "charRadius")) * p.distanceScale);
                Float(actor, "maxSpeed", p.npcMaxSpeed);
                var ex = root.GetComponent<NpcExpression>();
                Ref(ex, "body", torso); Ref(ex, "mouth", shape.Mouth); Refs(ex, "eyes", shape.Eyes); Refs(ex, "pupils", shape.Pupils); Refs(ex, "arms", shape.Arms);
                shapes.Add(shape);
            }
            npcShapes = shapes.ToArray();
        }
        static ToyNpcShape[] npcShapes;

        // ---------------------------------------------------------------- camera, hands, lenses

        static void CameraAndHands(Dictionary<string, Transform> byName)
        {
            int layer = CalibrationSceneBuilder.ViewLayer();
            var camT = byName["Camera"]; var cam = camT.GetComponent<Camera>();
            cam.cullingMask = ~(1 << layer); cam.fieldOfView = p.worldFov;
            camT.localPosition = new Vector3(0, p.cameraHeight, 0);
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;

            var oldVm = camT.Find("ViewmodelCamera"); if (oldVm != null) Object.DestroyImmediate(oldVm.gameObject);
            var oldHands = camT.Find("FirefighterHands"); if (oldHands != null) Object.DestroyImmediate(oldHands.gameObject);
            var vmGo = new GameObject("ViewmodelCamera"); vmGo.transform.SetParent(camT, false);
            var vm = vmGo.AddComponent<Camera>(); vm.nearClipPlane = .03f; vm.farClipPlane = 10f; vm.fieldOfView = p.viewmodelFov; vm.cullingMask = 1 << layer;
            vm.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            data.cameraStack.Clear(); data.cameraStack.Add(vm);

            Func<string, Color, Material> mat = (n, c) =>
            {
                switch (n)
                {
                    case "Sleeve": return Toon("Hand_Sleeve_Toon", pal.sleeve); case "Glove": return Toon("Hand_Glove_Toon", pal.glove);
                    case "Dirt": return Toon("Hand_Dirt_Toon", pal.dirt); default: return Toon("Npc_Stripe_Toon", pal.npcStripe);
                }
            };
            var rig = CalibrationSceneBuilder.BuildHands(vm.transform, layer, mat);
            var go = new GameObject("FirefighterHands"); go.transform.SetParent(vm.transform, false);
            var motion = go.AddComponent<FirstPersonHands>(); go.layer = layer;
            var tuning = AssetDatabase.LoadAssetAtPath<VisualTuning>(Root + "/Settings/VisualTuning.asset");
            Ref(motion, "arms", rig); Ref(motion, "hose", Object.FindFirstObjectByType<HoseController>()); Ref(motion, "tuning", tuning);
            Ref(Object.FindFirstObjectByType<GameBootstrap>(), "hands", motion);
            Ref(camT.GetComponentInParent<CameraFeedback>(), "scale", p);

            var oldRig = byName.TryGetValue("ScaleRig", out var existing) ? existing : null; if (oldRig != null) Object.DestroyImmediate(oldRig.gameObject);
            var scaleRig = new GameObject("ScaleRig"); var applier = scaleRig.AddComponent<ScaleApplier>();
            applier.Bind(p, cam, vm, rig, npcShapes, new ScaledProp[0]); applier.ApplyCameraPitch = false;
        }

        // ---------------------------------------------------------------- mechanics scaled to the new room

        static void Mechanics(Dictionary<string, Transform> byName)
        {
            float W = p.roomWidth, L = p.roomLength;
            var player = byName["Player"];
            Float(player.GetComponent<FlameBlame.Player.PlayerMotor>(), "moveSpeed", p.moveSpeed);
            Float(player.GetComponent<FlameBlame.Player.PlayerLook>(), "sensitivity", p.lookSensitivity);

            // Hose long enough to reach the farthest corner from the hydrant.
            var hose = Object.FindFirstObjectByType<HoseController>();
            var anchor = (Transform)new SerializedObject(hose).FindProperty("anchor").objectReferenceValue;
            float far = 0;
            foreach (var c in new[] { new Vector2(-W / 2, -L / 2), new Vector2(W / 2, -L / 2), new Vector2(-W / 2, L / 2), new Vector2(W / 2, L / 2) })
                far = Mathf.Max(far, Vector2.Distance(c, new Vector2(anchor.position.x, anchor.position.z)));
            var hs = new SerializedObject(hose);
            hs.FindProperty("segments").intValue = p.hoseSegments;
            hs.FindProperty("segmentLength").floatValue = far * p.hoseReachMargin / ((p.hoseSegments - 1) * .92f);
            hs.FindProperty("grabDistance").floatValue = p.hoseGrabDistance; hs.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[FlameBlame] Hose reach " + ((p.hoseSegments - 1) * far * p.hoseReachMargin / ((p.hoseSegments - 1))).ToString("0.0") + " m of " + far.ToString("0.0") + " m to the farthest corner.");

            var jet = Object.FindFirstObjectByType<JetController>();
            Float(jet, "rangeFactor", p.jetRangeFactor); Float(jet, "jetRadius", p.jetRadius);

            var floor = byName["Floor"].GetComponent<WaterSurface>();
            Float(floor, "puddleDiameter", bl.Value("puddle:d", GetFloat(floor, "puddleDiameter")) * p.puddleScale);
            Float(floor, "minSpacing", bl.Value("puddle:s", GetFloat(floor, "minSpacing")) * p.puddleScale);

            foreach (string n in new[] { "Wood", "Fabric", "Paper", "Metal" })
            {
                var prof = AssetDatabase.LoadAssetAtPath<FireMaterialProfile>(Root + "/Settings/Fire/FireProfile_" + n + ".asset");
                if (prof == null) continue;
                prof.spreadRadius = bl.Value("fire:" + n, prof.spreadRadius) * p.distanceScale; EditorUtility.SetDirty(prof);
            }
            var boot = Object.FindFirstObjectByType<GameBootstrap>();
            var fx = (Transform)new SerializedObject(boot).FindProperty("effectsRoot").objectReferenceValue;
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main; main.startSizeMultiplier = bl.Value("ps:" + Path(ps.transform, fx), main.startSizeMultiplier) * p.particleScale;
            }
        }
        static string Path(Transform t, Transform stop) { string s = t.name; while (t.parent != null && t.parent != stop) { t = t.parent; s = t.name + "/" + s; } return s; }

        // ---------------------------------------------------------------- serialized helpers

        static float GetFloat(Object o, string f) => new SerializedObject(o).FindProperty(f).floatValue;
        static void Float(Object o, string f, float v)
        {
            var so = new SerializedObject(o); var pr = so.FindProperty(f);
            if (pr == null) { Debug.LogError("[FlameBlame] " + o.GetType().Name + " has no field " + f); return; }
            pr.floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(o);
        }
        static void Ref(Object o, string f, Object v) { var so = new SerializedObject(o); so.FindProperty(f).objectReferenceValue = v; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(o); }
        static void Refs(Object o, string f, Object[] v)
        {
            var so = new SerializedObject(o); var pr = so.FindProperty(f); pr.arraySize = v.Length;
            for (int i = 0; i < v.Length; i++) pr.GetArrayElementAtIndex(i).objectReferenceValue = v[i];
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(o);
        }

        // ---------------------------------------------------------------- batch: rebuild + review captures

        public static void RunBatch()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs(); string tag = "p7";
                for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-calibTag") tag = args[i + 1];
                Rebuild();
                Directory.CreateDirectory("Captures/Calibration");
                Captures(tag);
                Debug.Log("[FlameBlame] Main rebuild + captures done: " + tag);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Captures/Calibration/error.txt", e.ToString()); EditorApplication.Exit(1); }
        }

        static void Captures(string tag)
        {
            var cam = Camera.main; var vm = cam.transform.Find("ViewmodelCamera").GetComponent<Camera>();
            var player = cam.transform.parent;
            var hands = vm.transform.Find("Hands");
            int w = 1920, h = 1080; cam.aspect = vm.aspect = w / (float)h;
            // View A: player start, framed like the calibration shot (pitch 18) toward the room centre.
            var startRot = player.rotation; var startPitch = cam.transform.localRotation;
            player.rotation = Quaternion.Euler(0, 38, 0); cam.transform.localRotation = Quaternion.Euler(p.cameraPitch, 0, 0);
            var viewA = WithHands(cam, vm, w, h);
            File.WriteAllBytes("Captures/Calibration/" + tag + "_main_playerview.png", viewA.EncodeToPNG());
            // View B: corner overview (hands hidden, camera moved out of the player rig temporarily).
            hands.gameObject.SetActive(false);
            var pos = cam.transform.position; var rot = cam.transform.rotation; var fov = cam.fieldOfView;
            cam.transform.position = new Vector3(p.roomWidth / 2 - .1f, p.roomHeight - .25f, -p.roomLength / 2 + .1f); cam.transform.rotation = Quaternion.Euler(32, -40, 0); cam.fieldOfView = 100;
            cam.GetUniversalAdditionalCameraData().cameraStack.Clear();
            var viewB = CalibrationSheets.Grab(cam, w, h);
            File.WriteAllBytes("Captures/Calibration/" + tag + "_main_overview.png", viewB.EncodeToPNG());
            // Ceiling must not change the lighting: compare mean brightness with the ceiling off.
            var ceiling = byNameCeiling();
            float on = Mean(viewB); ceiling.SetActive(false);
            var viewOff = CalibrationSheets.Grab(cam, w, h); float off = Mean(viewOff); ceiling.SetActive(true);
            File.WriteAllText("Captures/Calibration/" + tag + "_ceiling_check.txt", "mean luminance ceiling on=" + on.ToString("0.0000") + " off=" + off.ToString("0.0000") + " (bottom half of the frame only (floor and furniture, no ceiling pixels))\n");
            // Three-way comparison: reference 14 | calibration scene | main level.
            var reference = new Texture2D(2, 2); reference.LoadImage(File.ReadAllBytes("Reference/14_locker_room_scale_ref.jpg"));
            string calPath = "Captures/Calibration/it4_final.png"; var calib = new Texture2D(2, 2); calib.LoadImage(File.ReadAllBytes(calPath));
            var all = new Texture2D(3072, 576, TextureFormat.RGB24, false);
            all.SetPixels(0, 0, 1024, 576, CalibrationSheets.Resize(reference, 1024, 576).GetPixels());
            all.SetPixels(1024, 0, 1024, 576, CalibrationSheets.Resize(calib, 1024, 576).GetPixels());
            all.SetPixels(2048, 0, 1024, 576, CalibrationSheets.Resize(viewA, 1024, 576).GetPixels());
            File.WriteAllBytes("Captures/Calibration/" + tag + "_compare_ref_calib_main.png", all.EncodeToPNG());
            cam.transform.position = pos; cam.transform.rotation = rot; cam.fieldOfView = fov; player.rotation = startRot; cam.transform.localRotation = startPitch;
            hands.gameObject.SetActive(true);
        }
        static GameObject byNameCeiling() => GameObject.Find("Room").transform.Find("Ceiling").gameObject;
        static float Mean(Texture2D t) { var px = t.GetPixels32(); double s = 0; for (int i = 0; i < px.Length / 2; i += 7) s += (px[i].r * .2126 + px[i].g * .7152 + px[i].b * .0722) / 255.0; return (float)(s / (px.Length / 14)); }

        static Texture2D WithHands(Camera cam, Camera vm, int w, int h)
        {
            cam.GetUniversalAdditionalCameraData().cameraStack.Clear();
            var world = CalibrationSheets.Grab(cam, w, h);
            vm.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
            vm.clearFlags = CameraClearFlags.SolidColor; vm.backgroundColor = Color.black; var black = CalibrationSheets.Grab(vm, w, h);
            vm.backgroundColor = Color.white; var white = CalibrationSheets.Grab(vm, w, h);
            var wp = world.GetPixels32(); var bp = black.GetPixels32(); var hp = white.GetPixels32();
            for (int i = 0; i < wp.Length; i++)
                if (Mathf.Abs(bp[i].r - hp[i].r) + Mathf.Abs(bp[i].g - hp[i].g) + Mathf.Abs(bp[i].b - hp[i].b) <= 60) wp[i] = bp[i];
            world.SetPixels32(wp); world.Apply(); return world;
        }
    }
}
