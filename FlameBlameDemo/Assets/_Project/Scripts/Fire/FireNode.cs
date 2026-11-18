using System;
using UnityEngine;

namespace FlameBlame.Fire
{
    public enum FireState { Idle, Burning, Charred }

    /// Pure heat/fuel state of one flammable object. Mutated only by FireSystem.
    public sealed class FireNode
    {
        public FireMaterialProfile Profile { get; }
        public Vector3 Position { get; set; }
        public object Owner { get; }
        public FireState State { get; internal set; } = FireState.Idle;
        public float Heat { get; internal set; }
        public float FuelLeft { get; internal set; }

        /// Raised by FireSystem after a tick in which something visible changed.
        public event Action<FireNode> VisualChanged;

        internal bool Dirty;

        public bool IsBurning => State == FireState.Burning;
        public float Heat01 => Profile.ignitionThreshold <= 0f ? 0f : Mathf.Clamp01(Heat / Profile.ignitionThreshold);
        public float Charred01 => Profile.burnDuration <= 0f ? 0f : 1f - Mathf.Clamp01(FuelLeft / Profile.burnDuration);

        public FireNode(FireMaterialProfile profile, Vector3 position, object owner = null)
        {
            Profile = profile;
            Position = position;
            Owner = owner;
            FuelLeft = profile.burnDuration;
        }

        internal void AddHeat(float amount)
        {
            if (State != FireState.Idle || amount <= 0f) return;
            Heat = Mathf.Min(Heat + amount, Profile.ignitionThreshold);
            Dirty = true;
        }

        internal void CoolDown(float dt)
        {
            if (Heat <= 0f) return;
            Heat = Mathf.Max(0f, Heat - Profile.coolRate * dt);
            Dirty = true;
        }

        internal void NotifyVisual()
        {
            if (!Dirty) return;
            Dirty = false;
            VisualChanged?.Invoke(this);
        }
    }
}
