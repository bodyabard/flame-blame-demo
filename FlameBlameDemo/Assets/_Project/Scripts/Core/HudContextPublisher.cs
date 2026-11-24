using FlameBlame.Hose;
using FlameBlame.Ladder;
using FlameBlame.Player;
using UnityEngine;

namespace FlameBlame.Core
{
    /// Bridge from gameplay state to HUD events: raises only when something actually changed,
    /// so the UI never polls gameplay objects.
    public sealed class HudContextPublisher
    {
        readonly IGameEvents events;
        readonly HoseController hose;
        readonly PlayerLadderInteraction ladder;
        readonly PlayerLocomotionStateMachine locomotion;
        HudContext context = (HudContext)(-1);
        PromptKind prompt = (PromptKind)(-1);
        float pressure = -1f;

        public HudContextPublisher(IGameEvents events, HoseController hose,
            PlayerLadderInteraction ladder, PlayerLocomotionStateMachine locomotion)
        {
            this.events = events; this.hose = hose; this.ladder = ladder; this.locomotion = locomotion;
            hose.Modes.Changed += OnModeChanged;
        }

        void OnModeChanged() { events.RaiseNozzleModeChanged(hose.Modes.Current.UsesThrust); }

        public void Tick()
        {
            bool carrying = ladder != null && ladder.Carried != null;
            bool climbing = locomotion != null && locomotion.State == PlayerLocomotionState.Climb;
            var nextContext = climbing ? HudContext.Climbing
                : carrying ? HudContext.Carrying
                : !hose.Held ? HudContext.Dropped
                : hose.Modes.Current.UsesThrust ? HudContext.Jet : HudContext.Extinguish;
            if (nextContext != context) { context = nextContext; events.RaiseHudContextChanged(context); }

            var nextPrompt = carrying ? PromptKind.PlaceLadder
                : ladder != null && !climbing && ladder.HasFocus ? PromptKind.PickLadder : PromptKind.None;
            if (nextPrompt != prompt) { prompt = nextPrompt; events.RaisePromptChanged(prompt); }

            float p = hose.NozzlePressure;
            if (Mathf.Abs(p - pressure) > 0.002f) { pressure = p; events.RaisePressureChanged(p); }
        }

        public void Dispose() { hose.Modes.Changed -= OnModeChanged; }
    }
}
