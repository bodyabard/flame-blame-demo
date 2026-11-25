using UnityEngine;

namespace FlameBlame.Fire
{
    /// Strategy data: how a material heats up, burns, spreads and reacts to water.
    [CreateAssetMenu(menuName = "FlameBlame/Fire Material Profile", fileName = "FireProfile_")]
    public class FireMaterialProfile : ScriptableObject
    {
        [Header("Ignition")]
        public bool canBurn = true;
        [Tooltip("Heat needed to catch fire.")] public float ignitionThreshold = 100f;
        [Tooltip("Multiplier on heat received from burning neighbours.")] public float flammability = 1f;
        [Tooltip("Heat per second lost when not burning.")] public float coolRate = 5f;

        [Header("Burning")]
        [Tooltip("Total seconds of fuel. Extinguishing does not refill it.")] public float burnDuration = 15f;
        [Tooltip("Heat per second given to neighbours at zero distance.")] public float spreadHeatPerSecond = 40f;
        public float spreadRadius = 1.6f;

        [Header("Burning look")]
        [Tooltip("Seconds of burning for the char to reach ~63%. Independent of fuel, so long fires darken at the same pace.")] public float charSeconds = 18f;
        [Tooltip("Char level a burning object shows the instant it ignites, so flames always sit on a dark body.")] [Range(0f, 1f)] public float ignitionChar = 0.35f;
        [Tooltip("Heat per second a burning object regains; weak water sources cannot overcome it.")] public float reheatPerSecond = 0f;

        [Header("Water")]
        [Tooltip("Heat removed per unit of water.")] public float waterCooling = 100f;

        [Header("Look")]
        public Color charColor = new Color(0.08f, 0.07f, 0.06f);
        public Color glowColor = new Color(1f, 0.35f, 0.05f);
        public float glowIntensity = 1.5f;
        public float flameScale = 1f;

        [Header("Damage score")]
        public int damageOnIgnite = 20;
        public int damageOnBurnOut = 50;
    }
}
