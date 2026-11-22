using UnityEngine;
namespace FlameBlame.Hose
{
    public interface INozzleMode
    {
        bool UsesThrust { get; }
        Vector3 Position(JetTuning tuning, Vector3 heldPosition);
        Quaternion Rotation(JetTuning tuning, Vector2 move);
    }
    public sealed class ExtinguishMode : INozzleMode
    {
        public bool UsesThrust => false;
        public Vector3 Position(JetTuning tuning, Vector3 heldPosition) => heldPosition;
        public Quaternion Rotation(JetTuning tuning, Vector2 move) => Quaternion.identity;
    }
    public sealed class JetMode : INozzleMode
    {
        public bool UsesThrust => true;
        public Vector3 Position(JetTuning tuning, Vector3 heldPosition) => tuning.jetPosition;
        public Quaternion Rotation(JetTuning tuning, Vector2 move) =>
            Quaternion.Euler(tuning.downAngle + move.y * tuning.steerAngle, 0, -move.x * tuning.steerAngle);
    }
    public sealed class NozzleModeMachine
    {
        readonly INozzleMode extinguish = new ExtinguishMode(), jet = new JetMode();
        public INozzleMode Current { get; private set; }
        public event System.Action Changed;
        public NozzleModeMachine() { Current = extinguish; }
        public void Tick(bool held, bool toggle)
        {
            var next = !held ? extinguish : toggle ? (Current.UsesThrust ? extinguish : jet) : Current;
            if(next == Current) return;
            Current = next; Changed?.Invoke();
        }
    }
}
