using System;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Mode pill: the icon flips edge-on, swaps, flips back; the pill punches and a short banner names the mode.
    public sealed class ModePresenter : IHudPresenter
    {
        readonly HudServices services;
        readonly ModeView view;
        readonly BannerPresenter banner;
        readonly Action<float> flip;
        bool pendingJet, swapped;

        public ModePresenter(HudServices services, ModeView view, BannerPresenter banner)
        {
            this.services = services; this.view = view; this.banner = banner;
            flip = Flip;
            Apply(false);
            services.Events.NozzleModeChanged += OnModeChanged;
        }

        void Apply(bool jet)
        {
            view.icon.sprite = jet ? view.jetSprite : view.waterSprite;
            view.icon.color = services.Palette.outline;
            view.pill.SetFill(jet ? services.Palette.jet : services.Palette.water);
            view.label.SetText(jet ? "ТЯГА" : "ГАСІННЯ");
        }

        void Flip(float t)
        {
            view.icon.rectTransform.localScale = new Vector3(Mathf.Abs(1f - 2f * t), 1f, 1f);
            if (t >= 0.5f && !swapped) { swapped = true; Apply(pendingJet); }
        }

        void OnModeChanged(bool jet)
        {
            var m = services.Motion;
            pendingJet = jet;
            swapped = false;
            services.Tween.Value(view.icon, 2, flip, 0f, 1f, m.flipDuration, Ease.Linear);
            services.Tween.Punch(view.root, m.modePunch, 0.35f);
            banner.Show(jet ? "РЕЖИМ ТЯГИ" : "РЕЖИМ ГАСІННЯ", m.modeBannerHold, true);
        }

        public void Tick(float dt) { }
        public void Dispose() { services.Events.NozzleModeChanged -= OnModeChanged; }
    }
}
