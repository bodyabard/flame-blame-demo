using UnityEngine;
using UnityEngine.UI;

namespace FlameBlame.UI
{
    /// Layered metal plate: shadow, tinted tread, faint sheen, state outline. Same view for plate, small plate, flat plate and keycap prefabs.
    /// The outline is invisible at rest (the sprite carries its own dark edge) and carries state: danger, flash, shock.
    public sealed class UIPanelView : MonoBehaviour
    {
        public enum Role { Panel, Pill, Keycap }

        [SerializeField] Role role;
        [SerializeField] Image shadow, fill, gloss, outline;

        public Image Fill => fill;
        public Image Outline => outline;
        public Role PanelRole => role;
        public Color BaseFill { get; private set; }
        public Color BaseOutline { get; private set; }

        public void Apply(UIPalette palette)
        {
            BaseFill = role == Role.Keycap ? palette.keycapTint : palette.metalTint;
            BaseOutline = new Color(palette.danger.r, palette.danger.g, palette.danger.b, 0f);
            fill.color = BaseFill;
            outline.color = BaseOutline;
            shadow.color = Color.black;
            gloss.color = new Color(1f, 1f, 1f, 0.5f);
        }

        public void SetFill(Color color) { fill.color = color; }
        public void SetOutline(Color color) { outline.color = color; }
    }
}
