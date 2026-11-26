using TMPro;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Scene references of the result shutter (own Canvas on top of everything). Laid out in code from the drop value.
    [System.Serializable]
    public sealed class ShutterView
    {
        public GameObject root;                 // the whole shutter canvas
        public RectTransform shakeRoot;         // over-scanned so a shake never shows an edge
        public RectTransform curtain;           // screen-sized frame; y = 0 is the top of the screen
        public RectTransform housing, slats, bottomBar, handle, hazard;
        public RectTransform plate;             // banner plate screwed to the slats
        public TextMeshProUGUI plateLabel;
        public CanvasGroup[] hudGroups;         // HUD canvases hidden while the shutter works
        public AudioSource audioSource;
        public AudioClip rattle, slam, latch;
    }
}
