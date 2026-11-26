using System;
using System.Collections.Generic;
using FlameBlame.Core;
using FlameBlame.Fire;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Observer: turns game events into pooled VFX (flames, steam, splashes, puddles).
    public sealed class EffectsPresenter : IDisposable
    {
        readonly IGameEvents events;
        readonly EffectFactory effects;
        readonly PuddleFactory puddles;
        readonly Dictionary<FireNode, ParticleSystem> flames = new Dictionary<FireNode, ParticleSystem>(32);
        readonly float splashInterval;
        float nextSplashTime;

        public EffectsPresenter(IGameEvents events, EffectFactory effects, PuddleFactory puddles, float splashInterval = 0.08f)
        {
            this.events = events;
            this.effects = effects;
            this.puddles = puddles;
            this.splashInterval = splashInterval;

            events.FireIgnited += OnIgnited;
            events.FireExtinguished += OnExtinguished;
            events.FireBurnedOut += OnBurnedOut;
            events.SteamCreated += OnSteam;
            events.WaterImpact += OnWaterImpact;
            events.PuddleCreated += OnPuddle;
            events.NpcShocked += OnShock;
        }

        public void Dispose()
        {
            events.FireIgnited -= OnIgnited;
            events.FireExtinguished -= OnExtinguished;
            events.FireBurnedOut -= OnBurnedOut;
            events.SteamCreated -= OnSteam;
            events.WaterImpact -= OnWaterImpact;
            events.PuddleCreated -= OnPuddle;
            events.NpcShocked -= OnShock;
            foreach (var flame in flames) effects.Release(EffectKind.Flame, flame.Value);
            flames.Clear();
        }

        void OnIgnited(FireNode node)
        {
            if (node.Zone==FireZoneId.Ending || flames.ContainsKey(node)) return;
            var flame = effects.Spawn(EffectKind.Flame, node.Position, node.Profile.flameScale);
            if (flame != null) flames[node] = flame;
        }

        public void Tick()
        {
            foreach (var flame in flames) flame.Value.transform.position = flame.Key.Position;
        }

        void OnExtinguished(FireNode node)
        {
            StopFlame(node);
            effects.SpawnOneShot(EffectKind.Steam, node.Position);
        }

        void OnBurnedOut(FireNode node) { StopFlame(node); }

        void StopFlame(FireNode node)
        {
            if (!flames.TryGetValue(node, out var ps)) return;
            flames.Remove(node);
            effects.Release(EffectKind.Flame, ps);
        }

        void OnSteam(Vector3 point) { effects.SpawnOneShot(EffectKind.Steam, point); }
        void OnShock(Vector3 point) { effects.SpawnOneShot(EffectKind.Spark, point); }

        void OnWaterImpact(Vector3 point, Vector3 normal)
        {
            if (Time.time < nextSplashTime) return;
            nextSplashTime = Time.time + splashInterval;
            effects.SpawnOneShot(EffectKind.Splash, point + normal * 0.05f);
        }

        void OnPuddle(Vector3 position, float diameter) { puddles.Spawn(position, diameter); }
    }
}
