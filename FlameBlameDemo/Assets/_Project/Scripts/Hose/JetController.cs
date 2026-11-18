using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.Hose
{
    /// Valve logic + coordination of emitter, scanner, recoil and shake (the latter two go out as events).
    public class JetController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] HoseController hose;
        [SerializeField] JetEmitter emitter;
        [SerializeField] LayerMask hitMask = ~0;      // must exclude Player and Hose layers

        [Header("Jet")]
        [SerializeField] float maxSpeed = 15f;
        [SerializeField] int emissionRate = 450;
        [SerializeField] float jetRadius = 0.12f;
        [SerializeField] float rangeFactor = 0.55f;   // range = speed * factor
        [SerializeField] float waterPerSecond = 1f;
        [SerializeField] float pushForce = 25f;
        [SerializeField] float droppedFlowTime = 8f;

        [Header("Recoil")]
        [SerializeField] float recoilPerSecond = 9f;
        [SerializeField] float shakeAmount = 0.35f;

        IHoseInput input;
        IGameEvents events;
        JetHitScanner scanner;
        bool valveOpen;
        float droppedTimer;

        public bool ValveOpen => valveOpen;

        public void StopFlow()
        {
            valveOpen = false;
            hose.WaterFlowing = false;
            emitter.SetState(hose.NozzlePosition, hose.NozzleDirection, 0f, 0f);
        }

        public void Initialize(IHoseInput hoseInput, IGameEvents gameEvents)
        {
            input = hoseInput;
            events = gameEvents;
            scanner = new JetHitScanner { Radius = jetRadius, Mask = hitMask };
            emitter.Initialize(hitMask);
        }

        void Update()
        {
            if (input == null) return;

            if (hose.Held)
            {
                valveOpen = input.ValveHeld;
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
            Vector3 pos = hose.NozzlePosition;
            Vector3 dir = hose.NozzleDirection;

            emitter.SetState(pos, dir, speed, valveOpen ? emissionRate * Mathf.Clamp01(p + 0.2f) : 0f);
            if (!valveOpen) return;

            float dt = Time.deltaTime;
            if (scanner.Scan(pos, dir, speed * rangeFactor, waterPerSecond * p * dt, pushForce * p, out RaycastHit hit))
                events.RaiseWaterImpact(hit.point, hit.normal);

            if (hose.Held)
            {
                events.RaiseRecoil(-dir * recoilPerSecond * p * dt);
                events.RaiseShake(shakeAmount * p);
            }
        }
    }
}
