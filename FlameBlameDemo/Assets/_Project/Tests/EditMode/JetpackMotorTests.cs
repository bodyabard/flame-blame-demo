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
        }
    }
}
