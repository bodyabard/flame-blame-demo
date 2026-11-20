using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Prop whose pivot sits on the surface it rests on, so uniform scaling keeps it grounded.
    public sealed class ScaledProp : MonoBehaviour
    {
        [SerializeField] Vector3 baseScale = Vector3.one;
        public void Capture() { baseScale = transform.localScale; }
        public void Apply(float multiplier) { transform.localScale = baseScale * multiplier; }
    }
}
