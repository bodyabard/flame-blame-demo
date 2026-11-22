using UnityEngine;

namespace FlameBlame.Hose
{
    /// Glue: runs the simulation, holds state, exposes the nozzle. Rendering lives in HoseRenderer.
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
        [SerializeField] LayerMask collisionMask = ~0;   // must exclude Player and Hose layers

        [Header("Pressure")]
        [SerializeField] float basePressure = 1f;
        [SerializeField] float minPressureFactor = 0.15f;
        [SerializeField] float kinksForFullChoke = 3f;

        [Header("Whip")]
        [SerializeField] float whipForce = 900f;
        [SerializeField] float whipNoise = 0.8f;
        [SerializeField] float grabDistance = 2.5f;

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
        public Vector3 NozzlePosition => Held ? handPoint.position : sim.EndPosition;
        public Vector3 NozzleDirection => Held ? handPoint.forward : sim.EndDirection;

        // IHoseTether
        public bool IsTethered => Held;
        public Vector3 AnchorPosition => anchor.position;
        public float MaxReach => sim.MaxReach;

        public void Initialize(IHoseInput hoseInput)
        {
            input = hoseInput;
            collision?.Dispose();
            collision = new PhysicsHoseCollision(collisionMask);
            sim = new HoseSimulation(segments, segmentLength, collision)
            {
                Iterations = iterations, Gravity = gravity, Damping = damping, Radius = radius
            };
            if(scale != null)
            {
                sim.TubeRadius=scale.tubeRadius; sim.Radius=scale.tubeRadius*.9f;
                sim.RoomHalfExtents=new Vector2(scale.roomWidth*.5f,scale.roomLength*.5f);
                sim.BendLimitFactor=scale.bendLimitFactor; sim.Iterations=Mathf.Max(16,iterations);
            }
            sim.Reset(anchor.position, anchor.right);
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
            if (input.TogglePressed)
                hold.Toggle(Vector3.Distance(handPoint.position, sim.EndPosition), grabDistance);
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

            sim.Step(dt, anchor.position, handPoint.position, Held, whip);
            pressure.TickBend(sim.BendChoke(), dt);
        }
    }
}
