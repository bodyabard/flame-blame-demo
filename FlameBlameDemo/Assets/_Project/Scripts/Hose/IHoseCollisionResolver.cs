using UnityEngine;

namespace FlameBlame.Hose
{
    /// Pushes a hose point out of geometry. Keeps HoseSimulation free of Physics.
    public interface IHoseCollisionResolver
    {
        Vector3 Resolve(Vector3 point, float radius);
    }
}
