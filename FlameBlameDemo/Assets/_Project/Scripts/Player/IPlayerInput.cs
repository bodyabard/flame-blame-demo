using UnityEngine;

namespace FlameBlame.Player
{
    public interface IPlayerInput
    {
        Vector2 Move { get; }       // x strafe, y forward
        Vector2 Look { get; }       // mouse delta (already per-frame)
        bool ReleaseCursorPressed { get; }
    }
}
