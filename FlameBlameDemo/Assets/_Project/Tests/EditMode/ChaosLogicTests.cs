using FlameBlame.Core;
using FlameBlame.Electric;
using FlameBlame.Npc;
using NUnit.Framework;
using UnityEngine;

namespace FlameBlame.Tests
{
    public class ChaosLogicTests
    {
        [Test]
        public void Ledger_CountsReportsOnceAndStopsAtResult()
        {
            var events = new GameEvents();
            using (var ledger = new DamageLedger(events))
            {
                events.RaiseDamageReported(new DamageReport("Sofa", "Charred", 1200));
                events.RaiseDamageDealt(-20);
                Assert.That(ledger.Total, Is.EqualTo(1200));
                Assert.That(ledger.Incidents, Is.EqualTo(1));
                ledger.Closed = true;
                events.RaiseDamageDealt(200);
                Assert.That(ledger.Total, Is.EqualTo(1200));
            }
        }
        [Test]
        public void Round_RequiresFireBeforeQuietEndingAndFinishesOnlyOnce()
        {
            var game = new GameStateMachine(105f, 10000, 12f, 3f);
            int transitions = 0;
            game.Changed += _ => transitions++;
            game.Tick(13f, 0, 0);
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Playing));
            game.Tick(1f, 1, 0);
            game.Tick(2f, 0, 0);
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Playing));
            game.Tick(1f, 0, 0);
            Assert.That(game.Ending, Is.EqualTo(RoundEnding.FiresOut));
            game.Tick(200f, 1, 10000);
            Assert.That(transitions, Is.EqualTo(1));
            Assert.That(game.Elapsed, Is.EqualTo(17f));
        }
        [TestCase(105f, 0, RoundEnding.TimeUp)]
        [TestCase(1f, 10000, RoundEnding.DamageLimit)]
        public void Round_EndsForTimeOrDamage(float dt, int damage, RoundEnding reason)
        {
            var game = new GameStateMachine();
            game.Tick(dt, 1, damage);
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Result));
            Assert.That(game.Ending, Is.EqualTo(reason));
        }
        [Test]
        public void PuddleContact_UsesHorizontalRadii()
        {
            Assert.That(ElectricitySystem.Touches(Vector3.zero, 0.6f, new Vector3(1f, 0.01f, 0f), 0.6f), Is.True);
            Assert.That(ElectricitySystem.Touches(Vector3.zero, 0.6f, new Vector3(2f, 0f, 0f), 0.6f), Is.False);
        }
        [Test]
        public void Npc_RecoversFromShockButKeepsCharredAppearance()
        {
            var state = new NpcStateMachine();
            state.Tick(1f, true);
            Assert.That(state.State, Is.EqualTo(NpcState.Panic));
            state.React(NpcState.Charred, 1f);
            state.React(NpcState.Shocked, 1f);
            state.Tick(2f, false);
            Assert.That(state.State, Is.EqualTo(NpcState.Charred));
            Assert.That(state.IsCharred, Is.True);
        }
    }
}
