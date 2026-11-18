using UnityEngine;

namespace FlameBlame.Hose
{
    public sealed class PhysicsHoseCollision : IHoseCollisionResolver
    {
        readonly LayerMask mask;
        readonly Collider[] buffer = new Collider[8];

        public PhysicsHoseCollision(LayerMask mask) { this.mask = mask; }

        public Vector3 Resolve(Vector3 pt, float radius)
        {
            int c = Physics.OverlapSphereNonAlloc(pt, radius, buffer, mask, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < c; k++)
            {
                Vector3 closest = buffer[k].ClosestPoint(pt);
                Vector3 dir = pt - closest;
                float dist = dir.magnitude;
                if (dist < 1e-4f) { pt += Vector3.up * radius; continue; }
                if (dist < radius) pt += dir / dist * (radius - dist);
            }
            return pt;
        }
    }
}
