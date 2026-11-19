using UnityEngine;

namespace FlameBlame.Core
{
    [CreateAssetMenu(menuName = "FlameBlame/Chaos Settings")]
    public class ChaosSettings : ScriptableObject
    {
        [Header("Round")]
        public float roundSeconds = 105f;
        public int damageLimit = 10000;
        public float minimumRoundSeconds = 12f;
        public float firesOutDelay = 3f;
        [Header("Water / electricity")]
        [Range(1, 48)] public int maxPuddles = 24;
        public float evaporationSeconds = 22f;
        public float shockCooldown = 2f;
        public float shockStrength = 5f;
        public int npcShockDamage = 180;
        public int playerShockDamage = 80;
        [Header("Hose hits")]
        public float hoseHitRadius = 0.16f;
        public float hoseHitMinimumSpeed = 2.5f;
        public float hoseHitImpulse = 0.8f;
        [Header("Adaptive visual budget (gameplay remains unchanged)")]
        public float lowFps = 40f;
        public float recoverFps = 55f;
        public float qualitySampleSeconds = 1f;
        [Range(.1f, 1f)] public float minimumEffectQuality = .35f;
        public float effectCullDistance = 18f;
        [Header("Particle ceiling: 240 jet + 16x36 flame + 6x12 steam + 8x10 splash + 4x8 spark = 1000")]
        public int flamePoolSize = 16;
        public int steamPoolSize = 6;
        public int splashPoolSize = 8;
        public int sparkPoolSize = 4;
    }
}
