using FlameBlame.Core;
using FlameBlame.Electric;
using FlameBlame.Fire;
using FlameBlame.Water;
using UnityEngine;

namespace FlameBlame.Npc
{
    [RequireComponent(typeof(Rigidbody))]
    public class NpcActor : MonoBehaviour, IWaterReceiver
    {
        [SerializeField] Transform visual;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] string displayName = "Сусід";
        [SerializeField] float panicRadius = 3.2f;
        [SerializeField] float charRadius = 0.9f;
        [SerializeField] float panicAcceleration = 8f;
        [SerializeField] float maxSpeed = 2.2f;
        [SerializeField] float washForce = 25f;
        [SerializeField] float reactionSeconds = 1.2f;
        [SerializeField] float charExposure = 1.5f;
        [SerializeField] int washedCost = 120;
        [SerializeField] int charredCost = 600;
        [SerializeField] float feetOffset = 0.9f;
        readonly NpcStateMachine state = new NpcStateMachine();
        Rigidbody body;
        IGameEvents events;
        MaterialPropertyBlock block;
        Color originalColor;
        float exposure, nextShock, nextWashScore;
        public NpcState State => state.State;

        public void Initialize(IGameEvents gameEvents)
        {
            events = gameEvents;
            body = GetComponent<Rigidbody>();
            block = new MaterialPropertyBlock();
            originalColor = bodyRenderer.sharedMaterial.GetColor("_BaseColor");
        }
        public void Tick(float dt, FireSystem fire, ElectricitySystem electricity, ChaosSettings settings)
        {
            bool danger = fire.NearestBurning(transform.position, panicRadius, out Vector3 threat, out float distance);
            state.Tick(dt, danger);
            if (danger && state.State == NpcState.Panic)
            {
                Vector3 away = Vector3.ProjectOnPlane(transform.position - threat, Vector3.up).normalized;
                Vector3 velocity = body.linearVelocity;
                if (Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude < maxSpeed)
                    body.AddForce(away * panicAcceleration, ForceMode.Acceleration);
            }
            exposure = danger && distance < charRadius ? exposure + dt : Mathf.Max(0f, exposure - dt);
            if (!state.IsCharred && exposure >= charExposure)
            {
                state.React(NpcState.Charred, reactionSeconds);
                events.RaiseDamageReported(new DamageReport(displayName, "засмага без SPF", charredCost));
            }
            if (Time.time >= nextShock && electricity.IsLive(transform.position - Vector3.up * feetOffset, out Vector3 source))
            {
                nextShock = Time.time + settings.shockCooldown;
                state.React(NpcState.Shocked, reactionSeconds);
                Vector3 direction = Vector3.ProjectOnPlane(transform.position - source, Vector3.up).normalized;
                body.AddForce((direction + Vector3.up * 0.6f) * settings.shockStrength, ForceMode.VelocityChange);
                events.RaiseNpcShocked(transform.position);
                events.RaiseDamageReported(new DamageReport(displayName, "рахунок за електрику", settings.npcShockDamage));
            }
            Color color = state.State == NpcState.Shocked ? Color.yellow : state.IsCharred ? new Color(0.09f, 0.07f, 0.06f) : originalColor;
            block.SetColor("_BaseColor", color);
            bodyRenderer.SetPropertyBlock(block);
            if (visual != null)
            {
                float wobble = state.State == NpcState.Idle ? 2f : state.State == NpcState.Shocked ? 18f : 9f;
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 14f) * wobble);
            }
        }
        public void ReceiveWater(Vector3 point, Vector3 normal, float amount)
        {
            if (events == null) return;
            state.React(NpcState.Washed, reactionSeconds);
            body.AddForceAtPosition((-normal + Vector3.up * 0.15f) * washForce * amount, point, ForceMode.Impulse);
            if (Time.time < nextWashScore) return;
            nextWashScore = Time.time + 5f;
            events.RaiseDamageReported(new DamageReport(displayName, "душ із доставкою", washedCost));
        }
    }
}
