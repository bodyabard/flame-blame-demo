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
        /// Set once a burning node has been put out: it can never catch fire again (neighbours cannot re-ignite it).
        public bool Doused { get; internal set; }

        /// Raised by FireSystem after a tick in which something visible changed.
        public event Action<FireNode> VisualChanged;

        internal bool Dirty;

        public FireZoneId Zone { get; set; }
        public bool ZoneActive { get; set; } = true;
        public bool IsBurning => State == FireState.Burning;
        public float Heat01 => Profile.ignitionThreshold <= 0f ? 0f : Mathf.Clamp01(Heat / Profile.ignitionThreshold);
        public float Charred01
        {
            get
            {
                if (State == FireState.Charred) return 1f;
                float burned = Mathf.Max(0f, Profile.burnDuration - FuelLeft);
                float progress = 1f - Mathf.Exp(-burned / Mathf.Max(0.1f, Profile.charSeconds));
                if (State == FireState.Burning) progress = Mathf.Max(progress, Profile.ignitionChar);
                return Mathf.Clamp01(progress);
            }
        }

        public FireNode(FireMaterialProfile profile, Vector3 position, object owner = null)
        {
            Profile = profile;
            Position = position;
            Owner = owner;
            FuelLeft = profile.burnDuration;
        }

        internal void AddHeat(float amount)
        {
            if (Doused || State != FireState.Idle || amount <= 0f) return;
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
