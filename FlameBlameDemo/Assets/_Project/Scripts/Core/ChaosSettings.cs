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
        [Header("Particle budget: 400 jet + 320 flame + 72 steam + 80 splash + 32 sparks = 904")]
        public int flamePoolSize = 16;
        public int steamPoolSize = 6;
        public int splashPoolSize = 8;
        public int sparkPoolSize = 4;
    }
}
