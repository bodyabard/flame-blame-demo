using UnityEngine;

namespace FlameBlame.Feedback
{
    [CreateAssetMenu(menuName = "FlameBlame/Visual Tuning")]
    public sealed class VisualTuning : ScriptableObject
    {
        [Header("Camera and hands")]
        public float baseFov = 70f, fovKick = 4f, fovResponse = 8f;
        public float bobHeight = .018f, bobSpeed = 9f, handSway = .006f;
        public float shakeAngle = 2.2f, shakeSpeed = 25f;
        public Color sleeve = new Color(.64f,.33f,.12f), glove = new Color(.055f,.07f,.1f);
        public Color reflectiveStripe = new Color(.93f,1f,.23f);
        [Header("Expressions and HUD")]
        public float expressionSpeed = 10f, eyeAlarmScale = 1.35f, wetSeconds = 3f;
        public Color hudPanel = new Color(.65f,.12f,.045f,.94f), hudAccent = new Color(1f,.62f,.08f);
        public float gaugeResponse = 8f;
        [Header("Audio levels")]
        [Range(0,1)] public float effectsVolume = .55f, voiceVolume = .7f, musicVolume = .12f;
        public float audioResponse = 5f, soundCooldown = .22f;
        [Header("Palette")]
        public Color wall = new Color(.32f, .48f, .39f);
        public Color wood = new Color(.55f, .25f, .10f);
        public Color fabric = new Color(.12f, .43f, .49f);
        public Color curtain = new Color(.9f, .24f, .12f);
        public Color foliage = new Color(.26f, .55f, .12f);
        [Header("Sunset")]
        public Color sun = new Color(1f, .69f, .38f);
        public Color fill = new Color(.55f, .71f, 1f);
        [Range(0, 5)] public float sunIntensity = 1.8f;
        [Range(0, 3)] public float fillIntensity = .65f;
        [Range(0, 10)] public float bounceIntensity = 4f;
        [Range(0, 2)] public float bloom = .45f;
        [Range(-100, 100)] public float saturation = 12;
        [Range(0, .6f)] public float vignette = .22f;
        [Header("Look transfer — apply with Tools/FlameBlame/Apply Look or F2 twice")]
        public Color lookKey = new Color(.99f, .90f, .71f);
        [Range(0, 5)] public float lookKeyIntensity = 1.8f;
        [Range(0, 1)] public float lookShadowStrength = .75f;
        public Color lookAmbientSky = new Color(.62f, .59f, .51f);
        public Color lookAmbientEquator = new Color(.36f, .34f, .32f);
        public Color lookAmbientGround = new Color(.20f, .22f, .28f);
        [Range(0, 2)] public float lookAmbientIntensity = 1f;
        [Range(-100, 100)] public float lookContrast = 22f, lookSaturation = 22f;
        [Range(-2, 2)] public float lookExposure = 0f;
        public Color lookShadows = new Color(.48f, .48f, .53f);
        public Color lookHighlights = new Color(.54f, .51f, .48f);
        [Range(-100, 100)] public float lookSplitBalance = -20f;
        [Range(0, 2)] public float lookBloom = .6f;
        [Range(0, .6f)] public float lookVignette = .26f;
        [Range(0, 1)] public float lookVignetteSmoothness = .4f;
        [Header("Particles — hard caps remain fixed")]
        public Color flame = new Color(1f, .36f, .025f);
        public Color core = new Color(1f, .9f, .35f);
        public Color smoke = new Color(.17f, .14f, .18f, .5f);
        [Range(.1f, 3)] public float flameSpeed = 1.1f;
        [Range(.1f, 2)] public float smokeSpeed = .45f;
        [Range(.1f, 2)] public float flameSize = .35f;
    }
}
