using System.IO;
using FlameBlame.Core;
using FlameBlame.Feedback;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Player;
using FlameBlame.Water;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Build Greybox: layers, materials, fire profiles and the Main scene. Safe to re-run.
    public static class GreyboxBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Main.unity";

        static int layerPlayer, layerHose, layerInteractable, layerFloor;
        static LayerMask worldMask;

        // ---------------------------------------------------------------- entry

        [MenuItem("Tools/FlameBlame/Setup Layers")]
        public static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            EnsureLayer(layers, 8, "Player");
            EnsureLayer(layers, 9, "Hose");
            EnsureLayer(layers, 10, "Interactable");
            EnsureLayer(layers, 11, "Floor");
            tagManager.ApplyModifiedProperties();

            layerPlayer = LayerMask.NameToLayer("Player");
            layerHose = LayerMask.NameToLayer("Hose");
            layerInteractable = LayerMask.NameToLayer("Interactable");
            layerFloor = LayerMask.NameToLayer("Floor");
            worldMask = ~((1 << layerPlayer) | (1 << layerHose));
        }

        [MenuItem("Tools/FlameBlame/Build Greybox")]
        public static void Build()
        {
            SetupLayers();
            EnsureFolders();

            var mats = CreateMaterials();
            var profiles = CreateProfiles();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildRoom(mats);
            var anchor = BuildHydrant(mats);
            BuildZoneA(mats, profiles);
            BuildZoneB(mats, profiles);
            var player = BuildPlayer(out Transform cam, out Transform hand);
            var hoseObj = BuildHose(mats, anchor, hand, out Transform nozzle);
            var jetObj = BuildJet(mats);
            var fx = BuildEffects(mats);
            BuildBootstrap(player, hoseObj, jetObj, fx, nozzle, cam);
            PhaseTwoBuilder.UpgradeOpenScene();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] Greybox built: " + ScenePath);
        }

        // ---------------------------------------------------------------- setup helpers

        static void EnsureLayer(SerializedProperty layers, int index, string layerName)
        {
            var p = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = layerName;
            else if (p.stringValue != layerName)
                Debug.LogWarning($"[FlameBlame] Layer {index} is '{p.stringValue}', expected '{layerName}'.");
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { "Scenes", "Materials", "Settings", "Settings/Fire", "Prefabs", "VFX" })
                Directory.CreateDirectory(Path.Combine(Root, f));
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------- materials & profiles

        sealed class Mats
        {
            public Material Floor, Wall, Fabric, Wood, Paper, Metal, Hydrant, Hose, Dark;
            public Material Water, Flame, Steam, Splash, Puddle;
        }

        static Mats CreateMaterials() => new Mats
        {
            Floor = Lit("Floor", new Color(0.42f, 0.33f, 0.27f)),
            Wall = Lit("Wall", new Color(0.78f, 0.66f, 0.55f)),
            Fabric = Lit("Fabric", new Color(0.72f, 0.2f, 0.22f), 0f, 0.1f),
            Wood = Lit("Wood", new Color(0.52f, 0.34f, 0.18f), 0f, 0.25f),
            Paper = Lit("Paper", new Color(0.92f, 0.88f, 0.76f), 0f, 0.05f),
            Metal = Lit("Metal", new Color(0.55f, 0.58f, 0.62f), 0.85f, 0.55f),
            Hydrant = Lit("Hydrant", new Color(0.85f, 0.12f, 0.1f), 0.2f, 0.5f),
            Dark = Lit("Dark", new Color(0.15f, 0.15f, 0.17f)),
            Hose = Unlit("Hose", new Color(0.95f, 0.72f, 0.1f), false, false),
            Water = Particle("JetWater", new Color(0.6f, 0.82f, 1f, 0.8f), false),
            Flame = Particle("Flame", new Color(1f, 0.6f, 0.2f, 1f), true),
            Steam = Particle("Steam", new Color(0.9f, 0.9f, 0.95f, 0.6f), false),
            Splash = Particle("Splash", new Color(0.7f, 0.88f, 1f, 0.9f), false),
            Puddle = Unlit("Puddle", new Color(0.25f, 0.5f, 0.85f, 0.55f), true, false),
        };

        static Material LoadOrCreate(string name, string shaderName)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = Shader.Find(shaderName);
            return m;
        }

        static Material Lit(string name, Color color, float metallic = 0f, float smoothness = 0.2f)
        {
            var m = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            // Emission must be enabled on the material for Flammable's MaterialPropertyBlock glow to show.
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Unlit(string name, Color color, bool transparent, bool additive)
        {
            var m = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", color);
            if (transparent) MakeTransparent(m, additive);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Particle(string name, Color color, bool additive)
        {
            var m = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
            m.SetColor("_BaseColor", color);
            MakeTransparent(m, additive);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void MakeTransparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        sealed class Profiles { public FireMaterialProfile Wood, Fabric, Paper, Metal; }

        static Profiles CreateProfiles() => new Profiles
        {
            Wood = Profile("Wood", p =>
            {
                p.ignitionThreshold = 100f; p.flammability = 0.6f; p.coolRate = 6f;
                p.burnDuration = 20f; p.spreadHeatPerSecond = 40f; p.spreadRadius = 1.6f;
                p.waterCooling = 70f; p.charColor = new Color(0.06f, 0.05f, 0.04f);
                p.damageOnIgnite = 50; p.damageOnBurnOut = 100; p.flameScale = 1.2f;
            }),
            Fabric = Profile("Fabric", p =>
            {
                p.ignitionThreshold = 50f; p.flammability = 1.4f; p.coolRate = 4f;
                p.burnDuration = 10f; p.spreadHeatPerSecond = 60f; p.spreadRadius = 1.8f;
                p.waterCooling = 120f; p.charColor = new Color(0.1f, 0.08f, 0.08f);
                p.damageOnIgnite = 30; p.damageOnBurnOut = 60; p.flameScale = 1f;
            }),
            Paper = Profile("Paper", p =>
            {
                p.ignitionThreshold = 30f; p.flammability = 2f; p.coolRate = 3f;
                p.burnDuration = 5f; p.spreadHeatPerSecond = 70f; p.spreadRadius = 1.5f;
                p.waterCooling = 150f; p.charColor = new Color(0.12f, 0.1f, 0.09f);
                p.damageOnIgnite = 10; p.damageOnBurnOut = 20; p.flameScale = 0.6f;
            }),
            Metal = Profile("Metal", p =>
            {
                p.canBurn = false; p.ignitionThreshold = 200f; p.flammability = 0.3f; p.coolRate = 12f;
                p.burnDuration = 0f; p.spreadHeatPerSecond = 0f; p.spreadRadius = 0f;
                p.waterCooling = 200f; p.damageOnIgnite = 0; p.damageOnBurnOut = 0;
            }),
        };

        /// Creates the profile once; later runs keep the user's tuning.
        static FireMaterialProfile Profile(string name, System.Action<FireMaterialProfile> init)
        {
            string path = $"{Root}/Settings/Fire/FireProfile_{name}.asset";
            var p = AssetDatabase.LoadAssetAtPath<FireMaterialProfile>(path);
            if (p != null) return p;
            p = ScriptableObject.CreateInstance<FireMaterialProfile>();
            init(p);
            AssetDatabase.CreateAsset(p, path);
            return p;
        }

        // ---------------------------------------------------------------- scene pieces

        static void BuildLighting()
        {
            var go = new GameObject("Sun (Sunset)");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.62f, 0.36f);
            l.intensity = 1.5f;
            l.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(28f, -55f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.27f, 0.3f);
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, int layer = 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.layer = layer;
            return go;
        }

        static GameObject Group(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static void MakeFlammable(GameObject go, FireMaterialProfile profile, bool igniteOnStart = false)
        {
            SetLayerRecursive(go, layerInteractable);
            var f = go.AddComponent<Flammable>();
            var so = new SerializedObject(f);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.FindProperty("igniteOnStart").boolValue = igniteOnStart;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void MakeProp(GameObject go, float mass)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            SetLayerRecursive(go, layerInteractable);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        static void BuildRoom(Mats m)
        {
            var room = new GameObject("Room").transform;
            var floor = Box("Floor", room, new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f), m.Floor, layerFloor);
            floor.AddComponent<WaterSurface>();
            Box("Wall_N", room, new Vector3(0f, 1.6f, 5.1f), new Vector3(12.4f, 3.2f, 0.2f), m.Wall);
            Box("Wall_S", room, new Vector3(0f, 1.6f, -5.1f), new Vector3(12.4f, 3.2f, 0.2f), m.Wall);
            Box("Wall_W", room, new Vector3(-6.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 10f), m.Wall);
            Box("Wall_E", room, new Vector3(6.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 10f), m.Wall);
            Box("Ceiling", room, new Vector3(0f, 3.3f, 0f), new Vector3(12.4f, 0.2f, 10.4f), m.Wall);
        }

        static Transform BuildHydrant(Mats m)
        {
            var root = Group("Hydrant", null, new Vector3(-5.3f, 0f, -4.3f));
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            body.GetComponent<Renderer>().sharedMaterial = m.Hydrant;

            var anchor = new GameObject("HoseAnchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0.1f, 0.7f, 0.3f);
            return anchor;
        }

        // Zone A: fabric + paper (west)
        static void BuildZoneA(Mats m, Profiles p)
        {
            var zone = new GameObject("Zone_A_Fabric_Paper").transform;

            var sofa = Group("Sofa", zone, new Vector3(-4.9f, 0f, 2.5f), 90f);
            Box("Base", sofa.transform, new Vector3(0f, 0.25f, 0f), new Vector3(2.4f, 0.5f, 1f), m.Fabric);
            Box("Back", sofa.transform, new Vector3(0f, 0.7f, -0.4f), new Vector3(2.4f, 0.8f, 0.2f), m.Fabric);
            Box("ArmL", sofa.transform, new Vector3(-1.1f, 0.45f, 0f), new Vector3(0.2f, 0.6f, 1f), m.Fabric);
            Box("ArmR", sofa.transform, new Vector3(1.1f, 0.45f, 0f), new Vector3(0.2f, 0.6f, 1f), m.Fabric);
            MakeFlammable(sofa, p.Fabric);

            var chair = Group("Armchair", zone, new Vector3(-2.6f, 0f, -1.2f), 30f);
            Box("Base", chair.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.9f), m.Fabric);
            Box("Back", chair.transform, new Vector3(0f, 0.7f, -0.35f), new Vector3(0.9f, 0.8f, 0.2f), m.Fabric);
            MakeFlammable(chair, p.Fabric);

            var curtain = Group("Curtain", zone, new Vector3(-5.9f, 1.7f, 2.5f));
            Box("Cloth", curtain.transform, Vector3.zero, new Vector3(0.1f, 2.3f, 2.4f), m.Fabric);
            MakeFlammable(curtain, p.Fabric, igniteOnStart: true);

            // Loose papers: a fuse from the sofa towards the chair, and good targets for the water push.
            var paperPos = new[] { new Vector3(-3.6f, 0.15f, 2.3f), new Vector3(-2.7f, 0.15f, 1.2f), new Vector3(-2.3f, 0.15f, 0.0f) };
            for (int i = 0; i < paperPos.Length; i++)
            {
                var paper = Group("PaperStack_" + (i + 1), zone, paperPos[i], 15f * i);
                Box("Paper", paper.transform, Vector3.zero, new Vector3(0.5f, 0.3f, 0.35f), m.Paper);
                MakeFlammable(paper, p.Paper);
                MakeProp(paper, 1f);
            }
        }

        // Zone B: wood + metal (east)
        static void BuildZoneB(Mats m, Profiles p)
        {
            var zone = new GameObject("Zone_B_Wood_Metal").transform;

            for (int s = 0; s < 2; s++)
            {
                var shelf = Group("Shelf_" + (s + 1), zone, new Vector3(5.7f, 0f, s == 0 ? -1.8f : 1.8f), -90f);
                Box("SideL", shelf.transform, new Vector3(-0.8f, 0.9f, 0f), new Vector3(0.05f, 1.8f, 0.35f), m.Wood);
                Box("SideR", shelf.transform, new Vector3(0.8f, 0.9f, 0f), new Vector3(0.05f, 1.8f, 0.35f), m.Wood);
                foreach (float y in new[] { 0.1f, 0.65f, 1.2f, 1.75f })
                    Box("Board", shelf.transform, new Vector3(0f, y, 0f), new Vector3(1.65f, 0.05f, 0.35f), m.Wood);
                MakeFlammable(shelf, p.Wood);
            }

            var table = Group("Table", zone, new Vector3(3f, 0f, -0.2f));
            Box("Top", table.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.6f, 0.08f, 0.9f), m.Wood);
            foreach (var c in new[] { new Vector2(-0.7f, -0.38f), new Vector2(0.7f, -0.38f), new Vector2(-0.7f, 0.38f), new Vector2(0.7f, 0.38f) })
                Box("Leg", table.transform, new Vector3(c.x, 0.37f, c.y), new Vector3(0.08f, 0.74f, 0.08f), m.Wood);
            MakeFlammable(table, p.Wood);

            var crate = Group("Crate", zone, new Vector3(3.9f, 0.25f, -3f));
            Box("Box", crate.transform, Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f), m.Wood);
            MakeFlammable(crate, p.Wood);
            MakeProp(crate, 3f);

            var locker = Group("MetalLocker", zone, new Vector3(2.4f, 0f, 4.2f));
            Box("Body", locker.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 1.8f, 0.5f), m.Metal);
            MakeFlammable(locker, p.Metal);

            var barrel = Group("MetalBarrel", zone, new Vector3(4.3f, 0.45f, 3.4f));
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "Body";
            cyl.transform.SetParent(barrel.transform, false);
            cyl.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
            cyl.GetComponent<Renderer>().sharedMaterial = m.Metal;
            MakeFlammable(barrel, p.Metal);
            MakeProp(barrel, 8f);
        }

        static GameObject BuildPlayer(out Transform cam, out Transform hand)
        {
            var player = new GameObject("Player");
            player.layer = layerPlayer;
            player.transform.position = new Vector3(-4.2f, 0.05f, -3f);
            player.transform.rotation = Quaternion.Euler(0f, 40f, 0f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerLook>();
            player.AddComponent<CameraFeedback>();

            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var c = camGo.AddComponent<Camera>();
            c.nearClipPlane = 0.05f;
            c.fieldOfView = 70f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.2f, 0.12f, 0.1f);
            camGo.AddComponent<AudioListener>();
            cam = camGo.transform;

            var handGo = new GameObject("HandPoint");
            handGo.transform.SetParent(camGo.transform, false);
            handGo.transform.localPosition = new Vector3(0.22f, -0.2f, 0.7f);
            hand = handGo.transform;

            SetRef(player.GetComponent<CameraFeedback>(), "cam", cam);
            return player;
        }

        static GameObject BuildHose(Mats m, Transform anchor, Transform hand, out Transform nozzle)
        {
            var go = new GameObject("Hose");
            go.layer = layerHose;
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = m.Hose;
            lr.startWidth = lr.endWidth = 0.09f;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 3;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "NozzleModel";
            model.layer = layerHose;
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.transform.localScale = new Vector3(0.1f, 0.1f, 0.35f);
            model.GetComponent<Renderer>().sharedMaterial = m.Dark;
            nozzle = model.transform;

            var hose = go.AddComponent<HoseController>();
            SetRef(hose, "anchor", anchor);
            SetRef(hose, "handPoint", hand);
            SetMask(hose, "collisionMask", (1 << layerFloor) | (1 << layerInteractable));
            go.AddComponent<HoseRenderer>();
            SetRef(go.GetComponent<HoseRenderer>(), "nozzleModel", nozzle);
            return go;
        }

        static GameObject BuildJet(Mats m)
        {
            var go = new GameObject("Jet");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1f;
            main.startSpeed = 15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.16f);
            main.startColor = new Color(0.6f, 0.82f, 1f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;
            main.maxParticles = 600;
            var em = ps.emission; em.rateOverTime = 0f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 1.5f; shape.radius = 0.02f;
            ConfigureRenderer(ps, m.Water);

            var emitter = go.AddComponent<JetEmitter>();
            SetRef(emitter, "jet", ps);

            var jet = go.AddComponent<JetController>();
            SetMask(jet, "hitMask", worldMask);
            SetRef(emitter, "jet", ps);
            return go;
        }

        sealed class Effects
        {
            public Transform Root, Puddle;
            public ParticleSystem Flame, Steam, Splash;
        }

        static Effects BuildEffects(Mats m)
        {
            var fx = new Effects { Root = new GameObject("Effects").transform };

            fx.Flame = Template("Template_Flame", fx.Root, m.Flame, loop: true, duration: 1f, lifetime: 0.7f,
                speed: new Vector2(0.4f, 0.9f), size: new Vector2(0.25f, 0.5f), gravity: -0.15f, rate: 14f,
                burst: 0, maxParticles: 20, coneAngle: 12f, coneRadius: 0.2f, up: true,
                color: new Color(1f, 0.55f, 0.1f, 0.9f), shrink: true);
            fx.Steam = Template("Template_Steam", fx.Root, m.Steam, loop: false, duration: 0.2f, lifetime: 1.2f,
                speed: new Vector2(0.5f, 1f), size: new Vector2(0.3f, 0.5f), gravity: -0.1f, rate: 0f,
                burst: 8, maxParticles: 12, coneAngle: 25f, coneRadius: 0.1f, up: true,
                color: new Color(0.92f, 0.92f, 0.96f, 0.55f), shrink: false);
            fx.Splash = Template("Template_Splash", fx.Root, m.Splash, loop: false, duration: 0.3f, lifetime: 0.4f,
                speed: new Vector2(1f, 3f), size: new Vector2(0.05f, 0.1f), gravity: 1f, rate: 0f,
                burst: 8, maxParticles: 10, coneAngle: 40f, coneRadius: 0.05f, up: true,
                color: new Color(0.7f, 0.88f, 1f, 0.9f), shrink: true);

            var puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Template_Puddle";
            Object.DestroyImmediate(puddle.GetComponent<Collider>());
            puddle.transform.SetParent(fx.Root, false);
            puddle.transform.localScale = new Vector3(1f, 0.01f, 1f);
            var r = puddle.GetComponent<Renderer>();
            r.sharedMaterial = m.Puddle;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            fx.Puddle = puddle.transform;
            return fx;
        }

        static ParticleSystem Template(string name, Transform parent, Material mat, bool loop, float duration, float lifetime,
            Vector2 speed, Vector2 size, float gravity, float rate, int burst, int maxParticles,
            float coneAngle, float coneRadius, bool up, Color color, bool shrink)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = loop;
            main.playOnAwake = false;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = maxParticles;

            var em = ps.emission;
            em.rateOverTime = rate;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = coneRadius;
            if (up) shape.rotation = new Vector3(-90f, 0f, 0f);

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(fade);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, shrink ? AnimationCurve.Linear(0f, 1f, 1f, 0.1f) : AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));

            ConfigureRenderer(ps, mat);
            return ps;
        }

        static void ConfigureRenderer(ParticleSystem ps, Material mat)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void BuildBootstrap(GameObject player, GameObject hoseObj, GameObject jetObj, Effects fx, Transform nozzle, Transform cam)
        {
            var go = new GameObject("GameBootstrap");
            var input = go.AddComponent<LegacyInput>();
            var boot = go.AddComponent<GameBootstrap>();

            SetRef(boot, "input", input);
            SetRef(boot, "hose", hoseObj.GetComponent<HoseController>());
            SetRef(boot, "hoseRenderer", hoseObj.GetComponent<HoseRenderer>());
            SetRef(boot, "jet", jetObj.GetComponent<JetController>());
            SetRef(boot, "playerLook", player.GetComponent<PlayerLook>());
            SetRef(boot, "playerMotor", player.GetComponent<PlayerMotor>());
            SetRef(boot, "cameraFeedback", player.GetComponent<CameraFeedback>());
            SetRef(boot, "effectsRoot", fx.Root);
            SetRef(boot, "flameTemplate", fx.Flame);
            SetRef(boot, "steamTemplate", fx.Steam);
            SetRef(boot, "splashTemplate", fx.Splash);
            SetRef(boot, "puddleTemplate", fx.Puddle);

            SetRef(jetObj.GetComponent<JetController>(), "hose", hoseObj.GetComponent<HoseController>());
            SetRef(jetObj.GetComponent<JetController>(), "emitter", jetObj.GetComponent<JetEmitter>());
        }

        // ---------------------------------------------------------------- serialized field helpers

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[FlameBlame] {target.GetType().Name} has no field '{field}'"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetMask(Object target, string field, LayerMask mask)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[FlameBlame] {target.GetType().Name} has no field '{field}'"); return; }
            prop.intValue = mask.value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
