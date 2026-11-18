using System;
using UnityEngine;

namespace FlameBlame.Core
{
    public enum GamePhase { Playing, Result }
    public enum RoundEnding { TimeUp, FiresOut, DamageLimit }

    public sealed class GameStateMachine
    {
        readonly float duration, gracePeriod, quietPeriod;
        readonly int damageLimit;
        float quietTime;
        bool sawFire;
        public GamePhase Phase { get; private set; }
        public RoundEnding Ending { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, duration - Elapsed);
        public event Action<GamePhase> Changed;

        public GameStateMachine(float duration = 105f, int damageLimit = 10000,
            float gracePeriod = 12f, float quietPeriod = 3f)
        {
            this.duration = Mathf.Max(1f, duration);
            this.damageLimit = Mathf.Max(1, damageLimit);
            this.gracePeriod = gracePeriod;
            this.quietPeriod = quietPeriod;
        }
        public void Tick(float dt, int activeFires, int damage)
        {
            if (Phase != GamePhase.Playing) return;
            Elapsed += Mathf.Max(0f, dt);
            sawFire |= activeFires > 0;
            quietTime = activeFires > 0 ? 0f : quietTime + dt;
            if (damage >= damageLimit) Finish(RoundEnding.DamageLimit);
            else if (Elapsed >= duration) Finish(RoundEnding.TimeUp);
            else if (sawFire && Elapsed >= gracePeriod && quietTime >= quietPeriod) Finish(RoundEnding.FiresOut);
        }
        void Finish(RoundEnding ending)
        {
            Ending = ending;
            Phase = GamePhase.Result;
            Changed?.Invoke(Phase);
        }
    }
}
