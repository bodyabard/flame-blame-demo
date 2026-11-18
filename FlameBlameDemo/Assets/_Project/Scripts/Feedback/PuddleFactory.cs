using System.Collections.Generic;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Fixed-size ring pool of puddle discs: once full, the oldest puddle is recycled.
    public sealed class PuddleFactory
    {
        readonly Queue<Transform> active;
        readonly Transform template;
        readonly Transform root;
        readonly int capacity;
        readonly float thickness;
        int spawned;

        public PuddleFactory(Transform root, Transform template, int capacity = 24)
        {
            this.root = root;
            this.template = template;
            this.capacity = capacity;
            thickness = template.localScale.y;
            active = new Queue<Transform>(capacity);
            template.gameObject.SetActive(false);
        }

        public void Spawn(Vector3 position, float diameter)
        {
            Transform t;
            if (active.Count >= capacity) t = active.Dequeue();
            else t = Object.Instantiate(template, root);   // only until the ring is full

            // Tiny per-puddle lift avoids z-fighting between overlapping discs.
            float lift = 0.01f + (spawned++ % capacity) * 0.0004f;
            t.SetPositionAndRotation(new Vector3(position.x, position.y + lift, position.z), Quaternion.identity);
            t.localScale = new Vector3(diameter, thickness, diameter);
            t.gameObject.SetActive(true);
            active.Enqueue(t);
        }
    }
}
