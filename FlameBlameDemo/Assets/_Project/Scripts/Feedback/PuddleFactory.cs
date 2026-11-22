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
            public float Expires, Diameter, Born, VisualDiameter, Variation;
            public float GameplayDiameter;
            public Vector3 Position => View.position;
            public float Radius => GameplayDiameter * 0.5f;
            public bool Energized { get; set; }
        }
        readonly ObjectPool<Puddle> pool;
        readonly Puddle[] active;
        readonly ElectricitySystem electricity;
        readonly float lifetime;
        int sequence;
        readonly int fillId = Shader.PropertyToID("_Fill");
        readonly int electrifiedId = Shader.PropertyToID("_Electrified");

        int count;

        public PuddleFactory(Transform root, Transform template, ElectricitySystem electricity,
            int capacity = 24, float lifetime = 22f)
        {
            this.electricity = electricity;
            this.lifetime = Mathf.Max(1f, lifetime);

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
            puddle.Born = Time.time;
            puddle.Variation = Random.Range(.75f,1.25f);
            puddle.VisualDiameter = diameter;
            puddle.GameplayDiameter = diameter;
            puddle.Block.Clear();
            puddle.Block.SetFloat(Shader.PropertyToID("_Seed"), Random.value*100f);
            puddle.Block.SetFloat(Shader.PropertyToID("_MaskIndex"), Random.Range(0,4));
            puddle.Block.SetFloat(fillId, 0);
            puddle.Block.SetFloat(electrifiedId, 0);
            puddle.Renderer.SetPropertyBlock(puddle.Block);
            puddle.View.SetPositionAndRotation(position + Vector3.up * (0.012f + (sequence++ % 12) * 0.0007f), Quaternion.Euler(90f, Random.Range(0f,360f),0f));
            puddle.View.localScale = new Vector3(diameter, diameter, 1f);
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
                p.GameplayDiameter = diameter;
                p.VisualDiameter = Mathf.Lerp(p.VisualDiameter,p.Diameter,1f-Mathf.Exp(-Time.deltaTime*8f));
                float age = Mathf.Clamp01((now-p.Born)/.45f);
                float visual = p.VisualDiameter*p.Variation*(1f+Mathf.Sin(age*Mathf.PI)*.08f);
                p.View.localScale = new Vector3(visual,visual,1f);
                p.Block.SetFloat(fillId, Mathf.SmoothStep(0f,1f,age)*shrink);
                p.Block.SetFloat(electrifiedId,p.Energized?1f:0f);

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
