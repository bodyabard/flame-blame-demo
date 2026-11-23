using UnityEngine;
namespace FlameBlame.Hose
{
    [CreateAssetMenu(menuName="FlameBlame/Jet Tuning")]
    public sealed class JetTuning : ScriptableObject
    {
        public float thrust = 26f, gravity = 20f;
        public float liftResponse = 60f, steeringResponse = 20f;
        public float maxRiseSpeed = 3.5f, maxFallSpeed = 12f, maxAirSpeed = 4f;
        public float spoolUp = .28f, spoolDown = .22f, airControl = 2f, airDrag = .5f;
        public float tetherSpring = 14f, tetherDamping = 5f;
        public float poseDuration = .2f, downAngle = 78f, steerAngle = 18f;
        public Vector3 jetPosition = new Vector3(.24f,-.22f,.55f);
        public float fovKick = 7f, landingDip = .08f;
        public float sprayMultiplier = 1.35f, sprayAngle = 7f;
    }
}
