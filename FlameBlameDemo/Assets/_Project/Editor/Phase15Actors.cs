using System.Collections.Generic;
using System.IO;
using FlameBlame.Core;
using FlameBlame.Feedback;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Npc;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    public static partial class Phase15Builder
    {
        // ------------------------------------------------------------------ wall fire cabinet inside the window room

        static readonly Vector3 CabinetAnchor = new Vector3(2.2f, 4.30f, -0.12f);

        static void BuildFireCabinet(LevelScenario scenario, List<string> report)
        {
            var cab = Group(phase15, "FireCabinet");
            var red = Mat("Hydrant_Toon");
            var cream = Mat("Visual_Cream_Toon");
            var metal = Mat("Metal_Toon");
            var yellow = Mat("Npc_Stripe_Toon");

            // Red wall cabinet on the east wall of the window room: shell, open door, valve and outlet coupling.
            Box(cab, "Back", new Vector3(2.485f, 4.55f, 0f), new Vector3(0.03f, 1.1f, 0.8f), red);
            Box(cab, "Top", new Vector3(2.39f, 5.08f, 0f), new Vector3(0.2f, 0.04f, 0.8f), red);
            Box(cab, "Bottom", new Vector3(2.39f, 4.02f, 0f), new Vector3(0.2f, 0.04f, 0.8f), red);
            Box(cab, "SideN", new Vector3(2.39f, 4.55f, 0.38f), new Vector3(0.2f, 1.1f, 0.04f), red);
            Box(cab, "SideS", new Vector3(2.39f, 4.55f, -0.38f), new Vector3(0.2f, 1.1f, 0.04f), red);
            Box(cab, "InnerPanel", new Vector3(2.465f, 4.55f, 0f), new Vector3(0.01f, 1.0f, 0.72f), cream, false);
            // door hinged on the north side, swung open ~105 degrees into the room
            Box(cab, "Door", new Vector3(1.943f, 4.55f, 0.476f), new Vector3(0.03f, 1.06f, 0.74f), red, false, Quaternion.Euler(0, -75f, 0));
            Box(cab, "DoorWindow", new Vector3(1.925f, 4.55f, 0.47f), new Vector3(0.035f, 0.6f, 0.4f), cream, false, Quaternion.Euler(0, -75f, 0));
            Cylinder(cab, "ValveStem", new Vector3(2.40f, 4.80f, -0.14f), new Vector3(0, 0, 90), 0.03f, 0.14f, metal);
            Cylinder(cab, "ValveWheel", new Vector3(2.33f, 4.80f, -0.14f), new Vector3(0, 0, 90), 0.11f, 0.03f, yellow);
            Cylinder(cab, "OutletPipe", new Vector3(2.39f, CabinetAnchor.y, CabinetAnchor.z), new Vector3(0, 0, 90), 0.035f, 0.2f, metal);
            Cylinder(cab, "Coupling", new Vector3(2.265f, CabinetAnchor.y, CabinetAnchor.z), new Vector3(0, 0, 90), 0.055f, 0.07f, metal);

            // The Verlet anchor (and its connector) moves from the street hydrant to the coupling; the old hydrant goes away.
            var anchor = scenario.hydrant;
            if (PrefabUtility.IsPartOfPrefabInstance(anchor))   // the street hydrant is a prefab instance; its anchor must become a free scene object
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(anchor.gameObject), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var oldHydrant = anchor.root;
            anchor.SetParent(cab, true);
            anchor.SetPositionAndRotation(CabinetAnchor, Quaternion.Euler(0, 180f, 0));
            if (oldHydrant != anchor && oldHydrant.name == "Hydrant") Object.DestroyImmediate(oldHydrant.gameObject);

            // Hose length: cabinet -> hatch -> farthest attic corner, plus 15% slack.
            var hose = Object.FindFirstObjectByType<HoseController>();
            var data = new SerializedObject(hose);
            float segmentLength = data.FindProperty("segmentLength").floatValue;
            var hatch = new Vector3(HatchX, AtticFloorTop, HatchZ);
            float farthest = 0f;
            foreach (var corner in new[] { new Vector3(-2.5f, AtticFloorTop, HouseSouth), new Vector3(2.5f, AtticFloorTop, HouseSouth), new Vector3(-2.5f, AtticFloorTop, HouseNorth), new Vector3(2.5f, AtticFloorTop, HouseNorth) })
                farthest = Mathf.Max(farthest, Vector3.Distance(hatch, corner));
            float route = Vector3.Distance(CabinetAnchor, hatch) + farthest;
            int segments = Mathf.CeilToInt(route * 1.15f / (segmentLength * 0.92f)) + 1;
            int before = data.FindProperty("segments").intValue;
            data.FindProperty("segments").intValue = segments;
            data.ApplyModifiedPropertiesWithoutUndo();
            report.Add($"Fire cabinet at east wall of the window room (anchor {CabinetAnchor}); hose route {route:F1} m -> {segments} segments (was {before}), reach ~{segments * segmentLength * 0.92f:F1} m.");
        }

        // ------------------------------------------------------------------ no fire outside the house

        static void RemoveOutdoorFire(LevelScenario scenario, List<string> report)
        {
            int removed = 0;
            foreach (var f in Object.FindObjectsByType<Flammable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (f.transform.position.y < 3.0f) { Object.DestroyImmediate(f.gameObject); removed++; }
            var floor1 = Find("Floor1");
            if (floor1 != null) Object.DestroyImmediate(floor1.gameObject);
            // Floor2 zone marker doubles as the marker target: put it in the middle of the living room.
            scenario.floor2.transform.position = new Vector3(0, 4.5f, LivingCenterZ);
            report.Add($"Outdoor fire: removed {removed} outdoor flammable(s) and the Floor1 zone; Floor2 now means the whole second floor.");
        }

        // ------------------------------------------------------------------ attic fire, NPC with watering can, smoke

        static FireMaterialProfile Profile(string name) =>
            AssetDatabase.LoadAssetAtPath<FireMaterialProfile>(Root + "Settings/Fire/Scenario_FireProfile_" + name + ".asset");

        static Flammable BurningBox(Transform parent, string name, Vector3 center, Vector3 size, Material mat, FireMaterialProfile profile)
        {
            var go = Box(parent, name, center, size, mat);
            go.AddComponent<PropertyDamage>();
            var f = go.AddComponent<Flammable>();
            var so = new SerializedObject(f);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.FindProperty("igniteOnStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            return f;
        }

        static void BuildActors(LevelScenario scenario, List<string> report)
        {
            // Weak water must never win: burning objects regain heat faster than a watering can removes it.
            foreach (string n in new[] { "Fabric", "Wood", "Paper" })
            {
                var p = Profile(n);
                p.reheatPerSecond = 7f; p.charSeconds = 18f; p.ignitionChar = 0.35f;
                EditorUtility.SetDirty(p);
            }

            var zoneRoot = scenario.attic.transform;
            zoneRoot.position = new Vector3(0, 7f, 2.9f);
            var fabric = Profile("Fabric"); var wood = Profile("Wood"); var paper = Profile("Paper");
            var pal = LoadPalette();
            var cherry = FlamMaterial("Cherry", pal.flamCherry); var brick = FlamMaterial("Brick", pal.flamBrick);
            var choc = FlamMaterial("Chocolate", pal.flamChocolate); var mustard = FlamMaterial("Mustard", pal.flamMustard);
            var decorations = BurningBox(zoneRoot, "Family Christmas decorations", new Vector3(-0.95f, 7.1f, 1.3f), new Vector3(1.5f, 1.2f, 1.0f), brick, fabric);
            var trunk = BurningBox(zoneRoot, "Old trunk", new Vector3(1.7f, 6.95f, -1.8f), new Vector3(1.3f, 0.9f, 0.7f), choc, wood);
            var books = BurningBox(zoneRoot, "Boxes of old books", new Vector3(-1.7f, 7.0f, -2.0f), new Vector3(1.1f, 1.0f, 0.9f), mustard, paper);
            var carpets = BurningBox(zoneRoot, "Rolled carpets", new Vector3(1.6f, 7.0f, 6.9f), new Vector3(0.8f, 1.0f, 2.0f), cherry, fabric);
            var crates = BurningBox(zoneRoot, "Attic crates", new Vector3(-1.6f, 6.95f, 7.6f), new Vector3(1.2f, 0.9f, 1.0f), choc, wood);
            foreach (var f in new[] { decorations, trunk, books, carpets, crates }) f.gameObject.AddComponent<FireZone>().id = FireZoneId.Attic;
            scenario.attic.members = new[] { decorations, trunk, books, carpets, crates };

            BuildWateringCanNpc(zoneRoot, decorations.transform, report);

            var vent = new GameObject("Smoke vent (living room window)").transform;
            vent.SetParent(phase15, false);
            vent.position = new Vector3(-2.95f, 4.9f, LivingCenterZ);
            scenario.smokeVents = new[] { vent };
            report.Add("Attic fire: 5 flammable groups along the whole attic; smoke vent at the living-room facade window.");
        }

        static void BuildWateringCanNpc(Transform parent, Transform fireTarget, List<string> report)
        {
            var source = Find("Neighbor_B");
            var npcObject = Object.Instantiate(source.gameObject, parent);
            npcObject.name = "Neighbor_Can";
            Vector3 npcPos = new Vector3(0.55f, AtticFloorTop + 0.75f, 2.0f);
            npcObject.transform.SetPositionAndRotation(npcPos, Quaternion.identity);
            var npc = npcObject.GetComponent<NpcActor>();
            var npcData = new SerializedObject(npc);
            npcData.FindProperty("displayName").stringValue = "Сусід із лійкою";
            npcData.FindProperty("panicRadius").floatValue = 0.9f;     // stays at his post unless the fire is on top of him
            npcData.FindProperty("charRadius").floatValue = 0.6f;
            npcData.ApplyModifiedPropertiesWithoutUndo();

            // Can: kinematic rigidbody container so it can fall when he is washed away.
            Vector3 toFire = Vector3.ProjectOnPlane(fireTarget.position - npcPos, Vector3.up).normalized;
            Vector3 hold = npcPos + toFire * 0.5f + Vector3.up * 0.2f;
            var holdPoint = new GameObject("CanHold").transform;
            holdPoint.SetParent(npcObject.transform, true); holdPoint.position = hold;

            var canRoot = new GameObject("WateringCan_NPC");
            canRoot.transform.SetParent(parent, false);
            canRoot.transform.SetPositionAndRotation(hold, Quaternion.LookRotation(toFire));
            var body = canRoot.AddComponent<Rigidbody>();
            body.mass = 0.6f; body.isKinematic = true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Visuals/WateringCan.prefab");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canRoot.transform);
            model.name = "Model";
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var c in model.GetComponentsInChildren<Component>(true))
                if (c is Flammable || c is PropertyDamage || c is FireZone) Object.DestroyImmediate(c);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, -90f, 0);   // the model's spout points along +x; stream goes along +z
            var spoutChild = FindUnder(model.transform, "Spout");

            var streamPoint = new GameObject("StreamPoint").transform;
            streamPoint.SetParent(canRoot.transform, true);
            streamPoint.position = spoutChild != null ? spoutChild.position : canRoot.transform.position + canRoot.transform.forward * 0.25f;
            streamPoint.localRotation = Quaternion.identity;
            var stream = streamPoint.gameObject.AddComponent<ParticleSystem>();
            stream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = stream.main;
            main.loop = true; main.playOnAwake = false; main.startLifetime = 0.8f; main.startSpeed = 2.4f; main.startSize = 0.05f;
            main.gravityModifier = 1.0f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 40;
            main.startColor = new Color(0.75f, 0.9f, 1f, 0.85f);
            var emission = stream.emission; emission.rateOverTime = 0f;
            var shape = stream.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 3f; shape.radius = 0.01f;
            var jetRenderer = Object.FindFirstObjectByType<JetEmitter>().GetComponentInChildren<ParticleSystemRenderer>();
            var streamRenderer = stream.GetComponent<ParticleSystemRenderer>();
            streamRenderer.sharedMaterial = jetRenderer != null ? jetRenderer.sharedMaterial : null;
            streamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GenerateAudio();
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "Audio/FlameBlame.mixer");
            var gurgle = npcObject.AddComponent<AudioSource>();
            gurgle.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Generated/CanGurgle.wav");
            gurgle.loop = true; gurgle.playOnAwake = true; gurgle.volume = 0f; gurgle.spatialBlend = 1f; gurgle.minDistance = 1f; gurgle.maxDistance = 9f;
            gurgle.outputAudioMixerGroup = mixer.FindMatchingGroups("Effects")[0];
            var voice = npcObject.AddComponent<AudioSource>();
            voice.playOnAwake = false; voice.spatialBlend = 1f; voice.minDistance = 1f; voice.maxDistance = 10f;
            voice.outputAudioMixerGroup = mixer.FindMatchingGroups("Voices")[0];

            var tuningPath = Root + "Settings/WateringCanTuning.asset";
            var tuning = AssetDatabase.LoadAssetAtPath<WateringCanTuning>(tuningPath);
            if (tuning == null) { tuning = ScriptableObject.CreateInstance<WateringCanTuning>(); AssetDatabase.CreateAsset(tuning, tuningPath); }

            var behaviour = npcObject.AddComponent<WateringCanNpc>();
            SetRef(behaviour, "npc", npc);
            SetRef(behaviour, "tuning", tuning);
            SetRef(behaviour, "can", canRoot.transform);
            SetRef(behaviour, "spout", streamPoint);
            SetRef(behaviour, "fireTarget", fireTarget);
            SetRef(behaviour, "holdPoint", holdPoint);
            SetRef(behaviour, "stream", stream);
            SetRef(behaviour, "canBody", body);
            SetRef(behaviour, "gurgle", gurgle);
            SetRef(behaviour, "voice", voice);
            var so = new SerializedObject(behaviour);
            var clips = so.FindProperty("mumbles");
            clips.arraySize = 3;
            for (int i = 0; i < 3; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Generated/CanMumble" + (i + 1) + ".wav");
            // keep the can and its stream from hitting the player layer / hose layer
            so.FindProperty("hitMask").FindPropertyRelative("m_Bits").intValue = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Hose")));
            so.ApplyModifiedPropertiesWithoutUndo();
            report.Add($"Watering-can NPC at {npcPos} behind the pillar; stream {tuning.waterPerSecond:F3} water/s (hose ~1.0), burning objects regain 7 heat/s.");
        }

        // ------------------------------------------------------------------ procedural audio (CC0, generated here)

        static void GenerateAudio()
        {
            Directory.CreateDirectory(Root + "Audio/Generated");
            const int rate = 22050;
            var rng = new System.Random(15);
            // gurgle: random short chirps over a faint water bed, looped-friendly (starts and ends quiet)
            var gurgle = new float[(int)(rate * 1.8f)];
            for (int b = 0; b < 26; b++)
            {
                int start = rng.Next(0, gurgle.Length - rate / 6);
                float f0 = 260f + (float)rng.NextDouble() * 380f, len = rate * (0.05f + (float)rng.NextDouble() * 0.09f), amp = 0.25f + (float)rng.NextDouble() * 0.3f;
                for (int i = 0; i < len; i++)
                {
                    float t = i / len, env = Mathf.Sin(t * Mathf.PI);
                    gurgle[start + i] += Mathf.Sin(2f * Mathf.PI * (f0 * (1f + t * 0.9f)) * i / rate) * env * amp;
                }
            }
            for (int i = 0; i < gurgle.Length; i++) gurgle[i] += ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
            WriteWav(Root + "Audio/Generated/CanGurgle.wav", Normalize(gurgle, 0.55f), rate);

            // mumbles: voiced saw babble through moving formants, 3 variants
            for (int v = 0; v < 3; v++)
            {
                var clip = new float[(int)(rate * (0.7f + v * 0.15f))];
                float pitch = 105f + v * 22f; float phase = 0f;
                int syllables = 3 + v;
                for (int i = 0; i < clip.Length; i++)
                {
                    float t = (float)i / clip.Length;
                    float syl = Mathf.Abs(Mathf.Sin(t * Mathf.PI * syllables));
                    float f = pitch * (1f + 0.18f * Mathf.Sin(t * 9f + v));
                    phase += f / rate; phase -= Mathf.Floor(phase);
                    float saw = phase * 2f - 1f;
                    float formant1 = Mathf.Sin(2f * Mathf.PI * (420f + 220f * Mathf.Sin(t * 14f + v)) * i / rate);
                    float formant2 = Mathf.Sin(2f * Mathf.PI * (1250f + 400f * Mathf.Sin(t * 11f)) * i / rate);
                    clip[i] = saw * (0.55f * formant1 + 0.3f * formant2 + 0.25f) * syl * Mathf.Sin(t * Mathf.PI);
                }
                WriteWav(Root + "Audio/Generated/CanMumble" + (v + 1) + ".wav", Normalize(clip, 0.6f), rate);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static float[] Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            foreach (float s in data) max = Mathf.Max(max, Mathf.Abs(s));
            for (int i = 0; i < data.Length; i++) data[i] = data[i] / max * peak;
            return data;
        }

        static void WriteWav(string path, float[] samples, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (float s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }

        // ------------------------------------------------------------------ colours and materials

        static EnvironmentPalette LoadPalette()
        {
            var pal = AssetDatabase.LoadAssetAtPath<EnvironmentPalette>(Root + "Settings/EnvironmentPalette.asset");
            EditorUtility.SetDirty(pal);   // persists the new Flammables group defaults
            return pal;
        }

        static void Tint(Material m, Color c, Material template)
        {
            float baseMax = Mathf.Max(template.GetColor("_BaseColor").maxColorComponent, 0.0001f);
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
            foreach (string prop in new[] { "_1st_ShadeColor", "_2nd_ShadeColor" })
            {
                var shade = c * (template.GetColor(prop).maxColorComponent / baseMax); shade.a = c.a;
                m.SetColor(prop, shade);
            }
            EditorUtility.SetDirty(m);
        }

        static Material FlamMaterial(string name, Color color)
        {
            string path = ToonDir + "Flam_" + name + "_Toon.mat";
            var template = Mat("Neighbor_B_Toon");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(template) { name = "Flam_" + name + "_Toon" }; AssetDatabase.CreateAsset(m, path); }
            Tint(m, color, template);
            return m;
        }

        static void DarkenFlammables(List<string> report)
        {
            var pal = LoadPalette();
            var materials = new Dictionary<string, Material>
            {
                { "Cherry", FlamMaterial("Cherry", pal.flamCherry) }, { "Brick", FlamMaterial("Brick", pal.flamBrick) },
                { "Chocolate", FlamMaterial("Chocolate", pal.flamChocolate) }, { "Mustard", FlamMaterial("Mustard", pal.flamMustard) },
                { "Olive", FlamMaterial("Olive", pal.flamOlive) },
            };
            int renderers = 0, objects = 0;
            var log = new List<string>();
            foreach (var flammable in Object.FindObjectsByType<Flammable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var profile = (FireMaterialProfile)new SerializedObject(flammable).FindProperty("profile").objectReferenceValue;
                if (profile == null || !profile.canBurn) continue;      // metal never ignites: it is a wall-like prop
                objects++;
                foreach (var r in flammable.GetComponentsInChildren<Renderer>(true))
                {
                    var shared = r.sharedMaterials; bool changed = false;
                    for (int i = 0; i < shared.Length; i++)
                    {
                        if (shared[i] == null) continue;
                        string key = FamilyOf(shared[i].name, flammable.name);
                        if (key == null) continue;
                        if (log.Count < 12) log.Add($"{flammable.name}/{r.name}: {shared[i].name} V{V(shared[i].GetColor("_BaseColor")):F2} -> Flam_{key} V{V(materials[key].GetColor("_BaseColor")):F2}");
                        shared[i] = materials[key]; changed = true;
                    }
                    if (changed) { r.sharedMaterials = shared; renderers++; }
                }
            }
            report.Add($"Flammables darkened: {objects} objects, {renderers} renderers. Samples:\n  " + string.Join("\n  ", log));
        }

        static float V(Color c) { Color.RGBToHSV(c, out _, out _, out float v); return v; }

        static string FamilyOf(string materialName, string owner)
        {
            switch (materialName)
            {
                case "Visual_Teal_Toon": case "Fabric_Toon": return owner == "Armchair" ? "Brick" : "Cherry";
                case "Visual_Coral_Toon": return "Brick";
                case "Visual_Walnut_Toon": case "Wood_Toon": return "Chocolate";
                case "Paper_Toon": case "Visual_Cream_Toon": return "Mustard";
                case "Visual_Leaf_Toon": return "Olive";
                case "Visual_Petal_Toon": return "Cherry";
                default: return null;
            }
        }

        // ------------------------------------------------------------------ matte floors

        static void MakeMatte(Material m)
        {
            void F(string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
            F("_Smoothness", 0); F("_Metallic", 0); F("_SpecularHighlights", 0); F("_EnvironmentReflections", 0);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if (m.HasProperty("_HighColor")) m.SetColor("_HighColor", Color.black);
            F("_Is_SpecularToHighColor", 0); F("_MatCap", 0); F("_RimLight", 0);
            EditorUtility.SetDirty(m);
        }

        static void MakeFloorsMatte(List<string> report)
        {
            foreach (string name in new[] { "Floor_Toon", "Attic_Floor_Toon" }) MakeMatte(Mat(name));
            report.Add("Matte floors: Floor_Toon and Attic_Floor_Toon -> smoothness 0, specular highlights OFF, environment reflections OFF, HighColor black.");
        }

        // ------------------------------------------------------------------ showcase wiring

        static void WireShowcase(List<string> report)
        {
            var asset = ShowcaseDefaults.LoadOrCreate();
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var holder = new GameObject("PhotoMode").transform;
            holder.SetParent(bootstrap.transform, false);
            var photo = holder.gameObject.AddComponent<PhotoMode>();
            var look = Object.FindFirstObjectByType<FlameBlame.Player.PlayerLook>();
            SetRef(photo, "shots", asset);
            SetRef(photo, "gameCamera", look.GetComponentInChildren<Camera>());
            SetRef(photo, "hudRoot", Find("HUD"));
            SetRef(bootstrap, "photo", photo);
            report.Add("Photo mode (F12) and HUD toggle (F11) wired; ShowcaseShots asset with " + asset.shots.Count + " bookmarks.");
        }

        static void MarkStatic()
        {
            int count = 0;
            foreach (var root in new[] { phase15, Find("FlightExterior"), Find("Room") })
            {
                if (root == null) continue;
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var go = r.gameObject;
                    if (go.GetComponentInParent<Rigidbody>() != null || go.GetComponentInParent<Flammable>() != null
                        || go.GetComponentInParent<NpcActor>() != null) continue;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    count++;
                }
            }
            Debug.Log("[FlameBlame] Static batching flags set on " + count + " environment renderers.");
        }

    }
}
