using UnityEngine;

namespace FlameBlame.Hose
{
    /// Glue: runs the simulation, holds state, exposes the nozzle. Rendering lives in HoseRenderer.
    [DefaultExecutionOrder(100)]
    public class HoseController : MonoBehaviour, IHoseTether, IPressureSource
    {
        [Header("Refs")]
        [SerializeField] Transform anchor;       // hydrant outlet
        [SerializeField] Transform handPoint;    // empty in front of the camera

        [SerializeField] FlameBlame.Core.ScaleProfile scale;
        public FlameBlame.Core.ScaleProfile Scale => scale;
        [Header("Rope")]
        [SerializeField] int segments = 32;
        [SerializeField] float segmentLength = 0.3f;
        [SerializeField] int iterations = 10;
        [SerializeField] float gravity = -12f;
        [SerializeField] float damping = 0.985f;
        [SerializeField] float radius = 0.06f;
        [SerializeField] bool confineToRoom = true;
        [SerializeField] LayerMask collisionMask = ~0;   // must exclude Player and Hose layers

        [Header("Pressure")]
        [SerializeField] float basePressure = 1f;
        [SerializeField] float minPressureFactor = 0.15f;
        [SerializeField] float kinksForFullChoke = 3f;
        [SerializeField] float chokeStartFactor=1.05f;
        [SerializeField] float chokeFullFactor=2.5f;

        [Header("Whip")]
        [SerializeField] float whipForce = 900f;
        [SerializeField] float whipNoise = 0.8f;
        [SerializeField] float grabDistance = 2.5f;

        [SerializeField] NozzleRig nozzleRig;
        [SerializeField] JetTuning jetTuning;
        public JetTuning JetTuning => jetTuning;
        public NozzleModeMachine Modes { get; private set; }
        public bool Holstered {get;private set;}
        Transform holster;
        public void SetHolstered(bool value) {Holstered=value; if(value) WaterFlowing=false;}
        Vector3 heldPosition;
        Quaternion heldRotation;
        public Transform Muzzle => nozzleRig != null ? nozzleRig.muzzle : handPoint;
        IHoseInput input;
        HoseSimulation sim;
        PhysicsHoseCollision collision;
        HosePressure pressure;
        HoseHoldState hold;

        public bool WaterFlowing { get; set; }
        public bool Held => hold != null && hold.IsHeld;
        public float Pressure => pressure != null ? pressure.Pressure : 0f;
        public Vector3[] Points => sim.Points;
        public int Segments => sim.Segments;
        public Vector3 NozzlePosition => nozzleRig != null ? nozzleRig.inlet.position : (Holstered ? holster.position : Held ? handPoint.position : sim.EndPosition);
        public Vector3 NozzleDirection => nozzleRig != null ? nozzleRig.muzzle.forward : (Held ? handPoint.forward : sim.EndDirection);

        public Vector3 MuzzlePosition => nozzleRig != null ? nozzleRig.muzzle.position : NozzlePosition;
        void SyncNozzle()
        {
            if(nozzleRig == null || sim == null) return;
            nozzleRig.Follow(Holstered ? holster : handPoint, sim.EndPosition, sim.EndDirection, Held || Holstered, Holstered);
            if(!Holstered && Held && Modes != null && Modes.Current.UsesThrust && input is FlameBlame.Player.IPlayerInput player)
            {
                var heading=Vector3.ProjectOnPlane(handPoint.parent.forward,Vector3.up);
                if(heading.sqrMagnitude>.001f)
                    nozzleRig.pose.rotation=Quaternion.LookRotation(heading)*Modes.Current.Rotation(jetTuning,player.Move);
            }
        }
        void LateUpdate() { SyncNozzle(); }
        // IHoseTether
        public bool IsTethered => Held;
        public Vector3 AnchorPosition => anchor.position;
        public float MaxReach => sim.MaxReach;

        public void Initialize(IHoseInput hoseInput)
        {
            input = hoseInput;
            holster=new GameObject("NozzleHolster").transform;
            holster.SetParent(handPoint.parent.parent,false); holster.localPosition=new Vector3(.35f,.8f,-.2f);
            holster.localRotation=Quaternion.Euler(65,0,0);
            Modes = new NozzleModeMachine();
            heldPosition = handPoint.localPosition; heldRotation = handPoint.localRotation;
            collision?.Dispose();
            collision = new PhysicsHoseCollision(collisionMask);
            sim = new HoseSimulation(segments, segmentLength, collision)
            {
                Iterations = iterations, Gravity = gravity, Damping = damping, Radius = radius,
                ChokeStartFactor=chokeStartFactor, ChokeFullFactor=chokeFullFactor
            };
            if(scale != null)
            {
                sim.TubeRadius=scale.tubeRadius; sim.Radius=scale.tubeRadius*.9f;
                if(confineToRoom) sim.RoomHalfExtents=new Vector2(scale.roomWidth*.5f,scale.roomLength*.5f);
                sim.BendLimitFactor=scale.bendLimitFactor; sim.Iterations=Mathf.Max(16,iterations);
            }
            sim.Reset(anchor.position, anchor.right);
            if(!confineToRoom) sim.ResetBetween(anchor.position,handPoint.position,Vector3.left);
            pressure = new HosePressure
            {
                BasePressure = basePressure, MinFactor = minPressureFactor, KinksForFullChoke = kinksForFullChoke
            };
            hold = new HoseHoldState();
        }

        void OnDestroy()
        {
            collision?.Dispose();
        }

        void Update()
        {
            if (input == null) return;
            if (!Holstered && input.TogglePressed)
                hold.Toggle(Vector3.Distance(handPoint.position, sim.EndPosition), grabDistance);
            Modes.Tick(Held, input.ModePressed);
            if(jetTuning != null && input is FlameBlame.Player.IPlayerInput playerInput)
            {
                float blend = 1-Mathf.Exp(-3*Time.deltaTime/jetTuning.poseDuration);
                handPoint.localPosition = Vector3.Lerp(handPoint.localPosition, Modes.Current.Position(jetTuning,heldPosition),blend);
                handPoint.localRotation = Quaternion.Slerp(handPoint.localRotation,heldRotation * Modes.Current.Rotation(jetTuning,playerInput.Move),blend);
            }
            SyncNozzle();
        }

        void FixedUpdate()
        {
            if (sim == null) return;
            float dt = Time.fixedDeltaTime;

            Vector3 whip = Vector3.zero;
            if (!Held && WaterFlowing)
            {
                float t = Time.time * 3f;
                Vector3 noise = new Vector3(
                    Mathf.PerlinNoise(t, 1f) - 0.5f,
                    Mathf.PerlinNoise(t, 2f) - 0.5f,
                    Mathf.PerlinNoise(t, 3f) - 0.5f) * 2f * whipNoise;
                whip = (sim.EndDirection + noise) * whipForce * pressure.Pressure;
            }

            SyncNozzle();
            sim.Step(dt, anchor.position, NozzlePosition, Held || Holstered, whip);
            pressure.TickBend(sim.BendChoke(), dt);
        }
    }
}
