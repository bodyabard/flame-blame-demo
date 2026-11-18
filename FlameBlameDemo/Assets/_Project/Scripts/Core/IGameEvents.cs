using System;
using FlameBlame.Fire;
using UnityEngine;

namespace FlameBlame.Core
{
    /// Global Observer hub. UI, audio and VFX subscribe; gameplay code raises.
    public interface IGameEvents
    {
        event Action<FireNode> FireIgnited;
        event Action<FireNode> FireExtinguished;
        event Action<FireNode> FireBurnedOut;
        event Action<int> DamageDealt;
        event Action<Vector3> NpcShocked;
        event Action<Vector3, float> PuddleCreated;      // position, diameter
        event Action<Vector3> SteamCreated;              // position
        event Action<Vector3, Vector3> WaterImpact;      // point, normal
        event Action<Vector3> RecoilApplied;             // velocity delta for the player
        event Action<float> ShakeRequested;              // 0..1

        void RaiseFireIgnited(FireNode node);
        void RaiseFireExtinguished(FireNode node);
        void RaiseFireBurnedOut(FireNode node);
        void RaiseDamageDealt(int amount);
        void RaiseNpcShocked(Vector3 position);
        void RaisePuddleCreated(Vector3 position, float diameter);
        void RaiseSteamCreated(Vector3 position);
        void RaiseWaterImpact(Vector3 point, Vector3 normal);
        void RaiseRecoil(Vector3 velocityDelta);
        void RaiseShake(float amount);
    }
}
