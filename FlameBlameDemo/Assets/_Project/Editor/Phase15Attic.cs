using System.IO;
using System.Collections.Generic;
using FlameBlame.Core;
using UnityEditor;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    public static partial class Phase15Builder
    {
        // ------------------------------------------------------------------ attic: hatch, broken ladder, pillar, roof

        static void BuildAttic(LevelScenario scenario, List<string> report)
        {
            // Remove the old gable-window attic shell; the zone object and goals are kept and re-pointed below.
            foreach (string old in new[] { "Attic floor", "Attic back", "Attic side", "Gable opening side", "Roof", "Attic goal", "Attic entry", "Family Christmas decorations" })
                for (var t = Find(old); t != null; t = Find(old)) Object.DestroyImmediate(t.gameObject);

            atticRoot = Group(phase15, "Attic");
            var sage = Mat("Visual_Sage_Toon");
            var roofMat = Mat("Visual_Coral_Toon");
            var wood = Mat("Visual_Walnut_Toon");
            var atticFloor = CreateAtticFloorMaterial();

            // Floor of the attic (matte), with the same hatch opening as the ceiling below.
            BuildSlabs(atticRoot, "AtticFloor", 6.45f, 0.1f, atticFloor, HouseSouth);

            // Side walls and gable ends.
            float length = HouseNorth - HouseSouth, zc = (HouseNorth + HouseSouth) * 0.5f;
            Box(atticRoot, "Wall_W", new Vector3(-2.6f, 7.2f, zc), new Vector3(0.2f, 1.6f, length), sage);
            Box(atticRoot, "Wall_E", new Vector3(2.6f, 7.2f, zc), new Vector3(0.2f, 1.6f, length), sage);
            var gableMesh = BuildGableMesh();
            foreach (float z in new[] { HouseSouth + 0.1f, HouseNorth - 0.1f })
            {
                var gable = new GameObject("Gable");
                gable.transform.SetParent(atticRoot, false);
                gable.transform.position = new Vector3(0, 0, z);
                gable.AddComponent<MeshFilter>().sharedMesh = gableMesh;
                gable.AddComponent<MeshRenderer>().sharedMaterial = sage;
                var col = gable.AddComponent<BoxCollider>();
                col.center = new Vector3(0, 7.2f, 0); col.size = new Vector3(5.4f, 1.6f, 0.2f);
            }

            // Gable roof: ridge along the long axis.
            foreach (int sign in new[] { -1, 1 })
            {
                float rise = EaveHalfSpan * Mathf.Tan(RoofAngle * Mathf.Deg2Rad);
                float slope = Mathf.Sqrt(EaveHalfSpan * EaveHalfSpan + rise * rise);
                Box(atticRoot, "Roof", new Vector3(sign * EaveHalfSpan * 0.5f, RidgeY - rise * 0.5f, zc),
                    new Vector3(slope + 0.1f, 0.18f, length + 0.4f), roofMat, true, Quaternion.Euler(0, 0, -sign * RoofAngle));
            }

            // Plain liner under the coral roof so the attic interior is not a red box.
            foreach (int sign in new[] { -1, 1 })
            {
                float rise2 = EaveHalfSpan * Mathf.Tan(RoofAngle * Mathf.Deg2Rad);
                float slope2 = Mathf.Sqrt(EaveHalfSpan * EaveHalfSpan + rise2 * rise2);
                var normal = Quaternion.Euler(0, 0, -sign * RoofAngle) * Vector3.down;
                Box(atticRoot, "RoofLiner", new Vector3(sign * EaveHalfSpan * 0.5f, RidgeY - rise2 * 0.5f, zc) + normal * 0.115f,
                    new Vector3(slope2 + 0.1f, 0.04f, length + 0.4f), sage, false, Quaternion.Euler(0, 0, -sign * RoofAngle));
            }

            // Central pillar with a small cross beam, matte warm wood, not flammable.
            float pillarZ = zc;
            var pillar = Group(atticRoot, "Pillar");
            var post = Box(pillar, "Post", new Vector3(0, 7.9f, pillarZ), new Vector3(0.3f, 2.8f, 0.3f), wood);
            Box(pillar, "CrossBeam", new Vector3(0, 8.95f, pillarZ), new Vector3(1.2f, 0.12f, 0.14f), wood, false);
            Box(pillar, "Foot", new Vector3(0, 6.56f, pillarZ), new Vector3(0.5f, 0.12f, 0.5f), wood, false);

            BuildHatch(atticRoot);
            BuildBrokenLadder(atticRoot);

            // Interior light for the attic (no shadows).
            foreach (float z in new[] { 0.2f, 6.2f })
            {
                var lamp = new GameObject("Attic Light").transform;
                lamp.SetParent(atticRoot, false);
                lamp.position = new Vector3(0, 8.3f, z);
                var light = lamp.gameObject.AddComponent<Light>();
                light.type = LightType.Point; light.color = new Color(1f, 0.78f, 0.55f); light.intensity = 0.9f; light.range = 8f; light.shadows = LightShadows.None;
            }

            // Goal marker and trigger sit over the hatch, at attic floor level.
            var goal = new GameObject("Attic goal").transform;
            goal.SetParent(phase15, false);
            goal.position = new Vector3(HatchX, 7.0f, HatchZ);
            scenario.atticTarget = goal;
            var entry = new GameObject("Attic entry");
            entry.transform.SetParent(phase15, false);
            entry.transform.position = new Vector3(HatchX, 7.2f, HatchZ);
            var trigger = entry.AddComponent<BoxCollider>();
            trigger.isTrigger = true; trigger.size = new Vector3(HatchW + 0.6f, 1.4f, HatchL + 0.6f);
            scenario.atticVolume = trigger;
            report.Add($"Attic: hatch {HatchW:F1} x {HatchL:F1} m at ({HatchX}, {HatchZ}); pillar at z={pillarZ:F1}; roof ridge y={RidgeY}; attic runs z {HouseSouth}..{HouseNorth}.");
        }

        static void BuildHatch(Transform parent)
        {
            var hatch = Group(parent, "Hatch");
            var trim = Mat("Visual_Walnut_Toon");
            float x0 = HatchX - HatchW * 0.5f, x1 = HatchX + HatchW * 0.5f, z0 = HatchZ - HatchL * 0.5f, z1 = HatchZ + HatchL * 0.5f;
            const float y = 6.55f;
            // Frame beams around the opening (visual only; collision comes from the rounded rim below).
            Box(hatch, "FrameS", new Vector3(HatchX, y, z0 - 0.06f), new Vector3(HatchW + 0.24f, 0.1f, 0.12f), trim, false);
            Box(hatch, "FrameN", new Vector3(HatchX, y, z1 + 0.06f), new Vector3(HatchW + 0.24f, 0.1f, 0.12f), trim, false);
            Box(hatch, "FrameW", new Vector3(x0 - 0.06f, y, HatchZ), new Vector3(0.12f, 0.1f, HatchL), trim, false);
            Box(hatch, "FrameE", new Vector3(x1 + 0.06f, y, HatchZ), new Vector3(0.12f, 0.1f, HatchL), trim, false);

            // Rounded rim colliders: capsules lying along the four edges so the player and the hose slide instead of snagging.
            Capsule(hatch, "RimS", new Vector3(HatchX, 6.45f, z0), 0, HatchW + 0.1f, 0.08f);
            Capsule(hatch, "RimN", new Vector3(HatchX, 6.45f, z1), 0, HatchW + 0.1f, 0.08f);
            Capsule(hatch, "RimW", new Vector3(x0, 6.45f, HatchZ), 2, HatchL + 0.1f, 0.08f);
            Capsule(hatch, "RimE", new Vector3(x1, 6.45f, HatchZ), 2, HatchL + 0.1f, 0.08f);

            // Lid thrown open against the north side (visual only).
            Box(hatch, "Lid", new Vector3(HatchX, 7.19f, z1 + 0.16f), new Vector3(HatchW, 0.06f, 1.3f), trim, false, Quaternion.Euler(-80f, 0, 0));
        }

        static void Capsule(Transform parent, string name, Vector3 center, int direction, float length, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var c = go.AddComponent<CapsuleCollider>();
            c.direction = direction; c.radius = radius; c.height = length + radius * 2f;
        }

        static void BuildBrokenLadder(Transform parent)
        {
            var ladder = Group(parent, "BrokenAtticLadder");
            var wood = Mat("Visual_Walnut_Toon");
            float x0 = HatchX - HatchW * 0.5f, z1 = HatchZ + HatchL * 0.5f;

            // Upper part still hangs from the north rim of the hatch, swinging a little; two rungs are missing, one rail is snapped.
            var hang = Group(ladder, "Hanging");
            hang.position = new Vector3(HatchX, 6.55f, z1 - 0.12f);
            hang.rotation = Quaternion.Euler(9f, 0, 4f);
            Box(hang, "RailL", hang.TransformPoint(new Vector3(-0.22f, -0.65f, 0)), new Vector3(0.07f, 1.3f, 0.06f), wood, false, hang.rotation);
            Box(hang, "RailR", hang.TransformPoint(new Vector3(0.22f, -0.38f, 0)), new Vector3(0.07f, 0.76f, 0.06f), wood, false, hang.rotation);
            foreach (float y in new[] { -0.12f, -0.72f })
                Box(hang, "Rung", hang.TransformPoint(new Vector3(0, y, 0)), new Vector3(0.5f, 0.05f, 0.05f), wood, false, hang.rotation);
            // a splintered stub where the right rail broke
            Box(hang, "Splinter", hang.TransformPoint(new Vector3(0.22f, -0.8f, 0.02f)), new Vector3(0.05f, 0.14f, 0.05f), wood, false, hang.rotation * Quaternion.Euler(0, 0, 25f));

            // Lower part: debris on the living-room floor under the hatch (static, no colliders).
            var debris = Group(ladder, "Debris");
            float floorY = FloorTop + 0.03f;
            Box(debris, "RailPieceA", new Vector3(-0.5f, floorY, 5.2f), new Vector3(0.07f, 0.06f, 1.4f), wood, false, Quaternion.Euler(0, 14f, 0));
            Box(debris, "RailPieceB", new Vector3(0.1f, floorY, 5.7f), new Vector3(0.07f, 0.06f, 0.8f), wood, false, Quaternion.Euler(0, -32f, 0));
            Box(debris, "RungA", new Vector3(-0.2f, floorY, 5.0f), new Vector3(0.5f, 0.05f, 0.05f), wood, false, Quaternion.Euler(0, 68f, 0));
            Box(debris, "RungB", new Vector3(0.35f, floorY + 0.03f, 5.35f), new Vector3(0.5f, 0.05f, 0.05f), wood, false, Quaternion.Euler(8f, 20f, 12f));
            Box(debris, "Splinter", new Vector3(-0.85f, floorY, 5.6f), new Vector3(0.05f, 0.04f, 0.3f), wood, false, Quaternion.Euler(0, 55f, 0));
            Box(debris, "Plank", new Vector3(0.7f, floorY, 4.9f), new Vector3(0.16f, 0.03f, 0.55f), wood, false, Quaternion.Euler(0, -10f, 0));
        }

        /// Pentagon (gable end) extruded 0.2 m. Saved as an asset so the scene keeps a stable reference.
        static Mesh BuildGableMesh()
        {
            const string path = Root + "Art/Meshes/AtticGable.asset";
            Directory.CreateDirectory(Root + "Art/Meshes");
            var shape = new[]
            {
                new Vector2(-2.7f, 6.4f), new Vector2(2.7f, 6.4f), new Vector2(2.7f, AtticWallTop),
                new Vector2(0f, RidgeY - 0.05f), new Vector2(-2.7f, AtticWallTop)
            };
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            float h = 0.1f;
            for (int face = 0; face < 2; face++)
            {
                float z = face == 0 ? -h : h;
                int baseIndex = vertices.Count;
                foreach (var p in shape) { vertices.Add(new Vector3(p.x, p.y, z)); uv.Add(p * 0.2f); }
                for (int i = 1; i < shape.Length - 1; i++)
                {
                    if (face == 0) { triangles.Add(baseIndex); triangles.Add(baseIndex + i + 1); triangles.Add(baseIndex + i); }
                    else { triangles.Add(baseIndex); triangles.Add(baseIndex + i); triangles.Add(baseIndex + i + 1); }
                }
            }
            for (int i = 0; i < shape.Length; i++)
            {
                var a = shape[i]; var b = shape[(i + 1) % shape.Length];
                int baseIndex = vertices.Count;
                vertices.Add(new Vector3(a.x, a.y, -h)); vertices.Add(new Vector3(b.x, b.y, -h));
                vertices.Add(new Vector3(b.x, b.y, h)); vertices.Add(new Vector3(a.x, a.y, h));
                for (int k = 0; k < 4; k++) uv.Add(Vector2.zero);
                triangles.AddRange(new[] { baseIndex, baseIndex + 2, baseIndex + 1, baseIndex, baseIndex + 3, baseIndex + 2 });
            }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = "AtticGable" }; AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Material CreateAtticFloorMaterial()
        {
            string path = ToonDir + "Attic_Floor_Toon.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Mat("Floor_Toon")) { name = "Attic_Floor_Toon" }; AssetDatabase.CreateAsset(m, path); }
            var warmWood = new Color(.50f, .35f, .24f);
            Tint(m, warmWood, Mat("Floor_Toon"));
            MakeMatte(m);
            return m;
        }
    }
}
