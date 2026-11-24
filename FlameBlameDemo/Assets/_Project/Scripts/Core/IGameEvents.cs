using System;
using FlameBlame.Fire;
using UnityEngine;

namespace FlameBlame.Core
{
    /// Global Observer hub. UI, audio and VFX subscribe; gameplay code raises.
    public interface IGameEvents
    {
        event Action<ScenarioSignal> ScenarioSignaled;
        event Action<int> ObjectiveChanged;
        void RaiseScenarioSignal(ScenarioSignal signal);
        void RaiseObjectiveChanged(int current);
        event Action<FireNode> FireIgnited;
        event Action<FireNode> FireExtinguished;
        event Action<FireNode> FireBurnedOut;
        event Action<int> DamageDealt;
        event Action<DamageReport> DamageReported;
        event Action PlayerShocked;
        event Action<Vector3> NpcShocked;
        event Action<Vector3, float> PuddleCreated;      // position, diameter
        event Action<Vector3> SteamCreated;              // position
        event Action<Vector3, Vector3> WaterImpact;      // point, normal
        event Action<Vector3> RecoilApplied;             // velocity delta for the player
        event Action<float> ShakeRequested;              // 0..1

        // HUD-facing events
        event Action<int> RoundStarted;                  // total seconds
        event Action<RoundEnding> RoundEnded;
        event Action<int> TimerTick;                     // whole seconds left
        event Action<bool> NozzleModeChanged;            // true = jet thrust
        event Action<float> PressureChanged;             // 0..1
        event Action<HudContext> HudContextChanged;
        event Action<PromptKind> PromptChanged;
        event Action<float> HardImpact;                  // 0..1, ladder fall or hard landing

        void RaiseFireIgnited(FireNode node);
        void RaiseFireExtinguished(FireNode node);
        void RaiseFireBurnedOut(FireNode node);
        void RaiseDamageDealt(int amount);
        void RaiseDamageReported(DamageReport report);
        void RaisePlayerShocked();
        void RaiseNpcShocked(Vector3 position);
        void RaisePuddleCreated(Vector3 position, float diameter);
        void RaiseSteamCreated(Vector3 position);
        void RaiseWaterImpact(Vector3 point, Vector3 normal);
        void RaiseRecoil(Vector3 velocityDelta);
        void RaiseShake(float amount);

        void RaiseRoundStarted(int seconds);
        void RaiseRoundEnded(RoundEnding ending);
        void RaiseTimerTick(int secondsLeft);
        void RaiseNozzleModeChanged(bool thrust);
        void RaisePressureChanged(float pressure);
        void RaiseHudContextChanged(HudContext context);
        void RaisePromptChanged(PromptKind prompt);
        void RaiseHardImpact(float amount);
    }
}
