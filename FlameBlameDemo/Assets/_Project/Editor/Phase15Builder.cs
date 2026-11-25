using System.Collections.Generic;
using System.IO;
using FlameBlame.Core;
using FlameBlame.Fire;
using FlameBlame.Hose;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    /// Phase 15: point fixes to the existing Main scene (indoor fire cabinet, twice-longer house, attic hatch,
    /// pillar, watering-can NPC, darker flammables, matte floors). Works in place and refuses to run twice.
    /// Tools/FlameBlame/Apply Phase 15 Level Fixes
    public static partial class Phase15Builder
    {
        const string Root = "Assets/_Project/";
        const string ScenePath = Root + "Scenes/Main.unity";
        const string ToonDir = Root + "Materials/Toon/";

        // ---- house geometry (world metres) ----
        const float FloorTop = 3.2f, CeilingTop = 6.4f, AtticFloorTop = 6.5f;
        const float HouseSouth = -3.0f, HouseNorth = 8.8f;
        const float PartitionZ = 2.9f, LivingCenterZ = 5.8f, LivingShift = 5.8f;
        // attic hatch (hole in the ceiling / attic floor)
        const float HatchX = -0.2f, HatchZ = 4.0f, HatchW = 1.2f, HatchL = 1.4f;
        // roof
        const float RoofAngle = 28f, RidgeY = 9.5f, EaveHalfSpan = 2.8f, AtticWallTop = 8.0f;

        static Transform phase15;
        static Transform atticRoot;

        [MenuItem("Tools/FlameBlame/Apply Phase 15 Level Fixes")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Find("Phase15") != null) throw new System.InvalidOperationException("Phase 15 is already applied to this scene.");

            phase15 = new GameObject("Phase15").transform;
            var scenario = Object.FindFirstObjectByType<LevelScenario>();

            var report = new List<string>();
            ExtendHouse(report);
            BuildAttic(scenario, report);
            BuildFireCabinet(scenario, report);
            RemoveOutdoorFire(scenario, report);
            BuildActors(scenario, report);      // NPC with watering can, attic fire, smoke vents (Phase15Actors.cs)
            DarkenFlammables(report);
            MakeFloorsMatte(report);
            WireShowcase(report);
            MarkStatic();

            EditorUtility.SetDirty(scenario);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phase15_report.txt", string.Join("\n", report));
            Debug.Log("[FlameBlame] Phase 15 applied:\n" + string.Join("\n", report));
        }

        // ------------------------------------------------------------------ small helpers

        static Transform Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        static Material Mat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(ToonDir + name + ".mat");
            if (m == null) m = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/" + name + ".mat");
            if (m == null) throw new System.InvalidOperationException("Material missing: " + name);
            return m;
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, rotation ?? Quaternion.identity);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 center, Vector3 axisEuler, float radius, float length, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(axisEuler));
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new System.InvalidOperationException(target.GetType().Name + " has no field " + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ house: twice as long

        static void ExtendHouse(List<string> report)
        {
            var house = Group(phase15, "House");
            var sage = Mat("Visual_Sage_Toon");
            var ceilingMat = Mat("Env_Ceiling_Toon");
            var floorMat = Mat("Floor_Toon");

            // Ground floor volume and pavement follow the longer facade.
            var ground = Find("GroundFloor");
            float groundLength = HouseNorth - (-2.9f);
            ground.localScale = new Vector3(ground.localScale.x, ground.localScale.y, groundLength);
            ground.position = new Vector3(ground.position.x, ground.position.y, (HouseNorth + -2.9f) * 0.5f);
            var sidewalk = Find("Sidewalk");
            sidewalk.localScale = new Vector3(sidewalk.localScale.x, sidewalk.localScale.y, 22f);
            sidewalk.position = new Vector3(sidewalk.position.x, sidewalk.position.y, ground.position.z);

            // The old room keeps its window and becomes the cabinet room; its contents move into the new living room.
            foreach (string zone in new[] { "Zone_A_Fabric_Paper", "Zone_B_Wood_Metal", "Phase2" })
                Find(zone).position += Vector3.forward * LivingShift;
            var curtain = Find("Curtain");
            curtain.position += Vector3.back * 1.4f;                       // behind the middle facade window
            var locker = Find("MetalLocker"); locker.position = new Vector3(-1.9f, locker.position.y, -2.15f);
            var barrel = Find("MetalBarrel"); barrel.position = new Vector3(1.8f, barrel.position.y, -1.5f);

            // Living room shell.
            var living = Group(house, "LivingRoom");
            var floor = Box(living, "Floor", new Vector3(0, 3.1f, LivingCenterZ), new Vector3(5f, 0.2f, 5.6f), floorMat);
            floor.AddComponent<FlameBlame.Water.WaterSurface>();
            Box(living, "Wall_W", new Vector3(-2.6f, 4.7f, LivingCenterZ), new Vector3(0.2f, 3f, 5.6f), sage);
            Box(living, "Wall_E", new Vector3(2.6f, 4.7f, LivingCenterZ), new Vector3(0.2f, 3f, 5.6f), sage);
            Box(living, "Wall_N", new Vector3(0, 4.7f, 8.7f), new Vector3(5.4f, 3f, 0.2f), sage);
            var wainscot = Mat("Env_Wainscot_Toon");
            Box(living, "WainscotN", new Vector3(0, 3.7f, 8.58f), new Vector3(5f, 1f, 0.04f), wainscot, false);
            Box(living, "WainscotE", new Vector3(2.48f, 3.7f, LivingCenterZ), new Vector3(0.04f, 1f, 5.6f), wainscot, false);

            // Ceiling of the living room: four slabs around the hatch opening.
            BuildSlabs(house, "Ceiling", 6.3f, 0.2f, ceilingMat, 3.0f);

            // Doorway (no door) in the partition wall: replace the solid wall by two pieces and a lintel.
            Transform wallN = FindUnder(Find("Room"), "Wall_N");
            const float doorX = 0.6f, doorW = 1.3f;
            float left = doorX - doorW * 0.5f, right = doorX + doorW * 0.5f;
            wallN.localScale = new Vector3(left - (-2.7f), 3f, 0.2f);
            wallN.position = new Vector3((left + -2.7f) * 0.5f, 4.7f, PartitionZ);
            Box(house, "Wall_N_Right", new Vector3((right + 2.7f) * 0.5f, 4.7f, PartitionZ), new Vector3(2.7f - right, 3f, 0.2f), sage);
            Box(house, "Wall_N_Lintel", new Vector3(doorX, 5.8f, PartitionZ), new Vector3(doorW, 0.8f, 0.2f), sage);
            var decor = FindUnder(Find("Room"), "Decor");
            var door = FindUnder(decor, "Door");
            if (door != null) Object.DestroyImmediate(door.gameObject);
            var wain = FindUnder(decor, "WainscotN");
            wain.localScale = new Vector3(left - (-2.5f), 1f, 0.04f);
            wain.position = new Vector3((left + -2.5f) * 0.5f, 3.7f, 2.78f);
            Box(house, "WainscotN_Right", new Vector3((right + 2.5f) * 0.5f, 3.7f, 2.78f), new Vector3(2.5f - right, 1f, 0.04f), wainscot, false);
            Box(house, "WainscotN_Living_L", new Vector3((left + -2.5f) * 0.5f, 3.7f, 3.02f), new Vector3(left + 2.5f, 1f, 0.04f), wainscot, false);
            Box(house, "WainscotN_Living_R", new Vector3((right + 2.5f) * 0.5f, 3.7f, 3.02f), new Vector3(2.5f - right, 1f, 0.04f), wainscot, false);
            var frame = Mat("Env_DoorFrame_Toon");
            Box(house, "DoorwayJambL", new Vector3(left - 0.03f, 4.3f, PartitionZ), new Vector3(0.06f, 2.2f, 0.26f), frame, false);
            Box(house, "DoorwayJambR", new Vector3(right + 0.03f, 4.3f, PartitionZ), new Vector3(0.06f, 2.2f, 0.26f), frame, false);
            Box(house, "DoorwayHeader", new Vector3(doorX, 5.43f, PartitionZ), new Vector3(doorW + 0.12f, 0.06f, 0.26f), frame, false);

            // Facade rhythm: evenly spaced windows, some dark, some boarded (decoration only; the real window stays at z=0).
            BuildFacade(house);

            // Second warm bounce light for the living room (no shadows).
            var warm = Find("Warm Interior Bounce");
            if (warm != null)
            {
                var copy = Object.Instantiate(warm.gameObject, warm.parent);
                copy.name = "Warm Interior Bounce (Living)";
                copy.transform.position = new Vector3(-1.5f, 5.8f, LivingCenterZ);
            }
            report.Add($"House: ground volume {groundLength:F1} m long (was 5.8); living room z 3.0..8.6 added; contents moved +{LivingShift} m; doorway {doorW} m wide.");
        }

        static Transform FindUnder(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name && t != root) return t;
            return null;
        }

        /// Four boxes around the hatch for a slab layer between z0 and the house end (or the full attic floor from the south wall).
        static void BuildSlabs(Transform parent, string name, float centerY, float thickness, Material mat, float z0)
        {
            var layer = Group(parent, name);
            float hx0 = HatchX - HatchW * 0.5f, hx1 = HatchX + HatchW * 0.5f, hz0 = HatchZ - HatchL * 0.5f, hz1 = HatchZ + HatchL * 0.5f;
            const float x0 = -2.7f, x1 = 2.7f;
            // south of the hole, north of the hole, and the two side strips
            Slab(layer, "South", x0, x1, z0, hz0, centerY, thickness, mat);
            Slab(layer, "North", x0, x1, hz1, HouseNorth, centerY, thickness, mat);
            Slab(layer, "West", x0, hx0, hz0, hz1, centerY, thickness, mat);
            Slab(layer, "East", hx1, x1, hz0, hz1, centerY, thickness, mat);
        }

        static void Slab(Transform parent, string name, float xa, float xb, float za, float zb, float y, float thickness, Material mat)
        {
            Box(parent, name, new Vector3((xa + xb) * 0.5f, y, (za + zb) * 0.5f), new Vector3(xb - xa, thickness, zb - za), mat);
        }

        static void BuildFacade(Transform house)
        {
            var facade = Group(house, "Facade");
            var glass = Mat("FlightDarkGlass");
            var trim = Mat("FlightTrim");
            var boards = Mat("FlightPavement");
            const float faceX = -2.74f;
            // second floor: spacing 2.9 m, centred on the real window at z=0
            foreach (float z in new[] { 2.9f, 5.8f }) FacadeWindow(facade, glass, trim, boards, faceX, 4.9f, z, closed: z > 5f);
            // ground floor: existing blocked door (z=-1.6) and closed window (z=1.3), then the same pitch
            foreach (float z in new[] { 4.2f, 7.1f }) FacadeWindow(facade, glass, trim, boards, faceX, 1.7f, z, closed: z > 5f);
        }

        static void FacadeWindow(Transform parent, Material glass, Material trim, Material boards, float x, float y, float z, bool closed)
        {
            var w = Group(parent, closed ? "WindowBoarded" : "WindowDark");
            Box(w, "Pane", new Vector3(x, y, z), new Vector3(0.08f, 1.5f, 1.9f), closed ? boards : glass, false);
            Box(w, "SillBoard", new Vector3(x - 0.04f, y - 0.82f, z), new Vector3(0.14f, 0.1f, 2.2f), trim, false);
            Box(w, "Header", new Vector3(x - 0.02f, y + 0.8f, z), new Vector3(0.1f, 0.1f, 2.1f), trim, false);
            Box(w, "JambL", new Vector3(x - 0.02f, y, z - 1.0f), new Vector3(0.1f, 1.6f, 0.1f), trim, false);
            Box(w, "JambR", new Vector3(x - 0.02f, y, z + 1.0f), new Vector3(0.1f, 1.6f, 0.1f), trim, false);
            if (closed)
                for (int i = -2; i <= 2; i++) Box(w, "Plank", new Vector3(x - 0.06f, y + i * 0.3f, z), new Vector3(0.05f, 0.16f, 2.0f), trim, false);
            else
                Box(w, "Mullion", new Vector3(x - 0.04f, y, z), new Vector3(0.06f, 1.5f, 0.07f), trim, false);
        }
    }
}
