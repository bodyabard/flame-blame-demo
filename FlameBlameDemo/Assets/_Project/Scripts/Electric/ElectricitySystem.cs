using System.Collections.Generic;
using UnityEngine;

namespace FlameBlame.Electric
{
    public sealed class ElectricitySystem
    {
        readonly List<IElectrifiable> conductors = new List<IElectrifiable>(48);
        readonly List<SparkCable> cables = new List<SparkCable>(4);
        public void Register(IElectrifiable conductor) { if (!conductors.Contains(conductor)) conductors.Add(conductor); }
        public void Unregister(IElectrifiable conductor) => conductors.Remove(conductor);
        public void Register(SparkCable cable) { if (!cables.Contains(cable)) cables.Add(cable); }
        public void Tick()
        {
            for (int i = 0; i < conductors.Count; i++) conductors[i].Energized = false;
            for (int i = 0; i < cables.Count; i++)
            {
                if (cables[i] == null || !cables[i].isActiveAndEnabled) continue;
                for (int j = 0; j < conductors.Count; j++)
                    if (Touches(cables[i].transform.position, cables[i].Radius, conductors[j].Position, conductors[j].Radius))
                        conductors[j].Energized = true;
            }
            // At most N passes; connected puddles conduct regardless of insertion order.
            for (int pass = 0; pass < conductors.Count; pass++)
            {
                bool changed = false;
                for (int i = 0; i < conductors.Count; i++)
                {
                    if (!conductors[i].Energized) continue;
                    for (int j = 0; j < conductors.Count; j++)
                        if (!conductors[j].Energized && Touches(conductors[i].Position, conductors[i].Radius,
                            conductors[j].Position, conductors[j].Radius))
                        { conductors[j].Energized = true; changed = true; }
                }
                if (!changed) break;
            }
        }
        public bool IsLive(Vector3 feet, out Vector3 source)
        {
            for (int i = 0; i < conductors.Count; i++)
                if (conductors[i].Energized && Mathf.Abs(feet.y - conductors[i].Position.y) < 0.45f
                    && Touches(feet, 0f, conductors[i].Position, conductors[i].Radius))
                { source = conductors[i].Position; return true; }
            source = default;
            return false;
        }
        public static bool Touches(Vector3 a, float radiusA, Vector3 b, float radiusB)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            float radius = radiusA + radiusB;
            return delta.sqrMagnitude <= radius * radius;
        }
    }
}
