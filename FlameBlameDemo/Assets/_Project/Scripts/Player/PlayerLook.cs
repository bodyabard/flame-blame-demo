using UnityEngine;

namespace FlameBlame.Player
{
    /// Yaw rotates the body, pitch is stored here; CameraFeedback composes the final camera rotation.
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] float sensitivity = 2f;
        [SerializeField] float pitchLimit = 80f;

        IPlayerInput input;

        public float Pitch { get; private set; }

        public void Initialize(IPlayerInput playerInput)
        {
            input = playerInput;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (input == null) return;
            if (input.CaptureCursorPressed && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            Vector2 look = input.Look * sensitivity;
            transform.Rotate(0f, look.x, 0f);
            Pitch = Mathf.Clamp(Pitch - look.y, -pitchLimit, pitchLimit);

            if (input.ReleaseCursorPressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
