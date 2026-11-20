using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Original (pre-calibration) greybox values, captured once, so Rebuild Main Level is idempotent and never compounds scaling.
    public sealed class LevelBaseline : ScriptableObject
    {
        [Serializable] public struct Entry { public string key; public Vector3 position, scale; public float value; }
        public List<Entry> entries = new List<Entry>();

        int Index(string key) { for (int i = 0; i < entries.Count; i++) if (entries[i].key == key) return i; return -1; }

        public Entry Transform(string key, Transform t)
        {
            int i = Index(key);
            if (i < 0) { entries.Add(new Entry { key = key, position = t.position, scale = t.localScale }); i = entries.Count - 1; }
            return entries[i];
        }

        public float Value(string key, float current)
        {
            int i = Index(key);
            if (i < 0) { entries.Add(new Entry { key = key, value = current }); i = entries.Count - 1; }
            return entries[i].value;
        }
    }
}
