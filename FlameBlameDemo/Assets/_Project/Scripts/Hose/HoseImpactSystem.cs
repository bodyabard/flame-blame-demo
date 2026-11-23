using FlameBlame.Core;
using FlameBlame.Fire;
using UnityEngine;

namespace FlameBlame.Hose
{
    public sealed class HoseImpactSystem
    {
        readonly Vector3[] previous;
        readonly Collider[] hits = new Collider[16];
        readonly Rigidbody[] touched = new Rigidbody[32];
        readonly ChaosSettings settings;
        readonly int mask;
        public HoseImpactSystem(Vector3[] points, ChaosSettings settings)
        {
            previous = (Vector3[])points.Clone();
            this.settings = settings;
            mask = LayerMask.GetMask("Interactable");
        }
        public void Tick(Vector3[] points, float dt, int activeCount=-1, bool resetVelocity=false)
        {
            if(resetVelocity) {System.Array.Copy(points,previous,points.Length); return;}
            int touchedCount = 0;
            for (int i = 1; i < (activeCount<0 ? points.Length : activeCount); i++)
            {
                Vector3 velocity = (points[i] - previous[i]) / dt;
                previous[i] = points[i];
                float speed = velocity.magnitude;
                if (speed < settings.hoseHitMinimumSpeed) continue;
                int count = Physics.OverlapSphereNonAlloc(points[i], settings.hoseHitRadius, hits, mask, QueryTriggerInteraction.Ignore);
                for (int j = 0; j < count; j++)
                {
                    var rb = hits[j].attachedRigidbody;
                    if (rb == null || rb.isKinematic || touchedCount == touched.Length) continue;
                    bool duplicate = false;
                    for (int k = 0; k < touchedCount; k++) if (touched[k] == rb) { duplicate = true; break; }
                    if (duplicate) continue;
                    touched[touchedCount++] = rb;
                    rb.AddForceAtPosition(Vector3.ClampMagnitude(velocity, 12f) * settings.hoseHitImpulse, points[i], ForceMode.Impulse);
                    rb.GetComponent<PropertyDamage>()?.Hit(speed);
                }
            }
        }
    }
}
