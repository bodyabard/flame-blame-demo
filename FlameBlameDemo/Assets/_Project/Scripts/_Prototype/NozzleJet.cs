using UnityEngine;

/// Струмінь води: частинки (візуал) + SphereCast (логіка).
/// ЛКМ — вода. Кинув брандспойт під час води — потік тримається ще кілька секунд.
public class NozzleJet : MonoBehaviour
{
    [Header("Refs")]
    public HoseSystem hose;
    public PlayerController player;
    public ParticleSystem jet;                // Particle System, Simulation Space = World
    public LayerMask hitMask = ~0;            // виключи шар Player!

    [Header("Jet")]
    public float maxSpeed = 15f;
    public float lifetime = 1.0f;
    public int emissionRate = 450;            // 450 * 1с = ~450 частинок. Бюджет до 1000
    public float jetRadius = 0.12f;
    public float rangeFactor = 0.55f;         // дальність = швидкість * factor
    public float waterPerSecond = 1f;
    public float pushForce = 25f;
    public float droppedFlowTime = 8f;

    [Header("Recoil")]
    public float recoilPerSecond = 9f;
    public float shakeAmount = 0.35f;

    bool valveOpen;
    float droppedTimer;

    void Awake()
    {
        if (jet == null) return;
        var main = jet.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = lifetime;
        main.gravityModifier = 0.5f;
        main.maxParticles = 1000;
        var col = jet.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.lifetimeLoss = 1f;
    }

    void Update()
    {
        if (hose.Held)
        {
            valveOpen = Input.GetMouseButton(0);
            droppedTimer = droppedFlowTime;
        }
        else if (valveOpen)
        {
            droppedTimer -= Time.deltaTime;
            if (droppedTimer <= 0f) valveOpen = false;
        }
        hose.WaterFlowing = valveOpen;

        float p = hose.Pressure;
        float speed = maxSpeed * p;

        if (jet != null)
        {
            jet.transform.SetPositionAndRotation(hose.NozzlePos, Quaternion.LookRotation(hose.NozzleDir));
            var main = jet.main;
            main.startSpeed = speed;
            var em = jet.emission;
            em.rateOverTime = valveOpen ? emissionRate * Mathf.Clamp01(p + 0.2f) : 0f;
        }

        if (!valveOpen) return;

        Vector3 dir = hose.NozzleDir;
        float range = speed * rangeFactor;

        if (Physics.SphereCast(hose.NozzlePos, jetRadius, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            var rcv = hit.collider.GetComponentInParent<IWaterReceiver>();
            if (rcv != null) rcv.ReceiveWater(hit.point, hit.normal, waterPerSecond * p * Time.deltaTime);
            if (hit.rigidbody != null)
                hit.rigidbody.AddForceAtPosition(dir * pushForce * p, hit.point, ForceMode.Force);
        }

        if (hose.Held && player != null)
        {
            player.AddRecoil(-dir * recoilPerSecond * p * Time.deltaTime);
            player.AddShake(shakeAmount * p);
        }
    }
}
