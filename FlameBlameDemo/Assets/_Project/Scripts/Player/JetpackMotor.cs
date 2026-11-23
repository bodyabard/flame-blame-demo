using FlameBlame.Hose;
using UnityEngine;
namespace FlameBlame.Player
{
    // Pure velocity integrator. PlayerMotor remains the only CharacterController owner.
    public sealed class JetpackMotor
    {
        readonly JetTuning tuning;
        public Vector3 Velocity { get; private set; }
        public float Spool { get; private set; }
        public JetpackMotor(JetTuning tuning) { this.tuning = tuning; }
        public static float EquilibriumPressure(float gravity, float thrust, float upwardFraction) =>
            gravity / Mathf.Max(.001f, thrust * upwardFraction);
        public static Vector3 LimitVelocity(Vector3 velocity, float rise, float fall, float air)
        {
            var horizontal = Vector3.ClampMagnitude(new Vector3(velocity.x,0,velocity.z), air);
            return horizontal + Vector3.up * Mathf.Clamp(velocity.y,-fall,rise);
        }
        public Vector3 Step(float dt, bool flowing, float pressure, Vector3 muzzleForward, Vector3 control,
            bool grounded, Vector3 position, IHoseTether tether)
        {
            Spool = Mathf.MoveTowards(Spool, flowing ? 1 : 0, dt / Mathf.Max(.01f, flowing ? tuning.spoolUp : tuning.spoolDown));
            Vector3 velocity = Velocity;
            if(grounded && velocity.y < 0) velocity.y = 0;
            if(flowing)
            {
                velocity.y=Mathf.MoveTowards(velocity.y,tuning.maxRiseSpeed,tuning.liftResponse*dt);
                var horizontal=Vector3.MoveTowards(Vector3.ProjectOnPlane(velocity,Vector3.up),
                    Vector3.ClampMagnitude(control,1)*tuning.maxAirSpeed,tuning.steeringResponse*dt);
                velocity.x=horizontal.x; velocity.z=horizontal.z;
            }
            else
            {
                velocity.y-=tuning.gravity*dt;
                var horizontal=Vector3.ProjectOnPlane(velocity,Vector3.up);
                velocity-=horizontal*Mathf.Min(1,tuning.airDrag*dt);
                velocity+=control*tuning.airControl*dt;
            }
            if(tether.IsTethered)
            {
                var offset = position - tether.AnchorPosition;
                float excess = offset.magnitude - tether.MaxReach;
                if(excess > 0)
                {
                    var normal = offset.normalized;
                    velocity -= normal * (excess*tuning.tetherSpring + Mathf.Max(0,Vector3.Dot(velocity,normal))*tuning.tetherDamping)*dt;
                }
            }
            Velocity = LimitVelocity(velocity,tuning.maxRiseSpeed,tuning.maxFallSpeed,tuning.maxAirSpeed);
            return Velocity;
        }
        public void Reset(Vector3 velocity) { Velocity = velocity; Spool = 0; }
        public void Ceiling() { var v=Velocity; v.y=Mathf.Min(0,v.y); Velocity=v; }
    }
}
