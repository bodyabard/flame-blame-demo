namespace FlameBlame.Core
{
    public readonly struct DamageReport
    {
        public readonly string Subject;
        public readonly string Reason;
        public readonly int Amount;
        public DamageReport(string subject, string reason, int amount)
        {
            Subject = subject;
            Reason = reason;
            Amount = amount;
        }
    }
}
