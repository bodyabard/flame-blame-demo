using FlameBlame.Electric;
using UnityEngine;
using UnityEngine.Pool;

namespace FlameBlame.Feedback
{
    public sealed class PuddleFactory
    {
        sealed class Puddle : IElectrifiable
        {
            public Transform View;
            public Renderer Renderer;
            public MaterialPropertyBlock Block = new MaterialPropertyBlock();
            public float Expires, Diameter;
            public Vector3 Position => View.position;
            public float Radius => View.localScale.x * 0.5f;
            public bool Energized { get; set; }
        }
        readonly ObjectPool<Puddle> pool;
        readonly Puddle[] active;
        readonly ElectricitySystem electricity;
        readonly float lifetime, thickness;
        readonly int baseColorId = Shader.PropertyToID("_BaseColor");
        int count;

        public PuddleFactory(Transform root, Transform template, ElectricitySystem electricity,
            int capacity = 24, float lifetime = 22f)
        {
            this.electricity = electricity;
            this.lifetime = Mathf.Max(1f, lifetime);
            thickness = template.localScale.y;
            capacity = Mathf.Clamp(capacity, 1, 48);
            active = new Puddle[capacity];
            template.gameObject.SetActive(false);
            pool = new ObjectPool<Puddle>(() =>
            {
                var view = Object.Instantiate(template, root);
                return new Puddle { View = view, Renderer = view.GetComponent<Renderer>() };
            }, p => p.View.gameObject.SetActive(true), p => p.View.gameObject.SetActive(false),
                p => Object.Destroy(p.View.gameObject), false, capacity, capacity);
            for (int i = 0; i < capacity; i++) active[i] = pool.Get();
            for (int i = 0; i < capacity; i++) { pool.Release(active[i]); active[i] = null; }
        }
        public void Spawn(Vector3 position, float diameter)
        {
            for (int i = 0; i < count; i++)
                if (ElectricitySystem.Touches(position, 0f, active[i].Position, active[i].Radius * 0.65f))
                {
                    active[i].Expires = Time.time + lifetime;
                    active[i].Diameter = Mathf.Max(active[i].Diameter, diameter);
                    return;
                }
            if (count == active.Length)
            {
                int oldest = 0;
                for (int i = 1; i < count; i++) if (active[i].Expires < active[oldest].Expires) oldest = i;
                Release(oldest);
            }
            var puddle = pool.Get();
            puddle.Energized = false;
            puddle.Diameter = diameter;
            puddle.Expires = Time.time + lifetime;
            puddle.View.SetPositionAndRotation(position + Vector3.up * (0.012f + count * 0.0004f), Quaternion.identity);
            puddle.View.localScale = new Vector3(diameter, thickness, diameter);
            active[count++] = puddle;
            electricity.Register(puddle);
        }
        public void Tick(float now)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (now >= p.Expires) { Release(i); continue; }
                float shrink = Mathf.Clamp01((p.Expires - now) / Mathf.Min(4f, lifetime));
                float diameter = p.Diameter * shrink;
                p.View.localScale = new Vector3(diameter, thickness, diameter);
                p.Block.SetColor(baseColorId, p.Energized
                    ? new Color(1f, 0.75f, 0.08f, 0.8f) : new Color(0.25f, 0.5f, 0.85f, 0.55f));
                p.Renderer.SetPropertyBlock(p.Block);
            }
        }
        void Release(int index)
        {
            var puddle = active[index];
            electricity.Unregister(puddle);
            puddle.Energized = false;
            pool.Release(puddle);
            active[index] = active[--count];
            active[count] = null;
        }
    }
}
