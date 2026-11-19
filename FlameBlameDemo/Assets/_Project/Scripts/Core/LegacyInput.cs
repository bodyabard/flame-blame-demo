using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEngine;

namespace FlameBlame.Core
{
    /// Old Input Manager implementation of all input interfaces.
    public class LegacyInput : MonoBehaviour, IHoseInput, IPlayerInput
    {
        public bool TogglePressed => Input.GetKeyDown(KeyCode.E);
        public bool ValveHeld => Input.GetMouseButton(0);
        public bool JumpPressed => Input.GetKeyDown(KeyCode.Space);
        public Vector2 Move => Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        public Vector2 Look => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        public bool ReleaseCursorPressed => Input.GetKeyDown(KeyCode.Escape);
    }
}
