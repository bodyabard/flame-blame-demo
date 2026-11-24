using FlameBlame.Core;
using FlameBlame.Ladder;
using UnityEngine;
namespace FlameBlame.Hose
{
    public sealed class HydrantConnector : MonoBehaviour, IInteractable
    {
        LevelScenario scenario;
        public void Initialize(LevelScenario owner) {scenario=owner;}
        public void Interact(PlayerLadderInteraction player) {if(player!=null) scenario.BeginConnection();}
    }
}
