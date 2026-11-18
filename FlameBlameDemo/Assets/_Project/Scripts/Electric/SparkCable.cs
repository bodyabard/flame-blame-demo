using FlameBlame.Feedback;
using UnityEngine;

namespace FlameBlame.Electric
{
    public class SparkCable : MonoBehaviour
    {
        [SerializeField] float contactRadius = 0.6f;
        [SerializeField] float sparkInterval = 0.45f;
        float nextSpark;
        public float Radius => contactRadius;
        public void Tick(float now, EffectFactory effects)
        {
            if (!isActiveAndEnabled || now < nextSpark) return;
            nextSpark = now + sparkInterval;
            effects.SpawnOneShot(EffectKind.Spark, transform.position + Vector3.up * 0.08f);
        }
    }
}
