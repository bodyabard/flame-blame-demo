using FlameBlame.Water;
using UnityEngine;

namespace FlameBlame.Npc
{
    /// Comic loop: the NPC pours a thin stream on the fire, then shakes the empty can, forever.
    /// The stream really delivers water through IWaterReceiver, just far too little to win.
    /// If the player washes the NPC away the can falls to the floor.
    public sealed class WateringCanNpc : MonoBehaviour
    {
        enum Phase { Pour, Empty }

        [SerializeField] NpcActor npc;
        [SerializeField] WateringCanTuning tuning;
        [SerializeField] Transform can;             // visual root, moved/rotated by this script
        [SerializeField] Transform spout;           // stream origin (child of can)
        [SerializeField] Transform fireTarget;      // what he is trying to put out
        [SerializeField] Transform holdPoint;       // where the can floats next to him
        [SerializeField] ParticleSystem stream;
        [SerializeField] Rigidbody canBody;         // kinematic until dropped
        [SerializeField] AudioSource gurgle, voice;
        [SerializeField] AudioClip[] mumbles;
        [SerializeField] LayerMask hitMask = ~0;

        readonly RaycastHit[] hits = new RaycastHit[6];
        Phase phase;
        float phaseTime, tilt, mumbleAt;
        bool dropped;
        Vector3 holdLocal;

        public bool Dropped => dropped;

        public void Initialize()
        {
            holdLocal = transform.InverseTransformPoint(holdPoint.position);
            canBody.isKinematic = true;
            ScheduleMumble();
            tilt = tuning.restTilt;
        }

        void ScheduleMumble()
        {
            mumbleAt = Time.time + Random.Range(tuning.mumbleInterval.x, tuning.mumbleInterval.y);
        }

        void Update()
        {
            if (tuning == null || dropped) return;
            if (npc.State == NpcState.Washed || npc.State == NpcState.Shocked) { Drop(); return; }

            float dt = Time.deltaTime;
            phaseTime += dt;
            bool pouring = phase == Phase.Pour;
            if (phaseTime >= (pouring ? tuning.pourSeconds : tuning.emptySeconds))
            {
                phase = pouring ? Phase.Empty : Phase.Pour;
                phaseTime = 0f;
                pouring = phase == Phase.Pour;
            }

            Vector3 origin = transform.TransformPoint(holdLocal);
            Vector3 toFire = fireTarget.position - origin;
            Vector3 flat = Vector3.ProjectOnPlane(toFire, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) flat = transform.forward;
            Quaternion face = Quaternion.LookRotation(flat.normalized, Vector3.up);

            float target = pouring ? tuning.pourTilt + Mathf.Sin(Time.time * tuning.wobbleHz * 6.283f) * tuning.wobbleDegrees
                                   : tuning.restTilt + Mathf.Sin(Time.time * tuning.shakeHz * 6.283f) * tuning.shakeDegrees;
            tilt = Mathf.Lerp(tilt, target, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, tuning.blendSeconds)));

            Vector3 shake = pouring ? Vector3.zero
                : new Vector3(Mathf.Sin(Time.time * 37f), Mathf.Sin(Time.time * 29f), Mathf.Cos(Time.time * 31f)) * tuning.shakeMetres;
            can.SetPositionAndRotation(origin + shake, face * Quaternion.Euler(tilt, 0f, 0f));

            var emission = stream.emission;
            emission.rateOverTime = pouring ? tuning.particleRate : 0f;
            if (!stream.isPlaying) stream.Play();
            if (pouring) Pour();

            gurgle.volume = Mathf.MoveTowards(gurgle.volume, pouring ? tuning.gurgleVolume : 0f, dt * 2f);
            if (Time.time >= mumbleAt && mumbles != null && mumbles.Length > 0)
            {
                voice.PlayOneShot(mumbles[Random.Range(0, mumbles.Length)], tuning.mumbleVolume);
                ScheduleMumble();
            }
        }

        void Pour()
        {
            Vector3 from = spout.position;
            Vector3 dir = (fireTarget.position - from).normalized;
            int count = Physics.SphereCastNonAlloc(from, tuning.radius, dir, hits, tuning.range, hitMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform) || hits[i].collider.transform.IsChildOf(can)) continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; best = i; }
            }
            if (best < 0) return;
            var receiver = hits[best].collider.GetComponentInParent<IWaterReceiver>();
            if (receiver != null && !ReferenceEquals(receiver, npc))
                receiver.ReceiveWater(hits[best].point, hits[best].normal, tuning.waterPerSecond * Time.deltaTime);
        }

        void Drop()
        {
            dropped = true;
            var emission = stream.emission;
            emission.rateOverTime = 0f;
            gurgle.Stop();
            can.SetParent(null, true);
            canBody.isKinematic = false;
            canBody.AddForce((Vector3.up * 0.6f + transform.forward * 0.3f) * tuning.dropImpulse, ForceMode.VelocityChange);
            canBody.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
        }
    }
}
