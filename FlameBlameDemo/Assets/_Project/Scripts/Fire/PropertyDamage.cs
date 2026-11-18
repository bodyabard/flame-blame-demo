using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.Fire
{
    public class PropertyDamage : MonoBehaviour
    {
        [SerializeField] string displayName;
        [SerializeField] int propertyValue = 400;
        [SerializeField, Range(0f, 1f)] float ignitionLoss = 0.25f;
        [SerializeField, Range(0f, 1f)] float impactLoss = 0.15f;
        [SerializeField] float impactThreshold = 1.5f;
        IGameEvents events;
        Flammable flammable;
        bool knocked;
        public int RemainingValue { get; private set; }

        public void Initialize(IGameEvents gameEvents)
        {
            events = gameEvents;
            RemainingValue = propertyValue;
            flammable = GetComponent<Flammable>();
            events.FireIgnited += OnIgnited;
            events.FireBurnedOut += OnBurnedOut;
        }
        void OnIgnited(FireNode node)
        {
            if (ReferenceEquals(node.Owner, flammable)) Lose(Mathf.RoundToInt(propertyValue * ignitionLoss), "підсмажено");
        }
        void OnBurnedOut(FireNode node)
        {
            if (ReferenceEquals(node.Owner, flammable)) Lose(RemainingValue, "вугілля преміум-класу");
        }
        public void Hit(float speed)
        {
            if (events == null || knocked || speed < impactThreshold) return;
            knocked = true;
            Lose(Mathf.RoundToInt(propertyValue * impactLoss), "перестановка меблів");
        }
        void OnCollisionEnter(Collision collision) => Hit(collision.relativeVelocity.magnitude);
        void Lose(int amount, string reason)
        {
            int loss = Mathf.Min(RemainingValue, Mathf.Max(0, amount));
            RemainingValue -= loss;
            events.RaiseDamageReported(new DamageReport(string.IsNullOrEmpty(displayName) ? name : displayName, reason, loss));
        }
        void OnDestroy()
        {
            if (events == null) return;
            events.FireIgnited -= OnIgnited;
            events.FireBurnedOut -= OnBurnedOut;
        }
    }
}
