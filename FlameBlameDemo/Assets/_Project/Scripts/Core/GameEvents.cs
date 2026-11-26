using System;
using FlameBlame.Fire;
using UnityEngine;

namespace FlameBlame.Core
{
    public sealed class GameEvents : IGameEvents
    {
        public event Action<ScenarioSignal> ScenarioSignaled;
        public event Action DiaryOpened;
        public event Action DiaryRead;
        public event Action LoopBroken;
        public event Action<int> ObjectiveChanged;
        public void RaiseDiaryOpened() => DiaryOpened?.Invoke();
        public void RaiseDiaryRead() => DiaryRead?.Invoke();
        public void RaiseLoopBroken() => LoopBroken?.Invoke();
        public void RaiseScenarioSignal(ScenarioSignal signal) => ScenarioSignaled?.Invoke(signal);
        public void RaiseObjectiveChanged(int current) => ObjectiveChanged?.Invoke(current);
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
        public event Action<int> RoundStarted;
        public event Action<RoundEnding> RoundEnded;
        public event Action<int> TimerTick;
        public event Action<bool> NozzleModeChanged;
        public event Action<float> PressureChanged;
        public event Action<HudContext> HudContextChanged;
        public event Action<PromptKind> PromptChanged;
        public event Action<float> HardImpact;

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
        public void RaiseRoundStarted(int seconds) => RoundStarted?.Invoke(seconds);
        public void RaiseRoundEnded(RoundEnding ending) => RoundEnded?.Invoke(ending);
        public void RaiseTimerTick(int secondsLeft) => TimerTick?.Invoke(secondsLeft);
        public void RaiseNozzleModeChanged(bool thrust) => NozzleModeChanged?.Invoke(thrust);
        public void RaisePressureChanged(float pressure) => PressureChanged?.Invoke(pressure);
        public void RaiseHudContextChanged(HudContext context) => HudContextChanged?.Invoke(context);
        public void RaisePromptChanged(PromptKind prompt) => PromptChanged?.Invoke(prompt);
        public void RaiseHardImpact(float amount) => HardImpact?.Invoke(amount);
    }
}
