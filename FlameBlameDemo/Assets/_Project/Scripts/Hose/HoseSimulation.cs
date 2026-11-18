using UnityEngine;

namespace FlameBlame.Hose
{
    /// Pure Verlet chain. No MonoBehaviour, no rendering, no Physics (collision is injected).
    public sealed class HoseSimulation
    {
        public int Segments { get; }
        public float SegmentLength { get; }
        public int Iterations = 10;
        public float Gravity = -12f;
        public float Damping = 0.985f;
        public float Radius = 0.06f;
        /// Dot product below which two neighbouring segments count as a kink.
        public float KinkDotThreshold = 0.15f;

        readonly Vector3[] p;
        readonly Vector3[] prev;
        readonly IHoseCollisionResolver collision;

        public Vector3[] Points => p;
        public Vector3 EndPosition => p[Segments - 1];
        public Vector3 EndDirection => (p[Segments - 1] - p[Segments - 2]).normalized;
        public float MaxReach => (Segments - 1) * SegmentLength * 0.92f;

        public HoseSimulation(int segments, float segmentLength, IHoseCollisionResolver collision = null)
        {
            Segments = Mathf.Max(3, segments);
            SegmentLength = segmentLength;
            this.collision = collision;
            p = new Vector3[Segments];
            prev = new Vector3[Segments];
        }

        public void Reset(Vector3 start, Vector3 direction)
        {
            for (int i = 0; i < Segments; i++)
            {
                p[i] = start + direction * SegmentLength * i;
                prev[i] = p[i];
            }
        }

        /// endAcceleration is applied to the free end (whip), m/s^2.
        public void Step(float dt, Vector3 anchor, Vector3 hand, bool endPinned, Vector3 endAcceleration)
        {
            int n = Segments;

            for (int i = 1; i < n; i++)
            {
                Vector3 v = (p[i] - prev[i]) * Damping;
                prev[i] = p[i];
                p[i] += v + Vector3.up * Gravity * dt * dt;
            }

            if (!endPinned) p[n - 1] += endAcceleration * dt * dt;

            for (int it = 0; it < Iterations; it++)
            {
                p[0] = anchor;
                if (endPinned) p[n - 1] = hand;

                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 delta = p[i + 1] - p[i];
                    float d = delta.magnitude;
                    if (d < 1e-5f) continue;
                    Vector3 corr = delta * ((d - SegmentLength) / d);
                    bool pinA = i == 0;
                    bool pinB = endPinned && i + 1 == n - 1;
                    if (pinA && pinB) continue;
                    if (pinA) p[i + 1] -= corr;
                    else if (pinB) p[i] += corr;
                    else { p[i] += corr * 0.5f; p[i + 1] -= corr * 0.5f; }
                }

                if (collision != null)
                    for (int i = 1; i < n; i++) p[i] = collision.Resolve(p[i], Radius);
            }
        }

        public int CountKinks()
        {
            int kinks = 0;
            for (int i = 1; i < Segments - 1; i++)
            {
                Vector3 a = (p[i] - p[i - 1]).normalized;
                Vector3 b = (p[i + 1] - p[i]).normalized;
                if (Vector3.Dot(a, b) < KinkDotThreshold) kinks++;
            }
            return kinks;
        }
    }
}
