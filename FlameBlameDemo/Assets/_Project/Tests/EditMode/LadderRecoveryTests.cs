using FlameBlame.Ladder;
using NUnit.Framework;
using UnityEngine;
namespace FlameBlame.Tests
{
    public class LadderRecoveryTests
    {
        [Test] public void WaterWakesRestingBodyAndPreventsImmediateSleep()
        {
            var go=new GameObject("Water recovery test");
            var tuning=ScriptableObject.CreateInstance<LadderTuning>();
            try
            {
                var ladder=go.AddComponent<LadderStateMachine>(); ladder.tuning=tuning; typeof(LadderStateMachine).GetMethod("Awake",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(ladder,null);
                ladder.Body.Sleep(); ladder.ReceiveWater(go.transform.position,Vector3.left,.02f);
                for(int i=0;i<100;i++) typeof(LadderStateMachine).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(ladder,null);
                Assert.That(ladder.State,Is.EqualTo(LadderState.Falling));
                Assert.That(ladder.Body.IsSleeping(),Is.False);
                Assert.That(ladder.Body.isKinematic,Is.False);
            }
            finally {Object.DestroyImmediate(go); Object.DestroyImmediate(tuning);}
        }
    }
}
