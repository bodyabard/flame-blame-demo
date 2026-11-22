using FlameBlame.Hose;
using NUnit.Framework;
namespace FlameBlame.Tests
{
    public class HoseVisualLogicTests
    {
        [Test]
        public void BendPressure_SmoothlyChokesToThirtyPercent()
        {
            var pressure=new HosePressure();
            pressure.TickBend(0f,1f); Assert.That(pressure.Pressure,Is.EqualTo(1f));
            pressure.TickBend(.5f,2f); Assert.That(pressure.Pressure,Is.EqualTo(.65f).Within(.001f));
            pressure.TickBend(1f,2f); Assert.That(pressure.Pressure,Is.EqualTo(.3f).Within(.001f));
        }
        [Test]
        public void BendChord_RequiresFourRadiusCurvature()
        {
            float chord=HoseBendConstraint.ChordFactor(.2f,.09f,1.8f);
            Assert.That(chord,Is.GreaterThan(1.8f).And.LessThan(2f));
        }
    }
}
