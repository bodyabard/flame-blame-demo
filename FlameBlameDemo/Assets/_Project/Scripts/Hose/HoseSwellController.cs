using UnityEngine;
using FlameBlame.Core;
namespace FlameBlame.Hose
{
    public sealed class HoseSwellController : MonoBehaviour
    {
        IPressureSource source;
        ScaleProfile tuning;
        float[] amounts, velocities;
        bool flowing;
        float front;
        public void Initialize(IPressureSource pressure, ScaleProfile profile, int rings)
        {
            source=pressure; tuning=profile; amounts=new float[rings]; velocities=new float[rings];
        }
        public void Tick(bool water, float dt)
        {
            if(water!=flowing) {flowing=water; front=0f;}
            front+=Mathf.Max(.01f,tuning.hoseWaveSpeed)*dt;
        }
        public float Radius(int ring, float distance)
        {
            float target=flowing?Mathf.Clamp01(source.Pressure):0f;
            if(distance>front) target=amounts[ring];
            amounts[ring]=Mathf.SmoothDamp(amounts[ring],target,ref velocities[ring],Mathf.Max(.01f,tuning.hoseSwellTime));
            float vibration=Mathf.Sin(Time.time*47f-distance*13f)*tuning.hoseVibration*amounts[ring];
            return tuning.tubeRadius*(Mathf.Lerp(tuning.hoseDryScale,tuning.hoseWetScale,amounts[ring])+vibration);
        }
    }
}
