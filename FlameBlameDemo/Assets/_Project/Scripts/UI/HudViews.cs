using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlameBlame.UI
{
    /// Plain serializable bundles of scene references. Presenters read these; they never search the scene.

    [Serializable]
    public sealed class PanelWidget
    {
        public RectTransform root;
        public UIPanelView panel;
        public RectTransform icon;
        public Image iconImage;
        public TextMeshProUGUI label;
        public Vector2 restPosition;
        public Vector2 hiddenOffset;
        public float restRotation;
    }

    [Serializable]
    public sealed class GaugeView
    {
        public RectTransform root;
        public Image ring;
        public RectTransform needle;
        public TextMeshProUGUI percent;
        public Vector2 restPosition;
        public Vector2 hiddenOffset;
    }

    [Serializable]
    public sealed class ModeView
    {
        public RectTransform root;
        public UIPanelView pill;
        public Image icon;
        public TextMeshProUGUI label;
        public Sprite waterSprite;
        public Sprite jetSprite;
        public Vector2 restPosition;
        public Vector2 hiddenOffset;
    }

    [Serializable]
    public sealed class HintSlotView
    {
        public RectTransform root;
        public CanvasGroup group;
        public RectTransform keycap;
        public UIPanelView keycapPanel;
        public TextMeshProUGUI keyLabel;
        public TextMeshProUGUI caption;
    }

    [Serializable]
    public sealed class HintsView
    {
        public RectTransform root;
        public HintSlotView[] slots;
        public Vector2 restPosition;
        public Vector2 hiddenOffset;
    }

    [Serializable]
    public sealed class PromptView
    {
        public RectTransform root;
        public CanvasGroup group;
        public RectTransform breathe;
        public RectTransform keycap;
        public UIPanelView keycapPanel;
        public TextMeshProUGUI keyLabel;
        public RectTransform pill;
        public UIPanelView pillPanel;
        public TextMeshProUGUI label;
    }

    [Serializable]
    public sealed class BannerView
    {
        public RectTransform root;
        public CanvasGroup group;
        public UIPanelView pill;
        public TextMeshProUGUI label;
        public Vector2 restPosition;
    }

    [Serializable]
    public sealed class PopupView
    {
        public RectTransform template;
        public int count = 8;
    }

    [Serializable]
    public sealed class ResultRow
    {
        public RectTransform root;
        public CanvasGroup group;
        public TextMeshProUGUI value;
    }

    [Serializable]
    public sealed class ResultView
    {
        public GameObject root;
        public CanvasGroup dim;
        public RectTransform panel;
        public TextMeshProUGUI title;
        public ResultRow[] rows;
        public CanvasGroup reasonGroup;
        public TextMeshProUGUI reason;
        public RectTransform button;
        public RectTransform buttonVisual;
        public UnityEngine.UI.Button buttonComponent;
    }
}
