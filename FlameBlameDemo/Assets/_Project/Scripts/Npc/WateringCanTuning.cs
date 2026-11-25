using UnityEngine;

namespace FlameBlame.Npc
{
    /// Numbers for the NPC with the watering can. All feel is tuned here.
    [CreateAssetMenu(menuName = "FlameBlame/Watering Can Tuning")]
    public sealed class WateringCanTuning : ScriptableObject
    {
        [Header("Cycle")]
        [Min(1f)] public float pourSeconds = 6f;
        [Min(0.5f)] public float emptySeconds = 1.8f;
        [Min(0f)] public float blendSeconds = 0.35f;

        [Header("Pose")]
        public float pourTilt = 52f;
        public float restTilt = 8f;
        public float wobbleDegrees = 4f;
        public float wobbleHz = 3f;
        public float shakeDegrees = 14f;
        public float shakeHz = 11f;
        public float shakeMetres = 0.05f;

        [Header("Notice player in attic")]
        [Min(0f)] public float noticeDelay = 0.65f;
        [Min(1f)] public float turnDegreesPerSecond = 150f;

        [Header("Water")]
        [Tooltip("Water units per second delivered to what the stream hits. The hose does ~1 per second at full pressure.")]
        [Range(0f, 0.2f)] public float waterPerSecond = 0.035f;
        [Min(0.5f)] public float range = 3.2f;
        [Min(0.02f)] public float radius = 0.1f;
        public int particleRate = 28;
        [Min(0.1f)] public float streamSpeed = 2.4f;

        [Header("Voice")]
        public Vector2 mumbleInterval = new Vector2(6f, 11f);
        [Range(0f, 1f)] public float gurgleVolume = 0.22f;
        [Range(0f, 1f)] public float mumbleVolume = 0.35f;

        [Header("Dropped can")]
        public float dropImpulse = 2.2f;
    }
}
