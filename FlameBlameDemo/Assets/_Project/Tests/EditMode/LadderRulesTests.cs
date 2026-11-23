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
        [Test] public void ViewControlsDirectionAndLookingAwayDetachesEvenWithoutMovement()
        {
            Assert.That(LadderRules.ViewClimbSign(Vector3.up,1,.15f),Is.EqualTo(1));
            Assert.That(LadderRules.ViewClimbSign(Vector3.down,1,.15f),Is.EqualTo(-1));
            Assert.That(LadderRules.ViewClimbSign(Vector3.up,-1,.15f),Is.EqualTo(1));
            Assert.That(LadderRules.ViewClimbSign(Vector3.forward,-1,.15f),Is.EqualTo(-1));
            var eye=new Vector3(0,2,-.55f); var bottom=Vector3.zero; var top=Vector3.up*4;
            Assert.That(LadderRules.FacingLadder(eye,(top-eye).normalized,bottom,top,70),Is.True);
            Assert.That(LadderRules.FacingLadder(eye,(bottom-eye).normalized,bottom,top,70),Is.True);
            Assert.That(LadderRules.FacingLadder(eye,Vector3.back,bottom,top,70),Is.False);
            Assert.That(LadderRules.FacingLadder(eye,Vector3.right,bottom,top,70),Is.False);
        }
    }
}
