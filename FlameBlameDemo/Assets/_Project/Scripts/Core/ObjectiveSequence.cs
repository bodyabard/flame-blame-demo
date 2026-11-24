using System;
namespace FlameBlame.Core
{
    public enum ScenarioSignal { HosePickedUp, LadderPlaced, HoseConnected, LowerFiresOut, AtticEntered, AtticFiresOut }

    /// Remembers early setup actions; later goals require their predecessor.
    public sealed class ObjectiveSequence : IDisposable
    {
        readonly IGameEvents events;
        readonly bool[] completed = new bool[6];
        public int Current { get; private set; }
        public bool Complete => Current == 6;
        public ObjectiveSequence(IGameEvents events) { this.events=events; events.ScenarioSignaled+=OnSignal; }
        void OnSignal(ScenarioSignal signal)
        {
            int index=(int)signal;
            if(index>=3 && (index==3 ? !completed[2] : !completed[index-1])) return;
            if(completed[index]) return;
            completed[index]=true;
            int next=0; while(next<6 && completed[next]) next++;
            if(next==Current) return;
            Current=next; events.RaiseObjectiveChanged(Current);
        }
        public void Dispose() { events.ScenarioSignaled-=OnSignal; }
    }
}
