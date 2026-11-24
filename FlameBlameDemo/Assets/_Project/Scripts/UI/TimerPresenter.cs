using UnityEngine;

namespace FlameBlame.UI
{
    /// Timer plate: a tick every second, danger colour and pulse in the last seconds, shake in the final ones.
    public sealed class TimerPresenter : IHudPresenter
    {
        readonly HudServices services;
        readonly PanelWidget view;
        int lastSecond = -1;

        public TimerPresenter(HudServices services, PanelWidget view, int roundSeconds)
        {
            this.services = services; this.view = view;
            view.root.localRotation = Quaternion.Euler(0f, 0f, view.restRotation);
            view.label.SetText("{0:0} с", roundSeconds);
            services.Events.TimerTick += OnTick;
        }

        void OnTick(int seconds)
        {
            if (seconds == lastSecond) return;
            lastSecond = seconds;
            view.label.SetText("{0:0} с", seconds);

            var m = services.Motion;
            bool urgent = seconds <= m.pulseSeconds;
            if (urgent) view.panel.SetFill(services.Palette.danger);
            float scale = urgent ? m.pulseScale : m.tickScale;
            services.Tween.Scale(view.root, Vector3.one * (1f + scale), Vector3.one, 0.2f, Ease.OutCubic);
            if (seconds <= m.shakeSeconds) services.Tween.Shake(view.root, m.timerShakePixels, 0.3f);
        }

        public void Tick(float dt) { }
        public void Dispose() { services.Events.TimerTick -= OnTick; }
    }
}
