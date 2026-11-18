using UnityEngine;

/// Рукав: Verlet-ланцюжок від гідранта до брандспойта.
/// E — взяти/кинути брандспойт. Кинутий під час потоку води — хльоскає по кімнаті.
[RequireComponent(typeof(LineRenderer))]
public class HoseSystem : MonoBehaviour
{
    [Header("Refs")]
    public Transform anchor;          // гідрант / кран
    public Transform handPoint;       // порожній об'єкт перед камерою (де тримаємо)
    public Transform nozzleModel;     // модель брандспойта (необов'язково)
    public PlayerController player;

    [Header("Rope")]
    public int segments = 28;
    public float segmentLength = 0.3f;
    public int iterations = 10;
    public float gravity = -12f;
    public float damping = 0.985f;
    public float radius = 0.06f;
    public LayerMask collisionMask = ~0;   // виключи шар Player!

    [Header("Pressure / whip")]
    public float basePressure = 1f;
    public float whipForce = 900f;
    public float whipNoise = 0.8f;
    public float grabDistance = 2.5f;

    [HideInInspector] public bool WaterFlowing;
    public bool Held { get; private set; } = true;
    public float Pressure { get; private set; } = 1f;
    public Vector3 NozzlePos { get; private set; }
    public Vector3 NozzleDir { get; private set; } = Vector3.forward;
    public float MaxReach => (segments - 1) * segmentLength * 0.92f;

    Vector3[] p, prev;
    LineRenderer line;
    readonly Collider[] buf = new Collider[8];

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
        p = new Vector3[segments];
        prev = new Vector3[segments];
        Vector3 dir = anchor.right;
        for (int i = 0; i < segments; i++)
        {
            p[i] = anchor.position + dir * segmentLength * i;
            prev[i] = p[i];
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Held) Held = false;
            else if (Vector3.Distance(handPoint.position, p[segments - 1]) < grabDistance) Held = true;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        int n = segments;

        // Інтеграція
        for (int i = 1; i < n; i++)
        {
            Vector3 v = (p[i] - prev[i]) * damping;
            prev[i] = p[i];
            p[i] += v + Vector3.up * gravity * dt * dt;
        }

        // Хльоскіт: кінець штовхається вздовж останнього сегмента + шум
        if (!Held && WaterFlowing)
        {
            Vector3 d = (p[n - 1] - p[n - 2]).normalized;
            float t = Time.time * 3f;
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(t, 1f) - 0.5f,
                Mathf.PerlinNoise(t, 2f) - 0.5f,
                Mathf.PerlinNoise(t, 3f) - 0.5f) * 2f * whipNoise;
            p[n - 1] += (d + noise) * whipForce * Pressure * dt * dt;
        }

        // Обмеження
        for (int it = 0; it < iterations; it++)
        {
            p[0] = anchor.position;
            if (Held) p[n - 1] = handPoint.position;

            for (int i = 0; i < n - 1; i++)
            {
                Vector3 delta = p[i + 1] - p[i];
                float d = delta.magnitude;
                if (d < 1e-5f) continue;
                Vector3 corr = delta * ((d - segmentLength) / d);
                bool pinA = i == 0;
                bool pinB = Held && i + 1 == n - 1;
                if (pinA && pinB) continue;
                if (pinA) p[i + 1] -= corr;
                else if (pinB) p[i] += corr;
                else { p[i] += corr * 0.5f; p[i + 1] -= corr * 0.5f; }
            }

            for (int i = 1; i < n; i++) Collide(ref p[i]);
        }

        UpdatePressure();
    }

    void Collide(ref Vector3 pt)
    {
        int c = Physics.OverlapSphereNonAlloc(pt, radius, buf, collisionMask, QueryTriggerInteraction.Ignore);
        for (int k = 0; k < c; k++)
        {
            Vector3 closest = buf[k].ClosestPoint(pt);
            Vector3 dir = pt - closest;
            float dist = dir.magnitude;
            if (dist < 1e-4f) { pt += Vector3.up * radius; continue; }
            if (dist < radius) pt += dir / dist * (radius - dist);
        }
    }

    void UpdatePressure()
    {
        // Перегин: різкий кут між сусідніми сегментами душить тиск
        int kinks = 0;
        for (int i = 1; i < segments - 1; i++)
        {
            Vector3 a = (p[i] - p[i - 1]).normalized;
            Vector3 b = (p[i + 1] - p[i]).normalized;
            if (Vector3.Dot(a, b) < 0.15f) kinks++;
        }
        float target = basePressure * Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(kinks / 3f));
        Pressure = Mathf.Lerp(Pressure, target, 8f * Time.fixedDeltaTime);
    }

    void LateUpdate()
    {
        int n = segments;
        NozzlePos = Held ? handPoint.position : p[n - 1];
        NozzleDir = Held ? handPoint.forward : (p[n - 1] - p[n - 2]).normalized;

        line.SetPositions(p);

        if (nozzleModel != null)
        {
            nozzleModel.position = NozzlePos;
            nozzleModel.rotation = Quaternion.LookRotation(NozzleDir);
        }
    }
}
