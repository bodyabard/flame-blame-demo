using System;

namespace FlameBlame.Hose
{
    /// State pattern: Held and Dropped.
    public sealed class HoseHoldState
    {
        interface IState
        {
            bool IsHeld { get; }
            IState Toggle(float distanceToNozzle, float grabDistance);
        }

        sealed class HeldState : IState
        {
            public bool IsHeld => true;
            public IState Toggle(float distance, float grab) => Dropped;
        }

        sealed class DroppedState : IState
        {
            public bool IsHeld => false;
            public IState Toggle(float distance, float grab) => distance < grab ? Held : this;
        }

        static readonly IState Held = new HeldState();
        static readonly IState Dropped = new DroppedState();

        IState current = Held;

        public bool IsHeld => current.IsHeld;
        public event Action<bool> Changed;

        public void Toggle(float distanceToNozzle, float grabDistance)
        {
            var next = current.Toggle(distanceToNozzle, grabDistance);
            if (next == current) return;
            current = next;
            Changed?.Invoke(current.IsHeld);
        }
    }
}
