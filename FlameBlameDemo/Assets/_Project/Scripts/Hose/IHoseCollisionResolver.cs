using UnityEngine;

namespace FlameBlame.Hose
{
    /// Pushes a hose point out of geometry. Keeps HoseSimulation free of Physics.
    public interface IHoseCollisionResolver
    {
        Vector3 Resolve(Vector3 point, float radius);
    }

    public interface IHoseMotionCollisionResolver : IHoseCollisionResolver
    {
        Vector3 ResolveMotion(Vector3 from, Vector3 to, float radius);
    }
    public interface IHoseSpanCollision
    {
        Vector3 SpanCorrection(Vector3 from,Vector3 to,float radius);
    }
}
