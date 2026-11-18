using System;
using FlameBlame.Fire;
using UnityEngine;

namespace FlameBlame.Core
{
    public sealed class GameEvents : IGameEvents
    {
        public event Action<FireNode> FireIgnited;
        public event Action<FireNode> FireExtinguished;
        public event Action<FireNode> FireBurnedOut;
        public event Action<int> DamageDealt;
        public event Action<DamageReport> DamageReported;
        public event Action PlayerShocked;
        public event Action<Vector3> NpcShocked;
        public event Action<Vector3, float> PuddleCreated;
        public event Action<Vector3> SteamCreated;
        public event Action<Vector3, Vector3> WaterImpact;
        public event Action<Vector3> RecoilApplied;
        public event Action<float> ShakeRequested;

        public void RaiseFireIgnited(FireNode node) => FireIgnited?.Invoke(node);
        public void RaiseFireExtinguished(FireNode node) => FireExtinguished?.Invoke(node);
        public void RaiseFireBurnedOut(FireNode node) => FireBurnedOut?.Invoke(node);
        public void RaiseDamageDealt(int amount) => DamageDealt?.Invoke(amount);
        public void RaiseDamageReported(DamageReport report)
        {
            if (report.Amount <= 0) return;
            RaiseDamageDealt(report.Amount);
            DamageReported?.Invoke(report);
        }
        public void RaisePlayerShocked() => PlayerShocked?.Invoke();
        public void RaiseNpcShocked(Vector3 position) => NpcShocked?.Invoke(position);
        public void RaisePuddleCreated(Vector3 position, float diameter) => PuddleCreated?.Invoke(position, diameter);
        public void RaiseSteamCreated(Vector3 position) => SteamCreated?.Invoke(position);
        public void RaiseWaterImpact(Vector3 point, Vector3 normal) => WaterImpact?.Invoke(point, normal);
        public void RaiseRecoil(Vector3 velocityDelta) => RecoilApplied?.Invoke(velocityDelta);
        public void RaiseShake(float amount) => ShakeRequested?.Invoke(amount);
    }
}
