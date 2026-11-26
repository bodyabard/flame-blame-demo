using System;
using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Damage plate: rolling counter, punch, coin spin, colour flash; big hits also shake the plate.
    public sealed class DamagePresenter : IHudPresenter
    {
        readonly HudServices services;
        readonly PanelWidget view;
        readonly DamageLedger ledger;
        readonly Action<float> setCounter, setCoin;
        float shown;
        int target;

        public DamagePresenter(HudServices services, PanelWidget view, DamageLedger ledger)
        {
            this.services = services; this.view = view; this.ledger = ledger;
            setCounter = SetCounter;
            setCoin = SetCoin;
            view.root.localRotation = Quaternion.Euler(0f, 0f, view.restRotation);
            SetCounter(ledger.Total);
            target = ledger.Total;
            ledger.Changed += OnTotal;
        }

        void SetCounter(float value)
        {
            shown = value;
            view.label.SetText("${0:0}", value);
        }

        void SetCoin(float t) { view.icon.localScale = new Vector3(Mathf.Cos(t * 2f * Mathf.PI), 1f, 1f); }

        void OnTotal(int total)
        {
            var m = services.Motion;
            int delta = total - target;
            target = total;
            bool big = delta >= m.bigHitThreshold;

            services.Tween.Value(view.label, 0, setCounter, shown, total, m.counterDuration, Ease.OutCubic);
            services.Tween.Punch(view.root, big ? m.bigPunch : m.punch, m.punchDuration);
            services.Tween.Value(view.icon, 1, setCoin, 0f, 1f, m.coinSpinDuration, Ease.OutCubic);
            var flash = services.Palette.accent;
            services.Tween.Tint(view.panel.Outline, new Color(flash.r, flash.g, flash.b, big ? 1f : 0.7f), new Color(flash.r, flash.g, flash.b, 0f), m.flashDuration, Ease.OutCubic);
            if (big) services.Tween.Shake(view.root, m.bigShakePixels, 0.35f);
        }

        public void Tick(float dt) { }
        public void Dispose() { ledger.Changed -= OnTotal; }
    }
}
