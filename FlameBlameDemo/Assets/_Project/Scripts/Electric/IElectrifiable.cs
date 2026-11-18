using UnityEngine;

namespace FlameBlame.Electric
{
    public interface IElectrifiable
    {
        Vector3 Position { get; }
        float Radius { get; }
        bool Energized { get; set; }
    }
}
