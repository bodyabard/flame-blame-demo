using FlameBlame.Hose;
using NUnit.Framework;
using UnityEngine;
namespace FlameBlame.Tests
{
    public class HoseReelTests
    {
        sealed class Wall : IHoseVisibility
        {
            public bool Clear(Vector3 a,Vector3 b,float r) => a.y>=1.5f && b.y>=1.5f || a.x<.5f && b.x<.5f || a.x>1.5f && b.x>1.5f;
        }
        [Test] public void RouteRetainsObstacleDetourAndRetractionUsesVisibleReturn()
        {
            var reel=new HoseReel(new Wall());
            var points=new[]{new Vector3(0,0,0),new Vector3(0,2,0),new Vector3(2,2,0),new Vector3(2,0,0)};
            Assert.That(reel.RouteLength(points,4,.1f,128),Is.EqualTo(6).Within(.001f));
            points[3]=new Vector3(0,1,0);
            Assert.That(reel.RouteLength(points,4,.1f,128),Is.EqualTo(1).Within(.001f));
            Assert.That(reel.RouteLength(points,4,.1f,0),Is.EqualTo(float.PositiveInfinity));
            Assert.That(HoseReel.RequiredPoints(1,1,2,.25f,64),Is.EqualTo(9));
        }
        [Test] public void ResizeAtHydrantPreservesNozzleCapacityAndMaximumReach()
        {
            var sim=new HoseSimulation(64,.25f); sim.ResetBetween(Vector3.zero,Vector3.right*2,Vector3.forward);
            Vector3 end=sim.EndPosition; float reach=sim.MaxReach;
            sim.SetActiveCount(12);
            Assert.That(sim.EndPosition,Is.EqualTo(end)); Assert.That(sim.Capacity,Is.EqualTo(64));
            sim.SetActiveCount(30);
            Assert.That(sim.EndPosition,Is.EqualTo(end)); Assert.That(sim.MaxReach,Is.EqualTo(reach));
            for(int i=0;i<30;i++) sim.Step(.02f,Vector3.zero,end,true,Vector3.zero);
            Assert.That(float.IsNaN(sim.EndPosition.x),Is.False);
        }
        [Test] public void ThinFenceCrossingBetweenPointsHasSpanCorrection()
        {
            var go=new GameObject("Reel test fence"); go.layer=30;
            try
            {
                go.AddComponent<BoxCollider>().size=new Vector3(.04f,1,2); Physics.SyncTransforms();
                using(var collision=new PhysicsHoseCollision(1<<30))
                {
                    var a=new Vector3(-.3f,0,0); var b=new Vector3(.3f,0,0);
                    Assert.That(collision.Resolve(a,.1f),Is.EqualTo(a));
                    Assert.That(collision.Resolve(b,.1f),Is.EqualTo(b));
                    var correction=collision.SpanCorrection(a,b,.1f);
                    Assert.That(correction.sqrMagnitude,Is.GreaterThan(0));
                    Assert.That(collision.Clear(a+correction,b+correction,.1f),Is.True);
                    Assert.That(collision.Clear(a,b,.1f),Is.False);
                    collision.AttachmentRoot=go.transform;
                    Assert.That(collision.Clear(a,b,.1f),Is.True,"The outlet's own attachment must not block payout");
                }
            }
            finally {Object.DestroyImmediate(go);}
        }
    }
}
