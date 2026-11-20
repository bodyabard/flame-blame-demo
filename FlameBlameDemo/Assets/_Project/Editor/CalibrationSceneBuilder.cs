using System.Collections.Generic;
using FlameBlame.Core;
using FlameBlame.Feedback;
using FlameBlame.Npc;
using FlameBlame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Build Calibration Scene: compact locker room matching Reference/14, driven by ScaleProfile.
    public static class CalibrationSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/ReferenceCalibration.unity";
        public const string ProfilePath = "Assets/_Project/Settings/ScaleProfile.asset";
        const string MatDir = "Assets/_Project/Art/Calibration";
        const string ViewLayerName = "ViewModel";
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public static ScaleProfile LoadProfile()
        {
            var p = AssetDatabase.LoadAssetAtPath<ScaleProfile>(ProfilePath);
            if (p == null) { p = ScriptableObject.CreateInstance<ScaleProfile>(); AssetDatabase.CreateAsset(p, ProfilePath); AssetDatabase.SaveAssets(); }
            return p;
        }

        [MenuItem("Tools/FlameBlame/Build Calibration Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before building.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildContent(LoadProfile(), true);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] Calibration scene built: " + ScenePath);
        }

        public static int ViewLayer()
        {
            var tag = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tag.FindProperty("layers");
            for (int i = 8; i < 32; i++) if (layers.GetArrayElementAtIndex(i).stringValue == ViewLayerName) return i;
            for (int i = 12; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                { layers.GetArrayElementAtIndex(i).stringValue = ViewLayerName; tag.ApplyModifiedProperties(); return i; }
            throw new System.InvalidOperationException("No free layer for ViewModel.");
        }

        static Material Mat(string name, Color c)
        {
            if (mats.TryGetValue(name, out var cached) && cached != null) { cached.SetColor("_BaseColor", c); return cached; }
            System.IO.Directory.CreateDirectory(MatDir);
            string path = MatDir + "/Cal_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m); mats[name] = m; return m;
        }

        public static Transform Part(Transform parent, string name, Vector3 pos, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m; return go.transform;
        }

        public static Transform Group(Transform parent, string name, Vector3 pos)
        {
            var g = new GameObject(name).transform; g.SetParent(parent, false); g.localPosition = pos; return g;
        }

        public static GameObject BuildContent(ScaleProfile p, bool withPanel)
        {
            mats.Clear();
            int viewLayer = ViewLayer();
            var root = new GameObject("CalibrationRoot").transform;
            float W = p.roomWidth, L = p.roomLength, H = p.roomHeight;

            var peach = Mat("Peach", new Color(.96f, .74f, .58f)); var wainscot = Mat("Wainscot", new Color(.83f, .39f, .2f));
            var floor = Mat("Floor", new Color(.43f, .48f, .58f)); var yellow = Mat("Yellow", new Color(1f, .82f, .2f));
            var ceiling = Mat("Ceiling", new Color(.93f, .8f, .68f));
            var locker = Mat("Locker", new Color(.52f, .45f, .47f)); var lockerDark = Mat("LockerDark", new Color(.34f, .29f, .33f));
            var doorMat = Mat("Door", new Color(.82f, .62f, .45f)); var wood = Mat("Bench", new Color(.78f, .42f, .22f));

            // Room shell
            var room = Group(root, "Room", Vector3.zero);
            Part(room, "Floor", new Vector3(0, -.05f, L / 2), new Vector3(W, .1f, L), floor);
            Part(room, "Ceiling", new Vector3(0, H + .05f, L / 2), new Vector3(W, .1f, L), ceiling);
            Part(room, "WallFar", new Vector3(0, H / 2, L + .05f), new Vector3(W + .2f, H, .1f), peach);
            Part(room, "WallNear", new Vector3(0, H / 2, -.05f), new Vector3(W + .2f, H, .1f), peach);
            Part(room, "WallLeft", new Vector3(-W / 2 - .05f, H / 2, L / 2), new Vector3(.1f, H, L), peach);
            Part(room, "WallRight", new Vector3(W / 2 + .05f, H / 2, L / 2), new Vector3(.1f, H, L), peach);
            Part(room, "WainscotFar", new Vector3(0, .5f, L - .01f), new Vector3(W, 1f, .04f), wainscot);
            Part(room, "WainscotLeft", new Vector3(-W / 2 + .01f, .5f, L / 2), new Vector3(.04f, 1f, L), wainscot);
            Part(room, "WainscotRight", new Vector3(W / 2 - .01f, .5f, L / 2), new Vector3(.04f, 1f, L), wainscot);

            // Door on the far wall
            float doorW = p.doorHeight * .43f;
            var door = Group(room, "Door", new Vector3(W * .12f, 0, L - .03f));
            Part(door, "DoorFrame", new Vector3(0, p.doorHeight / 2 + .03f, 0), new Vector3(doorW + .12f, p.doorHeight + .06f, .05f), lockerDark);
            Part(door, "DoorLeaf", new Vector3(0, p.doorHeight / 2, -.02f), new Vector3(doorW, p.doorHeight, .05f), doorMat);
            Part(door, "DoorWindow", new Vector3(0, p.doorHeight * .72f, -.05f), new Vector3(doorW * .32f, p.doorHeight * .22f, .03f), Mat("Glass", new Color(.62f, .7f, .7f)));
            Part(door, "DoorHandle", new Vector3(doorW * .38f, p.doorHeight * .5f, -.06f), new Vector3(.08f, .05f, .05f), Mat("Metal", new Color(.7f, .7f, .72f)));

            // Floor marking: two yellow strips, one angled like the walkway in the reference
            var line = Part(room, "YellowLineA", new Vector3(-W * .22f, .006f, 1.1f), new Vector3(W * .75f, .012f, .14f), yellow);
            line.localRotation = Quaternion.Euler(0, -14, 0);
            Part(room, "YellowLineB", new Vector3(W * .1f, .006f, L - 1.2f), new Vector3(W * .7f, .012f, .14f), yellow);

            LockerBank(room, "LockersLeft", -W / 2 + .26f, 90, 1.2f, L - .4f, H, locker, lockerDark, Mat("Helmet", new Color(.86f, .26f, .12f)), false);
            LockerBank(room, "LockersRight", W / 2 - .26f, -90, 1.2f, L - .4f, H, locker, lockerDark, Mat("Helmet", new Color(.86f, .26f, .12f)), true);

            // Bench set: bench, bag, helmet, hose coil, nozzle. Pivot on the floor so scaling stays grounded.
            var bench = Group(root, "BenchSet", new Vector3(-.1f, 0, 1.6f));
            Part(bench, "BenchTop", new Vector3(0, .42f, 0), new Vector3(2.4f, .08f, .5f), wood);
            Part(bench, "BenchLegL", new Vector3(-1.05f, .2f, 0), new Vector3(.08f, .4f, .42f), wood);
            Part(bench, "BenchLegR", new Vector3(1.05f, .2f, 0), new Vector3(.08f, .4f, .42f), wood);
            var bag = Group(bench, "Bag", new Vector3(-.1f, .46f, 0));
            var bagMat = Mat("Bag", new Color(.72f, .3f, .2f)); var bagDark = Mat("BagDark", new Color(.35f, .24f, .26f));
            Part(bag, "BagBody", new Vector3(0, .17f, 0), new Vector3(.8f, .34f, .42f), bagMat, PrimitiveType.Cube);
            Part(bag, "BagTrim", new Vector3(0, .02f, 0), new Vector3(.82f, .06f, .44f), bagDark);
            Part(bag, "BagHandle", new Vector3(0, .4f, 0), new Vector3(.3f, .05f, .05f), bagDark);
            var helmet = Group(bench, "BenchHelmet", new Vector3(.7f, .46f, .02f));
            Part(helmet, "Dome", new Vector3(0, .02f, 0), new Vector3(.34f, .22f, .34f), Mat("Helmet", new Color(.86f, .26f, .12f)), PrimitiveType.Sphere);
            Part(helmet, "Brim", new Vector3(0, .01f, 0), new Vector3(.46f, .02f, .46f), Mat("Helmet", new Color(.86f, .26f, .12f)), PrimitiveType.Cylinder);
            var hose = Group(bench, "HoseCoil", new Vector3(-.95f, .46f, .02f));
            var beige = Mat("Hose", new Color(.87f, .8f, .68f)); var steel = Mat("Metal", new Color(.7f, .7f, .72f));
            for (int i = 0; i < 3; i++) Part(hose, "Ring", new Vector3(0, .03f + i * .03f, 0), new Vector3(.52f - i * .03f, .018f, .52f - i * .03f), beige, PrimitiveType.Cylinder);
            var nozzle = Part(hose, "Nozzle", new Vector3(-.3f, .14f, .1f), new Vector3(.12f, .12f, .26f), steel, PrimitiveType.Cylinder);
            nozzle.localRotation = Quaternion.Euler(70, 20, 0);

            // NPCs: 02 leans over the bag, 01 stands at the right of the bench
            var npcA = BuildNpc(root, "NPC02", new Vector3(-.6f, 0, 2.3f), 8f, p);
            var npcB = BuildNpc(root, "NPC01", new Vector3(1.1f, 0, 2.2f), -12f, p);

            // Player rig + cameras (URP camera stack: world base + viewmodel overlay)
            var rig = Group(root, "PlayerRig", new Vector3(0, 0, .2f));
            var camGo = new GameObject("WorldCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(rig, false); camGo.transform.localPosition = new Vector3(0, p.cameraHeight, 0);
            var world = camGo.AddComponent<Camera>(); world.nearClipPlane = .05f; world.farClipPlane = 60f; world.fieldOfView = p.worldFov;
            world.clearFlags = CameraClearFlags.SolidColor; world.backgroundColor = new Color(.2f, .12f, .1f);
            world.cullingMask = ~(1 << viewLayer);
            camGo.AddComponent<AudioListener>();
            var vmGo = new GameObject("ViewmodelCamera"); vmGo.transform.SetParent(camGo.transform, false);
            var vm = vmGo.AddComponent<Camera>(); vm.nearClipPlane = .03f; vm.farClipPlane = 10f; vm.fieldOfView = p.viewmodelFov; vm.cullingMask = 1 << viewLayer;
            var baseData = world.GetUniversalAdditionalCameraData(); var vmData = vm.GetUniversalAdditionalCameraData();
            vmData.renderType = CameraRenderType.Overlay; baseData.cameraStack.Add(vm);
            var hands = BuildHands(vmGo.transform, viewLayer, Mat);

            Lights(root);

            var props = new List<ScaledProp>();
            var benchProp = bench.gameObject.AddComponent<ScaledProp>(); benchProp.Capture(); props.Add(benchProp);
            var shapes = new[] { npcA, npcB };
            var applier = root.gameObject.AddComponent<ScaleApplier>();
            applier.Bind(p, world, vm, hands, shapes, props.ToArray());
            applier.Apply();
            if (withPanel) { var panel = root.gameObject.AddComponent<CalibrationPanel>(); panel.Bind(applier); }
            return root.gameObject;
        }

        static void LockerBank(Transform parent, string name, float x, float yaw, float z0, float z1, float H, Material body, Material dark, Material helmetMat, bool coats)
        {
            var bank = Group(parent, name, new Vector3(x, 0, 0));
            float h = H * .68f, w = .5f; int count = Mathf.FloorToInt((z1 - z0) / w);
            float side = yaw > 0 ? 1f : -1f; // direction the doors face (toward room centre)
            for (int i = 0; i < count; i++)
            {
                float z = z0 + (i + .5f) * w;
                var l = Group(bank, "Locker", new Vector3(0, 0, z));
                Part(l, "Body", new Vector3(0, h / 2, 0), new Vector3(.5f, h, w - .02f), body);
                Part(l, "DoorInset", new Vector3(side * .26f, h / 2, 0), new Vector3(.02f, h * .96f, w - .1f), dark);
                for (int v = 0; v < 3; v++) Part(l, "Vent", new Vector3(side * .275f, h * .84f - v * .05f, 0), new Vector3(.02f, .012f, w * .5f), body);
                Part(l, "Handle", new Vector3(side * .28f, h * .55f, w * .3f), new Vector3(.03f, .12f, .03f), dark);
                if (i % 2 == 0)
                    Part(l, "TopHelmet", new Vector3(0, h + .1f, 0), new Vector3(.34f, .22f, .34f), helmetMat, PrimitiveType.Sphere);
                if (coats && i % 3 == 1)
                {
                    var coat = Part(l, "Coat", new Vector3(side * .1f, h * .62f, 0), new Vector3(.2f, h * .5f, w * .7f), Mat("Coat", new Color(.95f, .66f, .15f)));
                    Part(coat, "CoatStripe", new Vector3(0, -.1f, 0), new Vector3(1.02f, .06f, 1.02f), Mat("Yellow", new Color(1f, .82f, .2f)));
                }
            }
        }

        public static ToyNpcShape BuildNpc(Transform parent, string name, Vector3 pos, float yaw, ScaleProfile p)
        {
            var npc = Group(parent, name, pos); npc.localRotation = Quaternion.Euler(0, 180 + yaw, 0);
            return BuildNpcShape(npc, p.npcHeight, p.npcHeadRatio, Mat, true, Mat("Jacket", new Color(.96f, .64f, .16f)), out _);
        }

        /// Shared toy-NPC construction (calibration scene and playable level). Parts are children of `shape`; returns the bound component on `owner`.
        public static ToyNpcShape BuildNpcShape(Transform owner, float height, float headRatio, System.Func<string, Color, Material> mat, bool helmet, Material jacketMat, out Renderer[] tinted)
        {
            var shape = Group(owner, "Shape", Vector3.zero);
            var skin = mat("Skin", new Color(.88f, .43f, .28f)); var helmetMat = helmet ? mat("Helmet", new Color(.86f, .26f, .12f)) : null;
            var stripe = mat("Yellow", new Color(1f, .82f, .2f)); var boot = mat("Boot", new Color(.3f, .22f, .2f));
            var eyeW = mat("Eye", new Color(.98f, .97f, .9f)); var pupil = mat("Pupil", new Color(.03f, .03f, .04f));
            var glove = mat("NpcGlove", new Color(.78f, .62f, .26f));
            var torso = Part(shape, "Torso", Vector3.zero, Vector3.one, jacketMat, PrimitiveType.Sphere);
            var head = Part(shape, "Head", Vector3.zero, Vector3.one, skin, PrimitiveType.Sphere);
            Transform hm = null, brim = null, badge = null;
            if (helmet)
            {
                hm = Part(shape, "Helmet", Vector3.zero, Vector3.one, helmetMat, PrimitiveType.Sphere);
                brim = Part(shape, "Brim", Vector3.zero, Vector3.one, helmetMat, PrimitiveType.Cylinder);
                badge = Part(shape, "Badge", Vector3.zero, Vector3.one, stripe);
            }
            var legs = new Transform[2]; var arms = new Transform[2]; var eyes = new Transform[2]; var pupils = new Transform[2];
            var parts = new List<Renderer> { head.GetComponent<Renderer>() };
            for (int i = 0; i < 2; i++)
            {
                legs[i] = Part(shape, "Leg", Vector3.zero, Vector3.one, boot, PrimitiveType.Cylinder);
                arms[i] = Group(shape, "ArmPivot", Vector3.zero);
                parts.Add(Part(arms[i], "Arm", Vector3.zero, Vector3.one, jacketMat, PrimitiveType.Capsule).GetComponent<Renderer>());
                parts.Add(Part(arms[i], "Glove", Vector3.zero, Vector3.one, glove, PrimitiveType.Sphere).GetComponent<Renderer>());
                eyes[i] = Part(shape, "Eye", Vector3.zero, Vector3.one, eyeW, PrimitiveType.Sphere);
                pupils[i] = Part(shape, "Pupil", Vector3.zero, Vector3.one, pupil, PrimitiveType.Sphere);
            }
            var mouth = Part(shape, "Mouth", Vector3.zero, Vector3.one, pupil, PrimitiveType.Sphere);
            var stripes = new[] { Part(shape, "Stripe", Vector3.zero, Vector3.one, stripe, PrimitiveType.Cylinder), Part(shape, "Stripe", Vector3.zero, Vector3.one, stripe, PrimitiveType.Cylinder) };
            var comp = owner.gameObject.AddComponent<ToyNpcShape>();
            comp.Bind(shape, torso, head, hm, brim, badge, mouth, legs, arms, eyes, pupils, stripes);
            comp.Apply(height, headRatio);
            tinted = parts.ToArray();
            return comp;
        }

        public static Transform BuildHands(Transform camera, int layer, System.Func<string, Color, Material> mat)
        {
            var rig = Group(camera, "Hands", Vector3.zero);
            var sleeve = mat("Sleeve", new Color(.5f, .43f, .2f)); var glove = mat("Glove", new Color(.45f, .38f, .17f));
            var dirt = mat("Dirt", new Color(.28f, .24f, .16f)); var stripe = mat("Yellow", new Color(1f, .82f, .2f));
            for (int s = -1; s <= 1; s += 2)
            {
                var arm = Group(rig, s < 0 ? "ArmL" : "ArmR", new Vector3(s * .40f, -.34f, .20f));
                arm.localRotation = Quaternion.Euler(-14, -s * 13, s * 5);
                Part(arm, "Sleeve", new Vector3(0, 0, .12f), new Vector3(.22f, .20f, .70f), sleeve);
                Part(arm, "Dirt", new Vector3(s * .02f, .102f, .12f), new Vector3(.12f, .012f, .22f), dirt);
                Part(arm, "Cuff", new Vector3(0, 0, .43f), new Vector3(.235f, .215f, .06f), stripe);
                var hand = Group(arm, "Hand", new Vector3(0, -.02f, .56f));
                Part(hand, "Palm", new Vector3(0, 0, .06f), new Vector3(.20f, .13f, .20f), glove);
                for (int f = 0; f < 3; f++)
                {
                    var finger = Part(hand, "Finger", new Vector3(-.065f + f * .065f, -.03f, .21f), new Vector3(.055f, .06f, .12f), glove);
                    finger.localRotation = Quaternion.Euler(30, 0, 0);
                }
                var thumb = Part(hand, "Thumb", new Vector3(-s * .12f, -.02f, .10f), new Vector3(.06f, .07f, .15f), glove);
                thumb.localRotation = Quaternion.Euler(0, s * 25, 0);
            }
            SetLayer(rig, layer);
            return rig;
        }

        public static void SetLayer(Transform t, int layer) { t.gameObject.layer = layer; foreach (Transform c in t) SetLayer(c, layer); }

        static void Lights(Transform root)
        {
            var key = new GameObject("Warm Key").AddComponent<Light>(); key.transform.SetParent(root, false);
            key.type = LightType.Directional; key.color = new Color(1f, .78f, .55f); key.intensity = 1.25f; key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(52, -28, 0);
            var fill = new GameObject("Soft Fill").AddComponent<Light>(); fill.transform.SetParent(root, false);
            fill.type = LightType.Directional; fill.color = new Color(.7f, .78f, 1f); fill.intensity = .35f; fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(30, 150, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.72f, .6f, .56f);
            RenderSettings.sun = key;
        }
    }
}
