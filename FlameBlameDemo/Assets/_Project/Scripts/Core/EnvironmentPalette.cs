using UnityEngine;

namespace FlameBlame.Core
{
    /// All environment colours in one place (warm "locker room" key). Flat colours, no textures. Blue stays reserved for water.
    [CreateAssetMenu(menuName = "FlameBlame/Environment Palette")]
    public sealed class EnvironmentPalette : ScriptableObject
    {
        [Header("Room")]
        public Color wall = new Color(.96f, .74f, .58f);
        public Color wainscot = new Color(.83f, .39f, .2f);
        public Color floor = new Color(.43f, .48f, .58f);
        public Color ceiling = new Color(.86f, .66f, .52f);
        public Color door = new Color(.82f, .62f, .45f);
        [Header("Furniture")]
        public Color sofa = new Color(.72f, .3f, .2f);
        public Color accent = new Color(.88f, .36f, .18f);
        public Color wood = new Color(.62f, .35f, .19f);
        public Color metal = new Color(.6f, .55f, .54f);
        public Color paper = new Color(.96f, .85f, .62f);
        public Color petal = new Color(.86f, .3f, .38f);
        [Tooltip("Small accent only (flower leaves).")] public Color leaf = new Color(.42f, .6f, .22f);
        [Header("Flammables")]
        [Tooltip("Burning objects sit 30-40% darker than walls so flames read against any background. Warm, deep tones only.")]
        public Color flamBrick = new Color(.57f, .19f, .12f);
        public Color flamCherry = new Color(.40f, .10f, .15f);
        public Color flamChocolate = new Color(.40f, .23f, .13f);
        public Color flamMustard = new Color(.62f, .48f, .18f);
        public Color flamOlive = new Color(.28f, .31f, .13f);
        [Header("Characters")]
        public Color npcJacketA = new Color(.96f, .64f, .16f);
        public Color npcJacketB = new Color(.93f, .78f, .27f);
        public Color npcSkin = new Color(.88f, .43f, .28f);
        public Color npcStripe = new Color(1f, .82f, .2f);
        public Color npcBoot = new Color(.3f, .22f, .2f);
        public Color npcGlove = new Color(.78f, .62f, .26f);
        [Header("Hands")]
        public Color sleeve = new Color(.5f, .43f, .2f);
        public Color glove = new Color(.45f, .38f, .17f);
        public Color dirt = new Color(.28f, .24f, .16f);
    }
}
