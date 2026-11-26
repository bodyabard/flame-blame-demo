using System.Collections.Generic;
using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.Fire
{
    /// Mediator: ticks every FireNode centrally, spreads heat to neighbours, caps active fires.
    public sealed class FireSystem
    {
        readonly List<FireNode> nodes = new List<FireNode>(64);
        readonly IGameEvents events;

        public int MaxActiveFires { get; set; }
        public int ActiveCount { get; private set; }
        public int NodeCount => nodes.Count;

        public FireSystem(IGameEvents events, int maxActiveFires = 16)
        {
            this.events = events;
            MaxActiveFires = maxActiveFires;
        }

        public void Register(FireNode node) { if (!nodes.Contains(node)) nodes.Add(node); }

        public void Unregister(FireNode node)
        {
            if (!nodes.Remove(node)) return;
            if (node.State == FireState.Burning) ActiveCount--;
        }

        public bool Ignite(FireNode node)
        {
            if (!node.ZoneActive || node.State != FireState.Idle || !node.Profile.canBurn || node.FuelLeft <= 0f) return false;
            if (ActiveCount >= MaxActiveFires && node.Zone!=FireZoneId.Ending) return false;
            node.State = FireState.Burning;
            node.Heat = node.Profile.ignitionThreshold;
            node.Dirty = true;
            ActiveCount++;
            events?.RaiseFireIgnited(node);
            return true;
        }

        /// Returns true if the node was extinguished by this call.
        public bool ApplyWater(FireNode node, float amount)
        {
            if (node.State == FireState.Charred || node.Heat <= 0f) return false;
            node.Heat = Mathf.Max(0f, node.Heat - amount * node.Profile.waterCooling);
            node.Dirty = true;
            if (node.State != FireState.Burning || node.Heat > 0f) return false;

            node.State = FireState.Idle;
            ActiveCount--;
            events?.RaiseFireExtinguished(node);
            return true;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                FireNode n = nodes[i];
                if(!n.ZoneActive) continue;
                switch (n.State)
                {
                    case FireState.Burning:
                        if(n.Zone!=FireZoneId.Ending) n.FuelLeft -= dt;
                        if (n.Profile.reheatPerSecond > 0f) n.Heat = Mathf.Min(n.Profile.ignitionThreshold, n.Heat + n.Profile.reheatPerSecond * dt);
                        Spread(n, dt);
                        if (n.FuelLeft <= 0f) BurnOut(n);
                        n.Dirty = true;
                        break;
                    case FireState.Idle:
                        if (n.Heat >= n.Profile.ignitionThreshold && n.Profile.canBurn) Ignite(n);
                        else n.CoolDown(dt);
                        break;
                    default:
                        n.CoolDown(dt);
                        break;
                }
                n.NotifyVisual();
            }
        }

        void Spread(FireNode source, float dt)
        {
            float radius = source.Profile.spreadRadius;
            if (radius <= 0f) return;
            float r2 = radius * radius;
            float emit = source.Profile.spreadHeatPerSecond * dt;

            for (int j = 0; j < nodes.Count; j++)
            {
                FireNode m = nodes[j];
                if (!m.ZoneActive || m.Zone!=source.Zone || m == source || m.State != FireState.Idle) continue;
                float d2 = (m.Position - source.Position).sqrMagnitude;
                if (d2 >= r2) continue;
                float falloff = 1f - Mathf.Sqrt(d2) / radius;
                m.AddHeat(emit * m.Profile.flammability * falloff);
            }
        }

        void BurnOut(FireNode n)
        {
            n.State = FireState.Charred;
            n.Heat = 0f;
            n.FuelLeft = 0f;
            ActiveCount--;
            events?.RaiseFireBurnedOut(n);
        }

        public bool NearestBurning(Vector3 position, float radius, out Vector3 nearest, out float distance)
        {
            nearest = default;
            distance = radius;
            bool found = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].IsBurning) continue;
                float d = Vector3.Distance(position, nodes[i].Position);
                if (d >= distance) continue;
                distance = d;
                nearest = nodes[i].Position;
                found = true;
            }
            return found;
        }
    }
}
