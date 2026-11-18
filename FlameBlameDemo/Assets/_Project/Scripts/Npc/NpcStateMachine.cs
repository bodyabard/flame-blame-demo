using UnityEngine;

namespace FlameBlame.Npc
{
    public enum NpcState { Idle, Panic, Shocked, Charred, Washed }

    public sealed class NpcStateMachine
    {
        public NpcState State { get; private set; }
        public bool IsCharred { get; private set; }
        float remaining;
        public void React(NpcState reaction, float seconds)
        {
            if (reaction == NpcState.Charred) IsCharred = true;
            State = reaction;
            remaining = seconds;
        }
        public void Tick(float dt, bool danger)
        {
            remaining = Mathf.Max(0f, remaining - dt);
            if (remaining > 0f) return;
            State = danger ? NpcState.Panic : IsCharred ? NpcState.Charred : NpcState.Idle;
        }
    }
}
