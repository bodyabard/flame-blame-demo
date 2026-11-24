using FlameBlame.UI;
using NUnit.Framework;
using UnityEngine;

namespace FlameBlame.Tests
{
    public class UiMotionTests
    {
        [Test]
        public void Easing_AllCurvesStartAtZeroAndEndAtOne()
        {
            foreach (Ease ease in System.Enum.GetValues(typeof(Ease)))
            {
                Assert.AreEqual(0f, Easing.Evaluate(ease, 0f), 1e-4f, ease.ToString());
                Assert.AreEqual(1f, Easing.Evaluate(ease, 1f), 1e-4f, ease.ToString());
            }
            Assert.Greater(Easing.Evaluate(Ease.OutBack, 0.7f), 1f);   // overshoot is the point of OutBack
        }

        [Test]
        public void Spring_OvershootsThenSettlesOnTarget()
        {
            var spring = new Spring();
            float peak = 0f;
            for (int i = 0; i < 240; i++)
            {
                spring.Step(1f, 1f / 60f, 3.2f, 0.38f);
                peak = Mathf.Max(peak, spring.Value);
            }
            Assert.Greater(peak, 1.05f);
            Assert.AreEqual(1f, spring.Value, 0.02f);
        }

        [Test]
        public void Tween_RestartOnSameChannelReplacesInsteadOfStacking()
        {
            var runner = new UITweenRunner(8);
            var go = new GameObject("tween-owner");
            float value = 0f;
            System.Action<float> setter = v => value = v;

            runner.Value(go, 0, setter, 0f, 10f, 1f, Ease.Linear);
            runner.Tick(0.5f);
            Assert.AreEqual(5f, value, 1e-3f);

            runner.Value(go, 0, setter, value, 100f, 1f, Ease.Linear);   // restart toward a new target
            runner.Tick(1f);
            Assert.AreEqual(100f, value, 1e-3f);                           // finished once, no leftover first tween
            runner.Tick(1f);
            Assert.AreEqual(100f, value, 1e-3f);
            Object.DestroyImmediate(go);
        }
    }
}
