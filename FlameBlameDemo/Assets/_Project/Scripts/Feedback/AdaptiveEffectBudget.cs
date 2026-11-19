using FlameBlame.Core;
using FlameBlame.Hose;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Samples unscaled frame time with hysteresis; only visual emission is reduced.
    public sealed class AdaptiveEffectBudget
    {
        readonly ChaosSettings settings;
        readonly EffectFactory effects;
        readonly JetController jet;
        readonly Transform viewer;
        float elapsed;
        int frames;
        public float Quality { get; private set; } = 1f;
        public AdaptiveEffectBudget(ChaosSettings settings, EffectFactory effects, JetController jet, Transform viewer)
        { this.settings = settings; this.effects = effects; this.jet = jet; this.viewer = viewer; }
        public void Tick(float unscaledDelta)
        {
            elapsed += unscaledDelta;
            frames++;
            if (elapsed < Mathf.Max(.25f, settings.qualitySampleSeconds)) return;
            float fps = frames / elapsed;
            float minimum = Mathf.Clamp(settings.minimumEffectQuality, .1f, 1f);
            if (fps < settings.lowFps) Quality = Mathf.Max(minimum, Quality - .15f);
            else if (fps > Mathf.Max(settings.lowFps + 1f, settings.recoverFps)) Quality = Mathf.Min(1f, Quality + .05f);
            elapsed = 0; frames = 0;
            effects.SetVisualBudget(Quality, viewer, Mathf.Max(1f, settings.effectCullDistance));
            jet.SetVisualQuality(Quality);
        }
    }
}
