using FlameBlame.Core;
using FlameBlame.Fire;
using NUnit.Framework;
using UnityEngine;

namespace FlameBlame.Tests
{
    public class FireLookTests
    {
        static FireMaterialProfile LongBurningProfile(float reheat = 0f)
        {
            var p = ScriptableObject.CreateInstance<FireMaterialProfile>();
            p.ignitionThreshold = 50f; p.burnDuration = 300f; p.waterCooling = 100f; p.coolRate = 0f;
            p.charSeconds = 18f; p.ignitionChar = 0.35f; p.reheatPerSecond = reheat;
            return p;
        }

        [Test]
        public void Burning_ObjectIsAlreadyDarkTheMomentItIgnites_EvenWithLongFuel()
        {
            var fire = new FireSystem(new GameEvents());
            var node = new FireNode(LongBurningProfile(), Vector3.zero);
            fire.Register(node);
            Assert.AreEqual(0f, node.Charred01, 1e-4f);

            fire.Ignite(node);
            Assert.GreaterOrEqual(node.Charred01, 0.35f - 1e-4f);

            for (int i = 0; i < 100; i++) fire.Tick(0.1f);           // 10 s of a 300 s fire
            Assert.Greater(node.Charred01, 0.4f);
        }

        [Test]
        public void Reheat_WeakWaterCannotExtinguish_StrongWaterCan()
        {
            var fire = new FireSystem(new GameEvents());
            var node = new FireNode(LongBurningProfile(reheat: 7f), Vector3.zero);
            fire.Register(node);
            fire.Ignite(node);

            // a watering can: 0.035 water/s * 100 cooling = 3.5 heat/s against 7 heat/s regained
            for (int i = 0; i < 600; i++) { fire.ApplyWater(node, 0.035f * 0.1f); fire.Tick(0.1f); }
            Assert.IsTrue(node.IsBurning);

            // a hose: 1 water/s * 100 cooling
            bool out_ = false;
            for (int i = 0; i < 100 && !out_; i++) { out_ = fire.ApplyWater(node, 1f * 0.1f); fire.Tick(0.1f); }
            Assert.IsTrue(out_);
        }
    }
}
