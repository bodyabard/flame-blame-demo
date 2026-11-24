using UnityEngine;

namespace FlameBlame.Core
{
    public readonly struct DamageReport
    {
        public readonly string Subject;
        public readonly string Reason;
        public readonly int Amount;
        public readonly Vector3 Position;
        public readonly bool HasPosition;
        public DamageReport(string subject, string reason, int amount)
        {
            Subject = subject;
            Reason = reason;
            Amount = amount;
            Position = default;
            HasPosition = false;
        }
        public DamageReport(string subject, string reason, int amount, Vector3 position)
        {
            Subject = subject;
            Reason = reason;
            Amount = amount;
            Position = position;
            HasPosition = true;
        }
    }
}
