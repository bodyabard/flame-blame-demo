using UnityEngine;

namespace FlameBlame.Hose
{
    /// Pressure model: kinks choke the flow, value is smoothed over time.
    public sealed class HosePressure : IPressureSource
    {
        public float BasePressure = 1f;
        public float MinFactor = 0.15f;
        public float KinksForFullChoke = 3f;
        public float Smoothing = 8f;

        public float Pressure { get; private set; } = 1f;

        public static float TargetPressure(float basePressure, int kinks, float minFactor, float kinksForFullChoke)
        {
            float choke = Mathf.Clamp01(kinks / Mathf.Max(kinksForFullChoke, 0.0001f));
            return basePressure * Mathf.Lerp(1f, minFactor, choke);
        }

        public void Tick(int kinks, float dt)
        {
            float target = TargetPressure(BasePressure, kinks, MinFactor, KinksForFullChoke);
            Pressure = Mathf.Lerp(Pressure, target, Smoothing * dt);
        }
    }
}
