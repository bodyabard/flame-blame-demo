using UnityEngine;

namespace FlameBlame.Hose
{
    /// What the player needs to know to stay on the leash.
    public interface IHoseTether
    {
        bool IsTethered { get; }
        Vector3 AnchorPosition { get; }
        float MaxReach { get; }
    }
}
