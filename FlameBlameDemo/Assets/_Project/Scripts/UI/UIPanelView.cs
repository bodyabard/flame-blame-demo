using UnityEngine;
using UnityEngine.UI;

namespace FlameBlame.UI
{
    /// Layered cartoon plate: shadow, tinted fill, gloss, dark outline. Same view for panel, pill and keycap prefabs.
    public sealed class UIPanelView : MonoBehaviour
    {
        public enum Role { Panel, Pill, Keycap }

        [SerializeField] Role role;
        [SerializeField] Image shadow, fill, gloss, outline;

        public Image Fill => fill;
        public Color BaseFill { get; private set; }

        public void Apply(UIPalette palette)
        {
            BaseFill = role == Role.Keycap ? palette.text : palette.fill;
            fill.color = BaseFill;
            outline.color = palette.outline;
            shadow.color = Color.black;
            gloss.color = Color.white;
        }

        public void SetFill(Color color) { fill.color = color; }
    }
}
