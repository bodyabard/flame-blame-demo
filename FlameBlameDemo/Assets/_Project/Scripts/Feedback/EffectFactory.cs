using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace FlameBlame.Feedback
{
    public enum EffectKind { Flame = 0, Steam = 1, Splash = 2, Spark = 3 }

    /// Factory + Object Pool for particle effects. Templates are inactive scene objects; no Instantiate in hot paths.
    public sealed class EffectFactory
    {
        struct Timed
        {
            public ParticleSystem Ps;
            public EffectKind Kind;
            public float ReleaseAt;
        }

        const int PoolCount = 4;

        readonly ObjectPool<ParticleSystem>[] pools = new ObjectPool<ParticleSystem>[PoolCount];
        readonly float[] lifespans = new float[PoolCount];
        readonly List<Timed> timed = new List<Timed>(64);

        public EffectFactory(Transform root, ParticleSystem flame, ParticleSystem steam, ParticleSystem splash,
            ParticleSystem spark = null, int flameMax = 16, int steamMax = 6, int splashMax = 8, int sparkMax = 4)
        {
            flameMax = Mathf.Clamp(flameMax, 1, 16);
            steamMax = Mathf.Clamp(steamMax, 1, 6);
            splashMax = Mathf.Clamp(splashMax, 1, 8);
            sparkMax = Mathf.Clamp(sparkMax, 1, 4);
            pools[(int)EffectKind.Flame] = MakePool(root, flame, flameMax);
            pools[(int)EffectKind.Steam] = MakePool(root, steam, steamMax);
            pools[(int)EffectKind.Splash] = MakePool(root, splash, splashMax);
            if (spark != null) pools[(int)EffectKind.Spark] = MakePool(root, spark, sparkMax);
            lifespans[(int)EffectKind.Flame] = 0f;
            lifespans[(int)EffectKind.Steam] = Lifespan(steam);
            lifespans[(int)EffectKind.Splash] = Lifespan(splash);
            if (spark != null) lifespans[(int)EffectKind.Spark] = Lifespan(spark);
        }

        static float Lifespan(ParticleSystem ps)
        {
            var main = ps.main;
            return main.duration + main.startLifetime.constantMax;
        }

        static ObjectPool<ParticleSystem> MakePool(Transform root, ParticleSystem template, int max)
        {
            template.gameObject.SetActive(false);
            var pool = new ObjectPool<ParticleSystem>(
                () => Object.Instantiate(template, root),
                ps => ps.gameObject.SetActive(true),
                ps =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                },
                ps => Object.Destroy(ps.gameObject),
                false, max, max);

            // Prewarm so instantiation does not happen during play.
            var warm = new ParticleSystem[max];
            for (int i = 0; i < max; i++) warm[i] = pool.Get();
            for (int i = 0; i < max; i++) pool.Release(warm[i]);
            return pool;
        }

        /// Spawn a looping effect (flame). Caller must Release it.
        public ParticleSystem Spawn(EffectKind kind, Vector3 position, float scale = 1f)
        {
            if (pools[(int)kind] == null || pools[(int)kind].CountInactive == 0) return null;
            var ps = pools[(int)kind].Get();
            ps.transform.SetPositionAndRotation(position, Quaternion.identity);
            ps.transform.localScale = Vector3.one * scale;
            ps.Play(true);
            return ps;
        }

        /// Spawn a one-shot effect that returns itself to the pool.
        public void SpawnOneShot(EffectKind kind, Vector3 position, float scale = 1f)
        {
            var ps = Spawn(kind, position, scale);
            if (ps == null) return;
            timed.Add(new Timed { Ps = ps, Kind = kind, ReleaseAt = Time.time + lifespans[(int)kind] });
        }

        public void Release(EffectKind kind, ParticleSystem ps) { if (ps != null) pools[(int)kind].Release(ps); }

        public void Tick(float now)
        {
            for (int i = timed.Count - 1; i >= 0; i--)
            {
                if (now < timed[i].ReleaseAt) continue;
                pools[(int)timed[i].Kind].Release(timed[i].Ps);
                timed[i] = timed[timed.Count - 1];
                timed.RemoveAt(timed.Count - 1);
            }
        }
    }
}
