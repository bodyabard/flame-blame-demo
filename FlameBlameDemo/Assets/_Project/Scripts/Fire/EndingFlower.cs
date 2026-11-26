using FlameBlame.Core;
using UnityEngine;
namespace FlameBlame.Fire
{
    public sealed class EndingFlower : MonoBehaviour
    {
        public Flammable flammable;
        public Light glow;
        public ParticleSystem flameVisual, smokeVisual;
        IGameEvents events;
        bool finished;
        public void Initialize(FireSystem fire,IGameEvents hub)
        {
            events=hub; flammable.Initialize(fire,hub); flammable.Node.Zone=FireZoneId.Ending;
            flammable.Node.ZoneActive=true; fire.Ignite(flammable.Node); if(flameVisual!=null) flameVisual.Play(true); if(smokeVisual!=null) smokeVisual.Play(true); hub.FireExtinguished+=OnExtinguished;
        }
        void OnExtinguished(FireNode node)
        {
            if(finished || node!=flammable.Node) return;
            finished=true; glow.enabled=false; if(flameVisual!=null) flameVisual.Stop(true,ParticleSystemStopBehavior.StopEmitting); events.RaiseSteamCreated(node.Position); events.RaiseLoopBroken();
        }
        void OnDestroy() { if(events!=null) events.FireExtinguished-=OnExtinguished; }
    }
}
