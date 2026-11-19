using UnityEngine;

namespace FlameBlame.Feedback
{
    [CreateAssetMenu(menuName = "FlameBlame/Visual Tuning")]
    public sealed class VisualTuning : ScriptableObject
    {
        [Header("Palette")]
        public Color wall = new Color(.32f, .48f, .39f);
        public Color wood = new Color(.55f, .25f, .10f);
        public Color fabric = new Color(.12f, .43f, .49f);
        public Color curtain = new Color(.9f, .24f, .12f);
        public Color foliage = new Color(.26f, .55f, .12f);
        [Header("Sunset")]
        public Color sun = new Color(1f, .69f, .38f);
        public Color fill = new Color(.55f, .71f, 1f);
        [Range(0, 5)] public float sunIntensity = 1.8f;
        [Range(0, 3)] public float fillIntensity = .65f;
        [Range(0, 10)] public float bounceIntensity = 4f;
        [Range(0, 2)] public float bloom = .45f;
        [Range(-100, 100)] public float saturation = 12;
        [Range(0, .6f)] public float vignette = .22f;
        [Header("Particles — hard caps remain fixed")]
        public Color flame = new Color(1f, .36f, .025f);
        public Color core = new Color(1f, .9f, .35f);
        public Color smoke = new Color(.17f, .14f, .18f, .5f);
        [Range(.1f, 3)] public float flameSpeed = 1.1f;
        [Range(.1f, 2)] public float smokeSpeed = .45f;
        [Range(.1f, 2)] public float flameSize = .35f;
    }
}
