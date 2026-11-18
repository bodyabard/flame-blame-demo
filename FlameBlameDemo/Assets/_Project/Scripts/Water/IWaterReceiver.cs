using UnityEngine;

namespace FlameBlame.Water
{
    /// Anything that reacts to water: fire, NPCs, cables, floor.
    public interface IWaterReceiver
    {
        void ReceiveWater(Vector3 point, Vector3 normal, float amount);
    }
}
