using FlameBlame.Core;
using FlameBlame.Hose;
using UnityEngine;

namespace FlameBlame.Player
{
    /// Walking, gravity, recoil impulse and the hose leash.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3.5f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float impulseDamping = 6f;

        CharacterController cc;
        IPlayerInput input;
        IHoseTether tether;
        IGameEvents events;
        Vector3 impulse;
        float yVel;

        public void Initialize(IPlayerInput playerInput, IHoseTether hoseTether, IGameEvents gameEvents)
        {
            cc = GetComponent<CharacterController>();
            input = playerInput;
            tether = hoseTether;
            events = gameEvents;
            events.RecoilApplied += OnRecoil;
        }

        void OnDestroy()
        {
            if (events != null) events.RecoilApplied -= OnRecoil;
        }

        void OnRecoil(Vector3 velocityDelta) { impulse += velocityDelta; }

        void Update()
        {
            if (input == null) return;
            float dt = Time.deltaTime;

            Vector2 m = input.Move;
            Vector3 move = transform.TransformDirection(new Vector3(m.x, 0f, m.y)) * moveSpeed;

            if (cc.isGrounded && yVel < 0f) yVel = -2f;
            yVel += gravity * dt;

            impulse = Vector3.Lerp(impulse, Vector3.zero, impulseDamping * dt);
            cc.Move((move + impulse + Vector3.up * yVel) * dt);

            if (tether.IsTethered)
            {
                Vector3 off = transform.position - tether.AnchorPosition;
                off.y = 0f;
                float max = tether.MaxReach;
                if (off.magnitude > max)
                    cc.Move(-off.normalized * (off.magnitude - max));
            }
        }
    }
}
