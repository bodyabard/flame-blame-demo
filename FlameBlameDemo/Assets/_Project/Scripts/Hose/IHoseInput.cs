namespace FlameBlame.Hose
{
    public interface IHoseInput
    {
        /// Grab/drop pressed this frame.
        bool TogglePressed { get; }
        bool ModePressed { get; }
        /// Valve (fire) button held.
        bool ValveHeld { get; }
    }
}
