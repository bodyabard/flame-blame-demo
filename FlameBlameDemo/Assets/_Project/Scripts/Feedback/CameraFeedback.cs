using FlameBlame.Core;
using FlameBlame.Player;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Owns the camera rotation: look pitch + Perlin shake + recoil kick. Driven by events.
    public class CameraFeedback : MonoBehaviour
    {
        [SerializeField] Transform cam;
        [SerializeField] float shakeMaxAngle = 3f;
        [SerializeField] float shakeDecay = 4f;
        [SerializeField] float kickPerRecoil = 0.5f;   // degrees of pitch per m/s of recoil
        [SerializeField] float kickMax = 4f;
        [SerializeField] float kickDecay = 3f;

        PlayerLook look;
        IGameEvents events;
        float shake;
        float kick;

        public void Initialize(PlayerLook playerLook, IGameEvents gameEvents)
        {
            look = playerLook;
            events = gameEvents;
            events.ShakeRequested += OnShake;
            events.RecoilApplied += OnRecoil;
        }

        void OnDestroy()
        {
            if (events == null) return;
            events.ShakeRequested -= OnShake;
            events.RecoilApplied -= OnRecoil;
        }

        void OnShake(float amount) { shake = Mathf.Max(shake, Mathf.Clamp01(amount)); }
        void OnRecoil(Vector3 delta) { kick = Mathf.Min(kick + delta.magnitude * kickPerRecoil, kickMax); }

        void LateUpdate()
        {
            if (look == null) return;
            float dt = Time.deltaTime;
            shake = Mathf.MoveTowards(shake, 0f, shakeDecay * dt);
            kick = Mathf.Lerp(kick, 0f, kickDecay * dt);

            float t = Time.time * 25f;
            float sx = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shake * shakeMaxAngle;
            float sy = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shake * shakeMaxAngle;
            cam.localRotation = Quaternion.Euler(look.Pitch - kick + sx, sy, 0f);
        }
    }
}
