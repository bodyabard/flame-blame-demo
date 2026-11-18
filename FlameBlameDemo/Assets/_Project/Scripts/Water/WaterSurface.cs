using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.Water
{
    /// Floor-like surface: collects water and raises PuddleCreated.
    public class WaterSurface : MonoBehaviour, IWaterReceiver
    {
        [SerializeField] float puddleThreshold = 0.35f;   // water units per puddle
        [SerializeField] float puddleDiameter = 1.1f;
        [SerializeField] float minSpacing = 0.7f;
        [SerializeField] float minNormalY = 0.7f;

        IGameEvents events;
        float accumulated;
        Vector3 lastPuddle = new Vector3(1e6f, 0f, 1e6f);

        public void Initialize(IGameEvents gameEvents) { events = gameEvents; }

        public void ReceiveWater(Vector3 point, Vector3 normal, float amount)
        {
            if (events == null || normal.y < minNormalY) return;
            accumulated += amount;
            if (accumulated < puddleThreshold) return;
            if ((point - lastPuddle).sqrMagnitude < minSpacing * minSpacing) return;
            accumulated = 0f;
            lastPuddle = point;
            events.RaisePuddleCreated(point, puddleDiameter);
        }
    }
}
