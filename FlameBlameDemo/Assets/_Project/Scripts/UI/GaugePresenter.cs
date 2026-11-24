using UnityEngine;

namespace FlameBlame.UI
{
    /// Pressure dial: needle and ring chase the pressure through an under-damped spring.
    public sealed class GaugePresenter : IHudPresenter
    {
        const float SweepDegrees = 240f;

        readonly HudServices services;
        readonly GaugeView view;
        Spring spring;
        float target = 1f, peak = 1f, cooldown, flash, clock;
        int shownPercent = -1;

        public GaugePresenter(HudServices services, GaugeView view)
        {
            this.services = services; this.view = view;
            spring.Value = 1f;
            // The ring is a full circle sprite: start its fill where the needle starts (120 deg left of up).
            view.ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, SweepDegrees * 0.5f);
            services.Events.PressureChanged += OnPressure;
        }

        void OnPressure(float pressure) { target = Mathf.Clamp(pressure, 0f, 1.2f); }

        public void Tick(float dt)
        {
            var m = services.Motion;
            clock += dt;
            cooldown -= dt;
            spring.Step(target, dt, m.gaugeFrequency, m.gaugeDamping);

            peak = Mathf.Max(target, peak - 0.6f * dt);
            if (peak - target > m.dropThreshold && cooldown <= 0f)
            {
                cooldown = m.dropCooldown;
                flash = 1f;
                services.Tween.Shake(view.root, m.dropShakePixels, 0.3f);
            }
            flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);

            float value = spring.Value;
            float shownValue = Mathf.Clamp(value, 0f, 1f);
            float tremor = target > 0.95f ? (Mathf.PerlinNoise(clock * 30f, 0.5f) - 0.5f) * 2f * m.highPressureTremor : 0f;
            float angle = Mathf.Clamp(Mathf.Lerp(SweepDegrees * 0.5f, -SweepDegrees * 0.5f, value) + tremor,
                -SweepDegrees * 0.5f - 10f, SweepDegrees * 0.5f + 10f);
            view.needle.localRotation = Quaternion.Euler(0f, 0f, angle);

            view.ring.fillAmount = shownValue * (SweepDegrees / 360f);
            view.ring.color = Color.Lerp(services.Palette.PressureColor(shownValue), Color.white, flash * 0.7f);

            int percent = Mathf.RoundToInt(shownValue * 100f);
            if (percent != shownPercent)
            {
                shownPercent = percent;
                view.percent.SetText("{0:0}%", percent);
            }
        }

        public void Dispose() { services.Events.PressureChanged -= OnPressure; }
    }
}
