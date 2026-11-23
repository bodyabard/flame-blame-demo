using FlameBlame.Hose;
using NUnit.Framework;
using UnityEngine;
namespace FlameBlame.Tests
{
    public class HoseBendPressureTests
    {
        [Test] public void AllowedOutdoorCurveDoesNotChokeButFoldDoes()
        {
            var sim=new HoseSimulation(12,.22636232f) {TubeRadius=.09f};
            for(int i=0;i<12;i++) sim.Points[i]=new Vector3(Mathf.Sin(i*33*Mathf.Deg2Rad),0,Mathf.Cos(i*33*Mathf.Deg2Rad))*.4f;
            Assert.That(sim.BendChoke(),Is.EqualTo(0).Within(.001f),"An allowed relaxed curve must preserve launch pressure");
            sim.Points[5]=sim.Points[3];
            Assert.That(sim.BendChoke(),Is.GreaterThan(.9f),"A real fold must still choke");
        }
    }
}
