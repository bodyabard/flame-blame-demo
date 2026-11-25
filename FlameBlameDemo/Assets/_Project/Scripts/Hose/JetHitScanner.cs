using FlameBlame.Water;
using UnityEngine;

namespace FlameBlame.Hose
{
    /// Jet logic: SphereCast along the nozzle, delivers water to IWaterReceiver and pushes rigidbodies.
    public sealed class JetHitScanner
    {
        public float Radius = 0.12f;
        public LayerMask Mask = ~0;

        public bool Scan(Vector3 origin, Vector3 dir, float range, float waterAmount, float pushForce, out RaycastHit hit)
        {
            if (!Physics.SphereCast(origin, Radius, dir, out hit, range, Mask, QueryTriggerInteraction.Ignore))
                return false;

            var receiver = hit.collider.GetComponentInParent<IWaterReceiver>();
            if (receiver != null) receiver.ReceiveWater(hit.point, hit.normal, waterAmount);
            var npc = receiver as FlameBlame.Npc.NpcActor;
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic && (npc == null || !npc.IgnoreWaterPush))
            {
                hit.rigidbody.AddForceAtPosition(dir * pushForce, hit.point, ForceMode.Force);
            }
            return true;
        }
    }
}
