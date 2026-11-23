using UnityEngine;

namespace FlameBlame.Hose
{
    public sealed class PhysicsHoseCollision : IHoseMotionCollisionResolver, IHoseSpanCollision, IHoseVisibility, System.IDisposable
    {
        readonly LayerMask mask;
        readonly Collider[] buffer = new Collider[32];
        readonly RaycastHit[] sweepHits=new RaycastHit[32];
        public Transform AttachmentRoot {get;set;}
        bool Relevant(Collider collider) => AttachmentRoot==null || !collider.transform.IsChildOf(AttachmentRoot);
        readonly SphereCollider probe;
        readonly CapsuleCollider spanProbe;

        public PhysicsHoseCollision(LayerMask mask)
        {
            this.mask = mask;
            var go = new GameObject("Hose collision probe") { hideFlags = HideFlags.HideAndDontSave };
            probe = go.AddComponent<SphereCollider>();
            // Keep the native shape available for ComputePenetration. Trigger
            // queries are ignored above, so this probe cannot collide with the hose.
            probe.isTrigger = true;
            spanProbe=go.AddComponent<CapsuleCollider>(); spanProbe.isTrigger=true; spanProbe.direction=2;
        }

        public Vector3 Resolve(Vector3 pt, float radius)
        {
            probe.radius = radius;
            for(int pass=0;pass<4;pass++)
            {
            bool moved=false;
            int c = Physics.OverlapSphereNonAlloc(pt, radius, buffer, mask, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < c; k++)
            {
                var obstacle = buffer[k];
                if(!Relevant(obstacle)) continue;
                // ClosestPoint returns pt when embedded; use the actual separation
                // direction rather than lifting the hose through walls and ceilings.
                if (Physics.ComputePenetration(probe, pt, Quaternion.identity,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation,
                    out Vector3 direction, out float distance))
                {pt += direction * (distance+.0001f); moved=true;}
            }
            if(!moved) break;
            }
            return pt;
        }
        public bool Clear(Vector3 from,Vector3 to,float radius)
        {
            if(Embedded(from,radius) || Embedded(to,radius)) return false;
            Vector3 delta=to-from;
            return delta.sqrMagnitude<.000001f || SweepDistance(from,delta,radius)>=delta.magnitude;
        }
        bool Embedded(Vector3 point,float radius)
        {
            int count=Physics.OverlapSphereNonAlloc(point,radius,buffer,mask,QueryTriggerInteraction.Ignore);
            if(count==buffer.Length) return true;
            for(int i=0;i<count;i++) if(Relevant(buffer[i])) return true;
            return false;
        }
        float SweepDistance(Vector3 from,Vector3 delta,float radius)
        {
            int count=Physics.SphereCastNonAlloc(from,radius,delta.normalized,sweepHits,delta.magnitude,mask,QueryTriggerInteraction.Ignore);
            float distance=delta.magnitude;
            if(count==sweepHits.Length) return 0;
            for(int i=0;i<count;i++) if(Relevant(sweepHits[i].collider)) distance=Mathf.Min(distance,sweepHits[i].distance);
            return distance;
        }
        public Vector3 SpanCorrection(Vector3 from,Vector3 to,float radius)
        {
            Vector3 delta=to-from;
            if(delta.sqrMagnitude<.000001f) return Vector3.zero;
            spanProbe.radius=radius; spanProbe.height=delta.magnitude+2*radius;
            Vector3 center=(from+to)*.5f, correction=Vector3.zero;
            Quaternion rotation=Quaternion.LookRotation(delta);
            int count=Physics.OverlapCapsuleNonAlloc(from,to,radius,buffer,mask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(Relevant(buffer[i]) && Physics.ComputePenetration(spanProbe,center+correction,rotation,buffer[i],buffer[i].transform.position,buffer[i].transform.rotation,out var normal,out float distance))
                    correction+=normal*(distance+.0001f);
            return correction;
        }

        public Vector3 ResolveMotion(Vector3 from, Vector3 to, float radius)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance > 0.00001f)
            {
                float clear=SweepDistance(from,delta,radius);
                if(clear<distance) to = from + delta / distance * Mathf.Max(0f, clear - 0.001f);
            }
            return Resolve(to, radius);
        }

        public void Dispose()
        {
            if (probe == null) return;
            if (Application.isPlaying) Object.Destroy(probe.gameObject);
            else Object.DestroyImmediate(probe.gameObject);
        }
    }
}
