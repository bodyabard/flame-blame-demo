using FlameBlame.Ladder;
using NUnit.Framework;
using UnityEngine;
namespace FlameBlame.Tests
{
    public class LadderRulesTests
    {
        [Test] public void PlacementGeometryAndKnockDecay()
        {
            Assert.That(LadderRules.BaseOffset(4.5f,73),Is.EqualTo(1.316f).Within(.01f));
            Assert.That(LadderRules.Height(4.5f,73),Is.GreaterThan(4.15f));
            Assert.That(LadderRules.ValidWall(Vector3.right,15),Is.True);
            Assert.That(LadderRules.ValidWall(Vector3.up,15),Is.False);
            Assert.That(LadderRules.ValidAngle(73),Is.True);
            Assert.That(LadderRules.ValidAngle(60),Is.False);
            float knock=0;
            for(int i=0;i<4;i++) knock=LadderRules.AddKnock(knock,.1f,1);
            Assert.That(knock,Is.GreaterThanOrEqualTo(.4f));
            Assert.That(LadderRules.Decay(knock,.2f,1),Is.EqualTo(.2f).Within(.001f));
            Assert.That(LadderRules.Decay(knock,.2f,10),Is.Zero);
        }
    }
}
