using UnityEngine;

namespace FlameBlame.Npc
{
    /// Comic loop: the NPC pours a thin stream on the fire, then shakes the empty can, forever.
    /// Water drops are decorative only; this actor stays at his attic post.
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
        Phase phase;
        float phaseTime, tilt, mumbleAt;
        bool dropped;
        Quaternion heldRotation;
        Transform player;
        BoxCollider atticVolume;
        bool noticedPlayer;
        float noticeTime = -1f;

        public bool Dropped => dropped;

        public void Initialize(Transform playerTransform, BoxCollider entryVolume)
        {
            player = playerTransform;
            atticVolume = entryVolume;
            npc.IgnoreWaterPush = true;
            npc.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            npc.Face(fireTarget, tuning.turnDegreesPerSecond);
            holdPoint.SetParent(npc.Visual, true);
            can.SetParent(holdPoint, true);
            heldRotation = can.localRotation;
            canBody.isKinematic = true;
            var main = stream.main;
            main.gravityModifier = tuning.dropGravity;
            main.startSpeed = tuning.streamSpeed;
            main.startLifetime = tuning.dropLifetime;
            var collision = stream.collision;
            collision.enabled = false;
            var triggers = stream.trigger;
            triggers.enabled = false;
            ScheduleMumble();
            tilt = tuning.restTilt;
        }

        void ScheduleMumble()
        {
            mumbleAt = Time.time + Random.Range(tuning.mumbleInterval.x, tuning.mumbleInterval.y);
        }

        void Update()
        {
            if (tuning == null) return;
            if (!noticedPlayer && player != null && atticVolume != null)
            {
                if (noticeTime < 0f && atticVolume.bounds.Contains(player.position))
                    noticeTime = Time.time + tuning.noticeDelay;
                if (noticeTime >= 0f && Time.time >= noticeTime)
                {
                    noticedPlayer = true;
                    npc.Face(player, tuning.turnDegreesPerSecond);
                }
            }
            if (dropped) return;
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

            float target = pouring ? tuning.pourTilt + Mathf.Sin(Time.time * tuning.wobbleHz * 6.283f) * tuning.wobbleDegrees
                                   : tuning.restTilt + Mathf.Sin(Time.time * tuning.shakeHz * 6.283f) * tuning.shakeDegrees;
            tilt = Mathf.Lerp(tilt, target, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, tuning.blendSeconds)));

            Vector3 shake = pouring ? Vector3.zero
                : new Vector3(Mathf.Sin(Time.time * 37f), Mathf.Sin(Time.time * 29f), Mathf.Cos(Time.time * 31f)) * tuning.shakeMetres;
            can.localPosition = shake;
            can.localRotation = heldRotation * Quaternion.Euler(tilt, 0f, 0f);
            spout.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            var emission = stream.emission;
            emission.rateOverTime = pouring ? tuning.particleRate : 0f;
            if (!stream.isPlaying) stream.Play();

            gurgle.volume = Mathf.MoveTowards(gurgle.volume, pouring ? tuning.gurgleVolume : 0f, dt * 2f);
            if (Time.time >= mumbleAt && mumbles != null && mumbles.Length > 0)
            {
                voice.PlayOneShot(mumbles[Random.Range(0, mumbles.Length)], tuning.mumbleVolume);
                ScheduleMumble();
            }
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
