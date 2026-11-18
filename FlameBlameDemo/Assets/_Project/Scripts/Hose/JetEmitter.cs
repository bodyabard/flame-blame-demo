using UnityEngine;

namespace FlameBlame.Hose
{
    /// Water jet visuals: one world-space ParticleSystem whose particle buffer is the pool (capped by maxParticles).
    public class JetEmitter : MonoBehaviour
    {
        [SerializeField] ParticleSystem jet;
        [SerializeField] float lifetime = 1f;
        [SerializeField] float gravityModifier = 0.5f;
        [SerializeField] int maxParticles = 400;      // total scene budget is 1000

        public void Initialize(LayerMask collidesWith)
        {
            var main = jet.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.gravityModifier = gravityModifier;
            main.maxParticles = maxParticles;
            var col = jet.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.lifetimeLoss = 1f;
            col.collidesWith = collidesWith;
            if (!jet.isPlaying) jet.Play();
        }

        public void SetState(Vector3 position, Vector3 direction, float speed, float rate)
        {
            jet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            var main = jet.main;
            main.startSpeed = speed;
            var em = jet.emission;
            em.rateOverTime = rate;
        }
    }
}
