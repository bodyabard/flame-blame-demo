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
        struct Visual
        {
            public ParticleSystem System;
            public ParticleSystem Root;
            public ParticleSystemRenderer Renderer;
            public float Rate;
        }
        readonly List<Visual> visuals = new List<Visual>(128);
        Transform viewer;
        float cullDistanceSquared = float.PositiveInfinity;
        float currentQuality = 1f;

        public EffectFactory(Transform root, ParticleSystem flame, ParticleSystem steam, ParticleSystem splash,
            ParticleSystem spark = null, int flameMax = 16, int steamMax = 6, int splashMax = 8, int sparkMax = 4)
        {
            flameMax = Mathf.Clamp(flameMax, 1, 16);
            steamMax = Mathf.Clamp(steamMax, 1, 6);
            splashMax = Mathf.Clamp(splashMax, 1, 8);
            sparkMax = Mathf.Clamp(sparkMax, 1, 4);
            pools[(int)EffectKind.Flame] = MakePool(root, flame, flameMax, 36);
            pools[(int)EffectKind.Steam] = MakePool(root, steam, steamMax, 12);
            pools[(int)EffectKind.Splash] = MakePool(root, splash, splashMax, 10);
            if (spark != null) pools[(int)EffectKind.Spark] = MakePool(root, spark, sparkMax, 8);
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

        ObjectPool<ParticleSystem> MakePool(Transform root, ParticleSystem template, int max, int particleCap)
        {
            template.gameObject.SetActive(false);
            var pool = new ObjectPool<ParticleSystem>(
                () =>
                {
                    var clone = Object.Instantiate(template, root);
                    int remaining = particleCap;
                    foreach (var system in clone.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = system.main;
                        int count = Mathf.Min(main.maxParticles, remaining);
                        main.maxParticles = count; remaining -= count;
                        visuals.Add(new Visual { System = system, Root = clone, Renderer = system.GetComponent<ParticleSystemRenderer>(), Rate = system.emission.rateOverTimeMultiplier });
                    }
                    return clone;
                },
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
            for (int i = 0; i < visuals.Count; i++)
            {
                var visual = visuals[i];
                if (visual.Root != ps) continue;
                bool near = viewer == null || (position - viewer.position).sqrMagnitude <= cullDistanceSquared;
                var emission = visual.System.emission;
                emission.rateOverTimeMultiplier = visual.Rate * (near ? currentQuality : 0f);
                if (visual.Renderer != null) visual.Renderer.enabled = near;
            }
            ps.Play(true);
            return ps;
        }

        /// Spawn a one-shot effect that returns itself to the pool.
        public void SpawnOneShot(EffectKind kind, Vector3 position, float scale = 1f)
        {
            if (viewer != null && (position - viewer.position).sqrMagnitude > cullDistanceSquared) return;
            var ps = Spawn(kind, position, scale);
            if (ps == null) return;
            timed.Add(new Timed { Ps = ps, Kind = kind, ReleaseAt = Time.time + lifespans[(int)kind] });
        }

        public void Release(EffectKind kind, ParticleSystem ps) { if (ps != null) pools[(int)kind].Release(ps); }

        public void SetVisualBudget(float quality, Transform camera, float distance)
        {
            viewer = camera; cullDistanceSquared = distance * distance;
            currentQuality = Mathf.Clamp01(quality);
            for (int i = 0; i < visuals.Count; i++)
            {
                var visual = visuals[i];
                if (visual.System == null || !visual.System.gameObject.activeInHierarchy) continue;
                bool near = camera == null || (visual.System.transform.position - camera.position).sqrMagnitude <= cullDistanceSquared;
                var emission = visual.System.emission;
                emission.rateOverTimeMultiplier = visual.Rate * (near ? quality : 0f);
                if (visual.Renderer != null) visual.Renderer.enabled = near;
            }
        }

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
