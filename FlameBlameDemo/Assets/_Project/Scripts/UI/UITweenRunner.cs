using System;
using UnityEngine;
using UnityEngine.UI;

namespace FlameBlame.UI
{
    public enum Ease { Linear, OutCubic, OutBack, OutElastic, OutBounce, InOutSine }

    public static class Easing
    {
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                case Ease.OutElastic:
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.OutBounce:
                {
                    const float n1 = 7.5625f, d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                    if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                    t -= 2.625f / d1; return n1 * t * t + 0.984375f;
                }
                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                default:
                    return t;
            }
        }
    }

    /// Under-damped spring for gauge needles and counters. Sub-stepped so large frames stay stable.
    public struct Spring
    {
        public float Value;
        public float Velocity;

        public void Step(float target, float dt, float frequency, float dampingRatio)
        {
            if (float.IsNaN(target) || float.IsInfinity(target)) return;
            if (float.IsNaN(Value) || float.IsInfinity(Value) || float.IsNaN(Velocity) || float.IsInfinity(Velocity)) { Value = target; Velocity = 0f; }
            dt = Mathf.Clamp(dt, 0f, 0.05f);   // a long editor hitch must not blow up the integrator
            float w = 2f * Mathf.PI * frequency;
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt * 240f), 1, 40);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float accel = w * w * (target - Value) - 2f * dampingRatio * w * Velocity;
                Velocity += accel * h;
                Value += Velocity * h;
            }
        }
    }

    /// Allocation-free tween engine: structs in a fixed array, one Tick per frame, unscaled time.
    /// Starting a tween on a busy channel of the same target restarts it in place.
    public sealed class UITweenRunner
    {
        enum Kind : byte { Position, Scale, RotZ, Alpha, Color, Fill, Value, Punch, Shake }

        struct Tween
        {
            public bool Active;
            public Kind Kind;
            public long Key;
            public RectTransform Rect;
            public CanvasGroup Group;
            public Graphic Graphic;
            public Image Image;
            public Action<float> Setter;
            public Ease Ease;
            public float Delay, Time, Duration;
            public Vector3 From, To;
            public Color ColorFrom, ColorTo;
            public float Strength, Cycles, Seed;
        }

        readonly Tween[] tweens;
        int seedCounter;

        public UITweenRunner(int capacity = 128) { tweens = new Tween[capacity]; }

        // ---- channel ids: tweens that write the same property share one so a restart replaces the old one
        const int ChPosition = 1, ChScale = 2, ChRot = 3, ChAlpha = 4, ChColor = 5, ChFill = 6;

        public void Move(RectTransform rect, Vector2 from, Vector2 to, float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(rect, ChPosition), Kind.Position, duration, ease, delay);
            t.Rect = rect; t.From = from; t.To = to;
            rect.anchoredPosition = from;
        }

        public void Scale(RectTransform rect, Vector3 from, Vector3 to, float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(rect, ChScale), Kind.Scale, duration, ease, delay);
            t.Rect = rect; t.From = from; t.To = to;
            rect.localScale = from;
        }

        public void Rotate(RectTransform rect, float fromZ, float toZ, float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(rect, ChRot), Kind.RotZ, duration, ease, delay);
            t.Rect = rect; t.From = new Vector3(fromZ, 0f, 0f); t.To = new Vector3(toZ, 0f, 0f);
            rect.localRotation = Quaternion.Euler(0f, 0f, fromZ);
        }

        public void Fade(CanvasGroup group, float from, float to, float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(group, ChAlpha), Kind.Alpha, duration, ease, delay);
            t.Group = group; t.From = new Vector3(from, 0f, 0f); t.To = new Vector3(to, 0f, 0f);
            group.alpha = from;
        }

        public void Tint(Graphic graphic, Color from, Color to, float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(graphic, ChColor), Kind.Color, duration, ease, delay);
            t.Graphic = graphic; t.ColorFrom = from; t.ColorTo = to;
            graphic.color = from;
        }

        public void Fill(Image image, float from, float to, float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(image, ChFill), Kind.Fill, duration, ease, delay);
            t.Image = image; t.From = new Vector3(from, 0f, 0f); t.To = new Vector3(to, 0f, 0f);
            image.fillAmount = from;
        }

        /// Float tween through a cached setter delegate (counters, custom channels). channel is 0..7.
        public void Value(UnityEngine.Object owner, int channel, Action<float> setter, float from, float to,
            float duration, Ease ease, float delay = 0f)
        {
            ref var t = ref Acquire(Key(owner, 16 + (channel & 7)), Kind.Value, duration, ease, delay);
            t.Setter = setter; t.From = new Vector3(from, 0f, 0f); t.To = new Vector3(to, 0f, 0f);
            setter(from);
        }

        /// Decaying scale oscillation: scale = base * (1 + strength * sin * falloff).
        public void Punch(RectTransform rect, float strength, float duration, float cycles = 2f)
        {
            long key = Key(rect, ChScale);
            bool running = Find(key, out int index) && tweens[index].Kind == Kind.Punch;
            Vector3 baseScale = running ? tweens[index].From : rect.localScale;
            ref var t = ref Acquire(key, Kind.Punch, duration, Ease.Linear, 0f);
            t.Rect = rect; t.From = baseScale; t.Strength = strength; t.Cycles = cycles;
        }

        /// Noise shake of anchoredPosition that fades out; restores the original position.
        public void Shake(RectTransform rect, float pixels, float duration)
        {
            long key = Key(rect, ChPosition);
            bool running = Find(key, out int index) && tweens[index].Kind == Kind.Shake;
            Vector3 basePos = running ? tweens[index].From : (Vector3)rect.anchoredPosition;
            ref var t = ref Acquire(key, Kind.Shake, duration, Ease.Linear, 0f);
            t.Rect = rect; t.From = basePos; t.Strength = pixels; t.Seed = (seedCounter++ % 97) * 3.17f;
        }

        public void Cancel(UnityEngine.Object target)
        {
            long id = target.GetInstanceID();
            for (int i = 0; i < tweens.Length; i++)
                if (tweens[i].Active && (tweens[i].Key >> 5) == id) tweens[i].Active = false;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < tweens.Length; i++)
            {
                if (!tweens[i].Active) continue;
                ref var t = ref tweens[i];
                float step = dt;
                if (t.Delay > 0f)
                {
                    t.Delay -= dt;
                    if (t.Delay > 0f) continue;
                    step = -t.Delay;
                    t.Delay = 0f;
                }
                t.Time += step;
                float p = t.Duration <= 0f ? 1f : Mathf.Clamp01(t.Time / t.Duration);
                Apply(ref t, p);
                if (p >= 1f) { Finish(ref t); t.Active = false; }
            }
        }

        static void Apply(ref Tween t, float p)
        {
            float e = Easing.Evaluate(t.Ease, p);
            switch (t.Kind)
            {
                case Kind.Position: t.Rect.anchoredPosition = Vector2.LerpUnclamped(t.From, t.To, e); break;
                case Kind.Scale: t.Rect.localScale = Vector3.LerpUnclamped(t.From, t.To, e); break;
                case Kind.RotZ: t.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(t.From.x, t.To.x, e)); break;
                case Kind.Alpha: t.Group.alpha = Mathf.LerpUnclamped(t.From.x, t.To.x, e); break;
                case Kind.Color: t.Graphic.color = Color.LerpUnclamped(t.ColorFrom, t.ColorTo, e); break;
                case Kind.Fill: t.Image.fillAmount = Mathf.LerpUnclamped(t.From.x, t.To.x, e); break;
                case Kind.Value: t.Setter(Mathf.LerpUnclamped(t.From.x, t.To.x, e)); break;
                case Kind.Punch:
                    t.Rect.localScale = t.From * (1f + t.Strength * Mathf.Sin(p * t.Cycles * 2f * Mathf.PI) * (1f - p));
                    break;
                case Kind.Shake:
                {
                    float fade = 1f - p, n = t.Seed + t.Time * 45f;
                    Vector2 offset = new Vector2(Mathf.PerlinNoise(n, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, n) - 0.5f) * (2f * t.Strength * fade);
                    t.Rect.anchoredPosition = (Vector2)t.From + offset;
                    break;
                }
            }
        }

        static void Finish(ref Tween t)
        {
            if (t.Kind == Kind.Punch) t.Rect.localScale = t.From;
            else if (t.Kind == Kind.Shake) t.Rect.anchoredPosition = t.From;
        }

        // key = instanceId << 5 | channel (channel < 32)
        static long Key(UnityEngine.Object target, int channel) => ((long)target.GetInstanceID() << 5) | (long)(channel & 31);

        bool Find(long key, out int index)
        {
            for (int i = 0; i < tweens.Length; i++)
                if (tweens[i].Active && tweens[i].Key == key) { index = i; return true; }
            index = -1;
            return false;
        }

        ref Tween Acquire(long key, Kind kind, float duration, Ease ease, float delay)
        {
            int index;
            if (!Find(key, out index))
            {
                index = -1;
                for (int i = 0; i < tweens.Length; i++) if (!tweens[i].Active) { index = i; break; }
                if (index < 0) index = 0;   // pool exhausted: recycle the oldest slot rather than allocate
            }
            ref var t = ref tweens[index];
            t.Active = true; t.Key = key; t.Kind = kind; t.Ease = ease;
            t.Delay = delay; t.Time = 0f; t.Duration = duration;
            t.Rect = null; t.Group = null; t.Graphic = null; t.Image = null; t.Setter = null;
            return ref t;
        }
    }
}
