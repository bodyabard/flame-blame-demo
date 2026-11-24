using UnityEngine;

namespace FlameBlame.UI
{
    /// Every duration, strength and easing used by HUD motion. Tune in the inspector, no code.
    [CreateAssetMenu(menuName = "FlameBlame/UI Motion Presets", fileName = "UIMotionPresets")]
    public sealed class UIMotionPresets : ScriptableObject
    {
        [Header("Round start")]
        public float bannerIn = 0.35f;
        public float bannerHold = 1.2f;
        public float bannerOut = 0.25f;
        public float ruleBannerDelay = 1.8f;
        public float ruleBannerHold = 3.5f;
        public float slideIn = 0.4f;
        public float slideStagger = 0.06f;
        public Ease slideEase = Ease.OutBack;
        public Ease bannerEase = Ease.OutBack;

        [Header("Damage")]
        public float counterDuration = 0.4f;
        public float punch = 0.12f;
        public float bigPunch = 0.24f;
        public float punchDuration = 0.35f;
        public int bigHitThreshold = 400;
        public float bigShakePixels = 9f;
        public float coinSpinDuration = 0.45f;
        public float flashDuration = 0.4f;

        [Header("Popups")]
        public float popupIn = 0.25f;
        public float popupSeconds = 0.9f;
        public float popupRise = 60f;
        public float burstSpin = 70f;
        public Ease popupEase = Ease.OutBack;

        [Header("Timer")]
        public float tickScale = 0.04f;
        public float pulseScale = 0.1f;
        public int pulseSeconds = 15;
        public int shakeSeconds = 5;
        public float timerShakePixels = 4f;

        [Header("Pressure gauge")]
        public float gaugeFrequency = 3.2f;
        [Range(0.1f, 1f)] public float gaugeDamping = 0.38f;
        public float dropThreshold = 0.35f;
        public float dropCooldown = 1f;
        public float dropShakePixels = 7f;
        public float highPressureTremor = 0.8f;

        [Header("Mode switch")]
        public float flipDuration = 0.25f;
        public float modeBannerHold = 0.8f;
        public float modePunch = 0.18f;

        [Header("Hints")]
        public float hintFade = 0.15f;
        public float hintStagger = 0.05f;
        public float promptIn = 0.2f;
        public float breatheScale = 0.03f;
        public float breathePeriod = 1.2f;
        public float squash = 0.85f;
        public float squashDuration = 0.18f;

        [Header("Shock / impact")]
        public float shockFlash = 0.75f;
        public float shockFlashSeconds = 0.6f;
        public float shockShakeSeconds = 0.3f;
        public float shockShakePixels = 14f;
        public float impactShakeSeconds = 0.25f;
        public float impactShakePixels = 6f;

        [Header("Result screen")]
        public float resultDim = 0.3f;
        public float dimAlpha = 0.7f;
        public float panelDrop = 0.75f;
        public float rowDelay = 0.35f;
        public float rowFirstDelay = 0.8f;
        public float rowCount = 0.6f;
        public float hoverScale = 1.06f;
        public Ease dropEase = Ease.OutBounce;
    }
}
