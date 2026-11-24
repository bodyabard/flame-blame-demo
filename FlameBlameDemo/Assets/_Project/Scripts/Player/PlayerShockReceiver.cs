using FlameBlame.Core;
using FlameBlame.Electric;
using UnityEngine;

namespace FlameBlame.Player
{
    public sealed class PlayerShockReceiver
    {
        readonly Transform player;
        readonly IGameEvents events;
        readonly ChaosSettings settings;
        float nextShock;
        public PlayerShockReceiver(Transform player, IGameEvents events, ChaosSettings settings)
        { this.player = player; this.events = events; this.settings = settings; }
        public void Tick(ElectricitySystem electricity)
        {
            if (Time.time < nextShock || !electricity.IsLive(player.position, out Vector3 source)) return;
            nextShock = Time.time + settings.shockCooldown;
            Vector3 away = Vector3.ProjectOnPlane(player.position - source, Vector3.up).normalized;
            if (away.sqrMagnitude < 0.01f) away = -player.forward;
            events.RaiseRecoil((away + Vector3.up * 0.35f) * settings.shockStrength);
            events.RaiseShake(1f);
            events.RaisePlayerShocked();
            events.RaiseDamageReported(new DamageReport("Пожежник", "мокра електротерапія", settings.playerShockDamage, player.position + Vector3.up * 1.2f));
        }
    }
}
