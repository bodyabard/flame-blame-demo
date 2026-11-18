using System;

namespace FlameBlame.Core
{
    public sealed class DamageLedger : IDisposable
    {
        readonly IGameEvents events;
        public int Total { get; private set; }
        public int Incidents { get; private set; }
        public bool Closed { get; set; }
        public event Action<int> Changed;

        public DamageLedger(IGameEvents events)
        {
            this.events = events;
            events.DamageDealt += OnDamage;
        }
        void OnDamage(int amount)
        {
            if (Closed || amount <= 0) return;
            Total += amount;
            Incidents++;
            Changed?.Invoke(Total);
        }
        public void Dispose() => events.DamageDealt -= OnDamage;
    }
}
