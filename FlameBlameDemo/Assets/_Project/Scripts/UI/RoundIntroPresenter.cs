using UnityEngine;

namespace FlameBlame.UI
{
    /// Choreography of the round start: banner, HUD plates sliding in from the screen edges, the one-time rules banner.
    public sealed class RoundIntroPresenter : IHudPresenter
    {
        readonly HudServices services;
        readonly BannerPresenter banner;
        readonly PanelWidget damage, timer;
        readonly GaugeView gauge;
        readonly ModeView mode;
        readonly HintsView hints;

        public RoundIntroPresenter(HudServices services, BannerPresenter banner, PanelWidget damage, PanelWidget timer,
            GaugeView gauge, ModeView mode, HintsView hints)
        {
            this.services = services; this.banner = banner;
            this.damage = damage; this.timer = timer; this.gauge = gauge; this.mode = mode; this.hints = hints;

            // Damage and timer wait off-screen for the round; the flight controls are on-screen from the start.
            damage.root.anchoredPosition = damage.restPosition + damage.hiddenOffset;
            timer.root.anchoredPosition = timer.restPosition + timer.hiddenOffset;
            SlideIn(gauge.root, gauge.restPosition, gauge.hiddenOffset, 0f);
            SlideIn(mode.root, mode.restPosition, mode.hiddenOffset, services.Motion.slideStagger);
            SlideIn(hints.root, hints.restPosition, hints.hiddenOffset, services.Motion.slideStagger * 2f);

            banner.Locked = true;
            banner.Show(Object.FindFirstObjectByType<FlameBlame.Core.LevelScenario>()!=null ? "Візьми шланг з машини!" : "Злети до вікна!", -1f);
            services.Events.RoundStarted += OnRoundStarted;
        }

        void SlideIn(RectTransform rect, Vector2 rest, Vector2 hidden, float delay)
        {
            services.Tween.Move(rect, rest + hidden, rest, services.Motion.slideIn, services.Motion.slideEase, delay);
        }

        void OnRoundStarted(int seconds)
        {
            var m = services.Motion;
            banner.Locked = false;
            banner.ShowNumber("ПОЖЕЖА! {0} с", seconds, m.bannerHold);
            SlideIn(damage.root, damage.restPosition, damage.hiddenOffset, 0.1f);
            SlideIn(timer.root, timer.restPosition, timer.hiddenOffset, 0.1f + m.slideStagger);
            // The electricity rule is told once, right after the fire banner, instead of living on screen.
            banner.Schedule(m.bannerIn + m.bannerHold + m.bannerOut + 0.1f, "Вода + кабель = електротерапія. Жовті калюжі небезпечні!", m.ruleBannerHold);
        }

        public void Tick(float dt) { }
        public void Dispose() { services.Events.RoundStarted -= OnRoundStarted; }
    }
}
