using FlameBlame.Hose;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FlameBlame.Tests
{
    public class HoseCollisionTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void MainScene_HoseSettlesOnFloor(bool held)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                HoseController controller = null;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<HoseController>() is HoseController found) controller = found;
                Assert.That(controller, Is.Not.Null);
                var serialized = new SerializedObject(controller);
                int mask = serialized.FindProperty("collisionMask").intValue;
                Assert.That(mask, Is.EqualTo(LayerMask.GetMask("Default", "Floor", "Interactable")));
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var collider in root.GetComponentsInChildren<Collider>())
                    {
                        string objectName = collider.gameObject.name;
                        if (objectName == "Floor") Assert.That(mask & (1 << collider.gameObject.layer), Is.Not.Zero);
                        if (objectName.StartsWith("Wall_") || objectName == "Ceiling")
                            Assert.That(mask & (1 << collider.gameObject.layer), Is.Not.Zero);
                    }
                var anchor = (Transform)serialized.FindProperty("anchor").objectReferenceValue;
                var handTransform = (Transform)serialized.FindProperty("handPoint").objectReferenceValue;
                // The hose now starts in the second-floor cabinet: measure against that floor and hold the nozzle a few metres away
                // (the player spawn is outside the hose reach, which would leave no slack to settle).
                float floorY = Mathf.Floor(anchor.position.y / 3.2f) * 3.2f;
                Vector3 handPosition = anchor.position.y > 3f ? new Vector3(-1.5f, floorY + 1.4f, 1.0f) : handTransform.position;
                Physics.SyncTransforms();
                using (var resolver = new PhysicsHoseCollision(mask))
                {
                    var profile = (FlameBlame.Core.ScaleProfile)serialized.FindProperty("scale").objectReferenceValue;
                    var sim = new HoseSimulation(serialized.FindProperty("segments").intValue,
                        serialized.FindProperty("segmentLength").floatValue, resolver)
                    {
                        Radius=profile.tubeRadius*.9f, TubeRadius=profile.tubeRadius,
                        RoomHalfExtents=serialized.FindProperty("confineToRoom").boolValue ? new Vector2(profile.roomWidth*.5f,profile.roomLength*.5f) : new Vector2(float.PositiveInfinity,float.PositiveInfinity),
                        BendLimitFactor=profile.bendLimitFactor, Iterations=16
                    };
                    if(serialized.FindProperty("confineToRoom").boolValue) sim.Reset(anchor.position, anchor.right);
                    else sim.ResetBetween(anchor.position,handPosition,Vector3.left);
                    for (int step = 0; step < 600; step++)
                    {
                        sim.Step(0.02f, anchor.position, handPosition, held, Vector3.zero);
                        foreach (var point in sim.Points)
                        {
                            if (anchor.position.y > 3f && Mathf.Abs(point.x) > 2.55f) continue;   // initial tail of the unreeled test chain hangs outside the building
                                Assert.That(point.y, Is.GreaterThanOrEqualTo(floorY + 0.059f), $"Step {step}: {point}");
                        }
                    }
                    Assert.That(System.Array.Exists(sim.Points, point => point.y < floorY + 0.15f), Is.True,
                        "The slack hose should settle on the floor.");
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void Reset_CoilsHoseHorizontallyNearOutlet()
        {
            var sim = new HoseSimulation(32, 0.3f);
            var origin = new Vector3(-5.2f, 0.7f, -4f);
            sim.Reset(origin, Vector3.right);
            foreach (var point in sim.Points)
            {
                Assert.That(point.y, Is.EqualTo(origin.y).Within(0.0001f));
                Assert.That(Vector3.Distance(point, origin), Is.LessThan(2f));
            }
            for (int i = 1; i < sim.Segments; i++)
                Assert.That(Vector3.Distance(sim.Points[i - 1], sim.Points[i]),
                    Is.EqualTo(0.3f).Within(0.001f));
        }

        [TestCase(-12f, 600)]
        [TestCase(-4000f, 1)]
        public void FallingHose_StaysAboveThinFloor(float gravity, int steps)
        {
            var floor = new GameObject("Test floor");
            using (var resolver = new PhysicsHoseCollision(1 << 30))
            {
                try
                {
                    floor.layer = 30;
                    floor.transform.position = new Vector3(0f, -0.1f, 0f);
                    floor.AddComponent<BoxCollider>().size = new Vector3(30f, 0.2f, 30f);
                    Physics.SyncTransforms();
                    var sim = new HoseSimulation(32, 0.3f, resolver) { Gravity = gravity };
                    var origin = new Vector3(0f, 0.7f, 0f);
                    sim.Reset(origin, Vector3.right);
                    for (int step = 0; step < steps; step++)
                    {
                        sim.Step(0.02f, origin, origin, false, Vector3.zero);
                        for (int i = 1; i < sim.Segments; i++)
                            Assert.That(sim.Points[i].y, Is.GreaterThanOrEqualTo(0.059f),
                                $"Point {i}, step {step}");
                    }
                    Assert.That(sim.EndPosition.y, Is.LessThan(0.15f));
                }
                finally { Object.DestroyImmediate(floor); }
            }
        }

        [TestCase(0f, -0.04f, -0.16f)] // underside of a ceiling
        [TestCase(0f, 0.04f, 0.16f)]   // top of a floor
        public void EmbeddedPoint_LeavesThroughNearestSurface(float center, float y, float expectedY)
        {
            var obstacle = new GameObject("Collision test slab");
            var resolver = new PhysicsHoseCollision(1 << 30);
            try
            {
                obstacle.layer = 30;
                var box = obstacle.AddComponent<BoxCollider>();
                box.size = new Vector3(10f, 0.2f, 10f);
                obstacle.transform.position = new Vector3(0f, center, 0f);
                Physics.SyncTransforms();

                var result = resolver.Resolve(new Vector3(0f, y, 0f), 0.06f);

                Assert.That(result.y, Is.EqualTo(expectedY).Within(0.001f));
            }
            finally
            {
                ((object)resolver as System.IDisposable)?.Dispose();
                Object.DestroyImmediate(obstacle);
            }
        }

        [Test]
        public void EmbeddedPoint_LeavesWallSideways()
        {
            var obstacle = new GameObject("Collision test wall");
            var resolver = new PhysicsHoseCollision(1 << 30);
            try
            {
                obstacle.layer = 30;
                obstacle.AddComponent<BoxCollider>().size = new Vector3(0.2f, 10f, 10f);
                Physics.SyncTransforms();
                var result = resolver.Resolve(new Vector3(0.04f, 0f, 0f), 0.06f);
                Assert.That(result.x, Is.EqualTo(0.16f).Within(0.001f));
                Assert.That(result.y, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                ((object)resolver as System.IDisposable)?.Dispose();
                Object.DestroyImmediate(obstacle);
            }
        }
    }
}
