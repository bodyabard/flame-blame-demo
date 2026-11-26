using FlameBlame.Core;
using FlameBlame.Fire;
using FlameBlame.Hose;
using NUnit.Framework;
using UnityEngine;

namespace FlameBlame.Tests
{
    public class FireAndPressureTests
    {
        static FireMaterialProfile Profile(float threshold, float flammability, float burn = 10f)
        {
            var p = ScriptableObject.CreateInstance<FireMaterialProfile>();
            p.ignitionThreshold = threshold;
            p.flammability = flammability;
            p.burnDuration = burn;
            p.coolRate = 0f;
            p.spreadHeatPerSecond = 50f;
            p.spreadRadius = 2f;
            p.waterCooling = 100f;
            return p;
        }

        static void Run(FireSystem fire, float seconds, float dt = 0.1f)
        {
            for (float t = 0f; t < seconds; t += dt) fire.Tick(dt);
        }

        [Test]
        public void Pressure_IsBaseWithoutKinksAndChokedWithMany()
        {
            Assert.AreEqual(1f, HosePressure.TargetPressure(1f, 0, 0.15f, 3f), 1e-4f);
            Assert.AreEqual(0.15f, HosePressure.TargetPressure(1f, 3, 0.15f, 3f), 1e-4f);
            Assert.AreEqual(0.15f, HosePressure.TargetPressure(1f, 10, 0.15f, 3f), 1e-4f);
            float one = HosePressure.TargetPressure(1f, 1, 0.15f, 3f);
            Assert.Less(one, 1f);
            Assert.Greater(one, 0.15f);
        }

        [Test]
        public void Fire_SpreadsToNeighbourInRadiusButNotFarOne()
        {
            var fire = new FireSystem(new GameEvents());
            var source = new FireNode(Profile(100f, 1f), Vector3.zero);
            var near = new FireNode(Profile(50f, 1f), new Vector3(1f, 0f, 0f));
            var far = new FireNode(Profile(50f, 1f), new Vector3(5f, 0f, 0f));
            fire.Register(source); fire.Register(near); fire.Register(far);

            Assert.IsTrue(fire.Ignite(source));
            Run(fire, 5f);

            Assert.AreEqual(FireState.Burning, near.State);
            Assert.AreEqual(FireState.Idle, far.State);
            Assert.AreEqual(0f, far.Heat);
        }

        [Test]
        public void Water_CoolsBurningNodeToZeroAndExtinguishes()
        {
            var events = new GameEvents();
            int extinguished = 0;
            events.FireExtinguished += _ => extinguished++;
            var fire = new FireSystem(events);
            var node = new FireNode(Profile(100f, 1f), Vector3.zero);
            fire.Register(node);
            fire.Ignite(node);

            Assert.IsFalse(fire.ApplyWater(node, 0.5f));      // 50 of 100 heat removed
            Assert.AreEqual(FireState.Burning, node.State);
            Assert.IsTrue(fire.ApplyWater(node, 0.6f));
            Assert.AreEqual(FireState.Idle, node.State);
            Assert.AreEqual(1, extinguished);
            Assert.AreEqual(0, fire.ActiveCount);
        }

        [Test]
        public void ExtinguishedNode_CannotReigniteFromNeighbourFire()
        {
            var fire = new FireSystem(new GameEvents());
            var source = new FireNode(Profile(100f, 1f, 60f), Vector3.zero);
            var neighbour = new FireNode(Profile(50f, 1f, 60f), new Vector3(1f, 0f, 0f));
            fire.Register(source); fire.Register(neighbour);
            fire.Ignite(source);
            Run(fire, 5f);
            Assert.AreEqual(FireState.Burning, neighbour.State);

            Assert.IsTrue(fire.ApplyWater(neighbour, 10f));
            Assert.IsTrue(neighbour.Doused);
            Run(fire, 30f);                                    // the source keeps burning next to it the whole time

            Assert.AreEqual(FireState.Burning, source.State);
            Assert.AreEqual(FireState.Idle, neighbour.State);
            Assert.AreEqual(0f, neighbour.Heat);
            Assert.IsFalse(fire.Ignite(neighbour));
        }

        [Test]
        public void Fire_RespectsMaxActiveFires()
        {
            var fire = new FireSystem(new GameEvents(), 2);
            var nodes = new FireNode[3];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new FireNode(Profile(100f, 1f), Vector3.zero);
                fire.Register(nodes[i]);
            }

            Assert.IsTrue(fire.Ignite(nodes[0]));
            Assert.IsTrue(fire.Ignite(nodes[1]));
            Assert.IsFalse(fire.Ignite(nodes[2]));
            Assert.AreEqual(2, fire.ActiveCount);
        }

        [Test]
        public void Fire_BurnsOutIntoCharredAndCannotReignite()
        {
            var fire = new FireSystem(new GameEvents());
            var node = new FireNode(Profile(100f, 1f, burn: 2f), Vector3.zero);
            fire.Register(node);
            fire.Ignite(node);

            Run(fire, 3f);

            Assert.AreEqual(FireState.Charred, node.State);
            Assert.AreEqual(1f, node.Charred01, 1e-4f);
            Assert.IsFalse(fire.Ignite(node));
            Assert.AreEqual(0, fire.ActiveCount);
        }
    }
}
