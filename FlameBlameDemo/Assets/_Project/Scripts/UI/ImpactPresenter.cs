using UnityEngine;

namespace FlameBlame.UI
{
    /// Electric shock: yellow vignette plus whole-HUD shake. Ladder falls: a lighter HUD shake.
    public sealed class ImpactPresenter : IHudPresenter
    {
        readonly HudServices services;
        readonly CanvasGroup vignette;
        readonly RectTransform shakeRoot;

        public ImpactPresenter(HudServices services, CanvasGroup vignette, RectTransform shakeRoot)
        {
            this.services = services; this.vignette = vignette; this.shakeRoot = shakeRoot;
            vignette.alpha = 0f;
            services.Events.PlayerShocked += OnShock;
            services.Events.HardImpact += OnImpact;
        }

        void OnShock()
        {
            var m = services.Motion;
            services.Tween.Fade(vignette, m.shockFlash, 0f, m.shockFlashSeconds, Ease.OutCubic);
            services.Tween.Shake(shakeRoot, m.shockShakePixels, m.shockShakeSeconds);
        }

        void OnImpact(float amount)
        {
            var m = services.Motion;
            services.Tween.Shake(shakeRoot, m.impactShakePixels * Mathf.Clamp01(amount) * 2f, m.impactShakeSeconds);
        }

        public void Tick(float dt) { }

        public void Dispose()
        {
            services.Events.PlayerShocked -= OnShock;
            services.Events.HardImpact -= OnImpact;
        }
    }
}
