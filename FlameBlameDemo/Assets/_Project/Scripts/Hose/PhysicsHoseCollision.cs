using UnityEngine;

namespace FlameBlame.Hose
{
    public sealed class PhysicsHoseCollision : IHoseMotionCollisionResolver, System.IDisposable
    {
        readonly LayerMask mask;
        readonly Collider[] buffer = new Collider[8];
        readonly SphereCollider probe;

        public PhysicsHoseCollision(LayerMask mask)
        {
            this.mask = mask;
            var go = new GameObject("Hose collision probe") { hideFlags = HideFlags.HideAndDontSave };
            probe = go.AddComponent<SphereCollider>();
            // Keep the native shape available for ComputePenetration. Trigger
            // queries are ignored above, so this probe cannot collide with the hose.
            probe.isTrigger = true;
        }

        public Vector3 Resolve(Vector3 pt, float radius)
        {
            probe.radius = radius;
            int c = Physics.OverlapSphereNonAlloc(pt, radius, buffer, mask, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < c; k++)
            {
                var obstacle = buffer[k];
                // ClosestPoint returns pt when embedded; use the actual separation
                // direction rather than lifting the hose through walls and ceilings.
                if (Physics.ComputePenetration(probe, pt, Quaternion.identity,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation,
                    out Vector3 direction, out float distance))
                    pt += direction * distance;
            }
            return pt;
        }

        public Vector3 ResolveMotion(Vector3 from, Vector3 to, float radius)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance > 0.00001f && Physics.SphereCast(from, radius, delta / distance,
                out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                to = from + delta / distance * Mathf.Max(0f, hit.distance - 0.001f);
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
