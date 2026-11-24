using UnityEngine;

namespace FlameBlame.UI
{
    /// Single source of UI colours.
    [CreateAssetMenu(menuName = "FlameBlame/UI Palette", fileName = "UIPalette")]
    public sealed class UIPalette : ScriptableObject
    {
        public Color fill = new Color32(0xD6, 0x3E, 0x24, 0xFF);
        public Color outline = new Color32(0x2B, 0x14, 0x10, 0xFF);
        public Color accent = new Color32(0xFF, 0xC2, 0x3C, 0xFF);
        public Color text = new Color32(0xFF, 0xF0, 0xDC, 0xFF);
        public Color water = new Color32(0x6E, 0xC8, 0xFF, 0xFF);
        public Color jet = new Color32(0xFF, 0xAA, 0x3C, 0xFF);
        public Color shock = new Color32(0xFF, 0xE0, 0x3C, 0xFF);
        public Color danger = new Color32(0xFF, 0x3B, 0x2F, 0xFF);

        /// Low pressure is red, mid is amber, healthy pressure is water blue.
        public Color PressureColor(float pressure01)
        {
            float p = Mathf.Clamp01(pressure01);
            return p < 0.5f ? Color.Lerp(danger, accent, p * 2f) : Color.Lerp(accent, water, (p - 0.5f) * 2f);
        }
    }
}
