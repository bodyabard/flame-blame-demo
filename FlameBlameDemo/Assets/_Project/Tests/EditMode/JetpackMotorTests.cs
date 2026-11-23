using FlameBlame.Player;
using NUnit.Framework;
using UnityEngine;
namespace FlameBlame.Tests
{
    public class JetpackMotorTests
    {
        [Test] public void EquilibriumAndVelocityLimits()
        {
            float pressure=JetpackMotor.EquilibriumPressure(20,26,1);
            Assert.That(26*pressure-20,Is.EqualTo(0).Within(.0001f));
            Assert.That(JetpackMotor.EquilibriumPressure(20,26,.5f),Is.GreaterThan(1));
            var rise=JetpackMotor.LimitVelocity(new Vector3(30,100,40),3.5f,12,4);
            Assert.That(rise.y,Is.EqualTo(3.5f));
            Assert.That(new Vector2(rise.x,rise.z).magnitude,Is.EqualTo(4).Within(.0001f));
            Assert.That(JetpackMotor.LimitVelocity(Vector3.down*100,3.5f,12,4).y,Is.EqualTo(-12));
            var tuning=ScriptableObject.CreateInstance<FlameBlame.Hose.JetTuning>();
            try
            {
                var motor=new JetpackMotor(tuning);
                var tether=new FreeTether();
                for(int i=0;i<20;i++) motor.Step(.05f,true,1,Vector3.down,Vector3.zero,false,Vector3.zero,tether);
                Assert.That(motor.Velocity.y,Is.GreaterThan(0));
                motor.Reset(Vector3.zero);
                for(int i=0;i<40;i++) motor.Step(.02f,true,1,Vector3.down,Vector3.zero,true,Vector3.zero,tether);
                Assert.That(motor.Velocity.y,Is.GreaterThan(0),"Full pressure must lift off from grounded state");
                motor.Reset(Vector3.zero);
                var fall=motor.Step(.1f,false,1,Vector3.down,Vector3.zero,false,Vector3.zero,tether);
                Assert.That(fall.y,Is.EqualTo(-tuning.gravity*.1f).Within(.001f));
                motor.Reset(Vector3.down*tuning.maxFallSpeed);
                for(int i=0;i<15;i++) motor.Step(.02f,true,0,Vector3.down,Vector3.right,false,Vector3.zero,tether);
                Assert.That(motor.Velocity.y,Is.GreaterThan(0),"Reactivation must arrest a fast fall without pressure or landing");
                var zeroPressure=motor.Velocity;
                motor.Reset(Vector3.down*tuning.maxFallSpeed);
                for(int i=0;i<15;i++) motor.Step(.02f,true,1,Vector3.down,Vector3.right,false,Vector3.zero,tether);
                Assert.That(motor.Velocity,Is.EqualTo(zeroPressure));
            }
            finally {Object.DestroyImmediate(tuning);}
        }
        sealed class FreeTether : FlameBlame.Hose.IHoseTether
        {
            public bool IsTethered=>false;
            public Vector3 AnchorPosition=>Vector3.zero;
            public float MaxReach=>100;
        }
    }
}
