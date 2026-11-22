using FlameBlame.Core;
using FlameBlame.Hose;
using UnityEngine;

namespace FlameBlame.Player
{
    /// Walking, grounded jumping, gravity, recoil impulse and the hose leash.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3.5f;
        [SerializeField] float gravity = -20f;
        [SerializeField, Min(0f)] float jumpHeight = 1f;
        [SerializeField] float impulseDamping = 6f;

        CharacterController cc;
        IPlayerInput input;
        IHoseTether tether;
        IGameEvents events;
        Vector3 impulse;
        float yVel;
        HoseController hose;
        JetpackMotor jetpack;
        bool wasFlying;
        public event System.Action<float> Landed;
        public float Speed => cc!=null ? cc.velocity.magnitude : 0;

        public void Initialize(IPlayerInput playerInput, IHoseTether hoseTether, IGameEvents gameEvents)
        {
            cc = GetComponent<CharacterController>();
            input = playerInput;
            tether = hoseTether;
            hose = hoseTether as HoseController;
            if(hose != null && hose.JetTuning != null) jetpack = new JetpackMotor(hose.JetTuning);
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

            bool flying = jetpack != null && (hose.Modes.Current.UsesThrust || wasFlying && !cc.isGrounded);
            if(flying)
            {
                if(!wasFlying) jetpack.Reset(impulse + Vector3.up*yVel);
                var velocity=jetpack.Step(dt,hose.Held && hose.Modes.Current.UsesThrust && hose.WaterFlowing,
                    hose.Pressure,hose.Muzzle.forward,move/moveSpeed,cc.isGrounded,transform.position,tether);
                bool grounded=cc.isGrounded;
                var flags=cc.Move(velocity*dt);
                if((flags & CollisionFlags.Above)!=0) { jetpack.Ceiling(); events.RaiseShake(.2f); }
                if(!grounded && cc.isGrounded && velocity.y<0) { Landed?.Invoke(-velocity.y); events.RaiseShake(Mathf.Clamp01(-velocity.y*.04f)); }
                yVel=jetpack.Velocity.y; impulse=Vector3.zero; wasFlying=true;
                return;
            }
            if(wasFlying) { wasFlying=false; jetpack.Reset(Vector3.zero); }

            if (cc.isGrounded && yVel <= 0f)
                yVel = input.JumpPressed ? Mathf.Sqrt(Mathf.Max(0f, -2f * gravity * jumpHeight)) : -2f;
            yVel += gravity * dt;

            impulse = Vector3.Lerp(impulse, Vector3.zero, impulseDamping * dt);
            CollisionFlags collisions = cc.Move((move + impulse + Vector3.up * yVel) * dt);
            if ((collisions & CollisionFlags.Above) != 0 && yVel > 0f) yVel = 0f;

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
