namespace FlameBlame.Hose
{
    public interface IPressureSource
    {
        /// Normalized pressure, 0..~1 (can exceed 1 if base pressure is tuned up).
        float Pressure { get; }
    }
}
