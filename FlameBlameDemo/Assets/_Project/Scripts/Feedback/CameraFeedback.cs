using FlameBlame.Core;
using FlameBlame.Player;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Owns the camera rotation: look pitch + Perlin shake + recoil kick. Driven by events.
    public class CameraFeedback : MonoBehaviour
    {
        [SerializeField] Transform cam;
        [SerializeField] VisualTuning tuning;
        [SerializeField] CharacterController motor;
        [SerializeField] FlameBlame.Hose.HoseController hose;
        [SerializeField] FlameBlame.Core.ScaleProfile scale;   // optional: world FOV source
        Camera lens;
        Vector3 origin;
        float bob;
        [SerializeField] float shakeMaxAngle = 3f;
        [SerializeField] float shakeDecay = 4f;
        [SerializeField] float kickPerRecoil = 0.5f;   // degrees of pitch per m/s of recoil
        [SerializeField] float kickMax = 4f;
        [SerializeField] float kickDecay = 3f;

        PlayerLook look;
        IGameEvents events;
        float shake;
        float kick;
        float dip;
        public void LandingDip(float amount) { dip=Mathf.Max(dip,amount); }
        public void JetKick(float amount) { shake=Mathf.Max(shake,amount); kick=Mathf.Min(kickMax,kick+amount); }

        public void Initialize(PlayerLook playerLook, IGameEvents gameEvents)
        {
            look = playerLook;
            events = gameEvents;
            lens = cam.GetComponent<Camera>(); origin = cam.localPosition;
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
            dip=Mathf.MoveTowards(dip,0,dt*.3f);
            shake = Mathf.MoveTowards(shake, 0f, shakeDecay * dt);
            kick = Mathf.Lerp(kick, 0f, kickDecay * dt);

            float t = Time.time * (tuning != null ? tuning.shakeSpeed : 25f);
            float angle = tuning != null ? tuning.shakeAngle : shakeMaxAngle;
            float sx = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shake * angle;
            float sy = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shake * angle;
            cam.localRotation = Quaternion.Euler(look.Pitch - kick + sx, sy, 0f);
            if(tuning == null) return;
            bool moving = motor != null && motor.isGrounded && Vector3.ProjectOnPlane(motor.velocity,Vector3.up).sqrMagnitude > .1f;
            bob = Mathf.Lerp(bob,moving ? 1f : 0f,1f-Mathf.Exp(-tuning.fovResponse*dt));
            cam.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time*tuning.bobSpeed)*tuning.bobHeight*bob-dip);
            float flow = hose != null && hose.Held && hose.WaterFlowing ? hose.Pressure : 0f;
            float fovKick=hose!=null && hose.JetTuning!=null && hose.Modes.Current.UsesThrust ? hose.JetTuning.fovKick : tuning.fovKick;
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView,(scale!=null ? scale.worldFov : tuning.baseFov)+flow*fovKick,1f-Mathf.Exp(-tuning.fovResponse*dt));
        }
    }
}
